using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Core.Helpers;
using TagLib;
using File = System.IO.File;

namespace BlueAndMeManager.Core
{
  public class BlueAndMeFixer
  {
    private enum EOpMode
    {
      OnlyTags, RenameAll, RenameNewOnly
    }

    private enum ESearchLevel
    {
      Folder, Track
    }

    private const string TmpFolderName = "_blueMeTmp";

    private readonly string _rootPath;
    private readonly EOpMode _opMode;
    private readonly int _affectedFilesCount;
    private readonly Dictionary<string, string[]> _folderToTracksMap = new();

    private uint _highestFolderNumber = 0;
    private uint _highestTrackNumber = 0;

    public int AffectedFilesCount => _affectedFilesCount;

    public BlueAndMeFixer(string rootPath, IEnumerable<string> trackPathsOrdered, bool renameFiles, bool isQuickRun)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _rootPath = rootPath;

      if (renameFiles && isQuickRun)
      {
        _opMode = EOpMode.RenameNewOnly;
      }
      else if (renameFiles)
      {
        _opMode = EOpMode.RenameAll;
      }
      else
      {
        _opMode = EOpMode.OnlyTags;
      }

      _folderToTracksMap = GetFolderToTracksMap(trackPathsOrdered, out _affectedFilesCount);
    }

    public Task<Dictionary<string, string>> RunAsync()
    {
      ManagerService.CancelSource = new CancellationTokenSource();
      var task = new Task<Dictionary<string, string>>(Run, ManagerService.CancelSource.Token);
      task.Start();
      return task;
    }

    public Dictionary<string, string> Run()
    {
      Dictionary<string, string> movedFiles = new();

      if (_affectedFilesCount == 0)
      {
        return movedFiles;
      }

      string currentFile = "";
      try
      {
        var processedFilesCount = 0;

        _highestFolderNumber = GetHighestFileNumber(Directory.GetDirectories(_rootPath, "*", SearchOption.TopDirectoryOnly));
        uint folderNumber = 0;
        foreach (var entry in _folderToTracksMap)
        {
          folderNumber = GetNextNumber(folderNumber, entry.Key, ESearchLevel.Folder);

          _highestTrackNumber = GetHighestFileNumber(Directory.GetFiles(entry.Key, "*.mp3", SearchOption.TopDirectoryOnly));
          uint trackNumber = 0;
          foreach (var trackPath in entry.Value)
          {
            if (ManagerService.CancelSource != null && ManagerService.CancelSource.IsCancellationRequested)
            {
              return movedFiles;
            }

            MessagePresenter.UpdateProgress(processedFilesCount++ / (double)_affectedFilesCount * 100, $"Processing {trackPath}...");

            trackNumber = GetNextNumber(trackNumber, trackPath, ESearchLevel.Track);
            currentFile = trackPath;
            var mp3File = TagLib.File.Create(trackPath);

            var album = SanitizeName(mp3File.Tag.Album, 14);
            var artist = SanitizeName(mp3File.Tag.FirstPerformer, 14);
            var title = SanitizeName(mp3File.Tag.Title, 30);
            var track = mp3File.Tag.Track;
            var genre = mp3File.Tag.FirstGenre;

            if (string.IsNullOrWhiteSpace(album))
            {
              album = $"Album {folderNumber}";
            }
            if (string.IsNullOrWhiteSpace(artist))
            {
              artist = "Unknown";
            }
            if (string.IsNullOrWhiteSpace(title))
            {
              title = SanitizeName(Path.GetFileNameWithoutExtension(trackPath), 30);
            }
            if (track == 0)
            {
              track = trackNumber;
            }

            mp3File.RemoveTags(TagTypes.AllTags);

            var id3V1 = mp3File.GetTag(TagTypes.Id3v1, true);
            id3V1.Album = album;
            id3V1.Title = title;
            id3V1.Track = track;
            if (!string.IsNullOrWhiteSpace(artist))
            {
              id3V1.Performers = new[] { artist };
            }
            if (!string.IsNullOrWhiteSpace(genre))
            {
              id3V1.Genres = new[] { genre };
            }

            mp3File.Save();

            if (_opMode != EOpMode.OnlyTags)
            {
              string tmpRootPath = _rootPath;
              if (_opMode == EOpMode.RenameAll)
              {
                tmpRootPath = Path.Combine(_rootPath, TmpFolderName);
              }
              var newFolderName = folderNumber.ToString("D3");
              var newFileName = trackNumber.ToString("D3") + ".mp3";
              var tmpFolderPath = Path.Combine(tmpRootPath, newFolderName);
              var tmpFilePath = Path.Combine(tmpFolderPath, newFileName);

              if (trackPath != tmpFilePath)
              {
                if (!Directory.Exists(tmpFolderPath))
                {
                  Directory.CreateDirectory(tmpFolderPath);
                }
                File.Move(trackPath, tmpFilePath);
                var oldRelPath = Utilities.GetRelativePath(_rootPath, trackPath);
                var newRelPath = Utilities.GetRelativePath(tmpRootPath, tmpFilePath);
                movedFiles.Add(oldRelPath, newRelPath);
              }
            }
          }
        }

        MessagePresenter.UpdateProgress(-1, "Cleanup folders...");
        ManagerService.CleanupFolders(_rootPath);

        return movedFiles;
      }
      catch (Exception e)
      {
        MessagePresenter.ShowError($"Could not convert file '{currentFile}': {e.Message}");
        return movedFiles;
      }
      finally
      {
        RestoreTemporarilyMovedFilesIfNeeded();
        MessagePresenter.UpdateProgress(0, "Idle");
      }
    }

    private void RestoreTemporarilyMovedFilesIfNeeded()
    {
      var tmpRootPath = Path.Combine(_rootPath, TmpFolderName);

      if (_opMode != EOpMode.RenameAll || !Directory.Exists(tmpRootPath))
      {
        return;
      }

      var errors = false;
      MessagePresenter.UpdateProgress(-1, "Restore temporarily moved files...");
      foreach (var oldDirectoryPath in Directory.GetDirectories(tmpRootPath, "*", SearchOption.AllDirectories))
      {
        var directoryName = Path.GetFileName(oldDirectoryPath);
        var newDirectoryPath = Path.Combine(_rootPath, directoryName);
        try
        {
          Directory.Move(oldDirectoryPath, newDirectoryPath);
        }
        catch
        {
          errors = true;
        }
      }

      if (errors)
      {
        MessagePresenter.ShowError($"Could not restore all temporarily moved files. Please move remaining files manually from from '{TmpFolderName}'.");
      }
      else
      {
        try
        {
          Directory.Delete(tmpRootPath, true);
        }
        catch (Exception ex)
        {
          MessagePresenter.ShowError($"Could not delete '{TmpFolderName}' folder: {ex.Message}");
        }
      }
    }

    private Dictionary<string, string[]> GetFolderToTracksMap(IEnumerable<string> trackPathsOrdered, out int affectedFilesCount)
    {
      const string onlyDigitsPattern = @"^\d+$";

      Dictionary<string, string[]> folderToTracksMap = new();

      affectedFilesCount = 0;
      foreach (var folderPath in Directory.GetDirectories(_rootPath))
      {
        var trackPaths = trackPathsOrdered.Where(trackPath => trackPath.StartsWith(folderPath)).ToArray();

        if (trackPaths.Length == 0)
        {
          continue;
        }

        if (_opMode != EOpMode.RenameNewOnly || !Regex.IsMatch(Path.GetFileNameWithoutExtension(folderPath), onlyDigitsPattern))
        {
          affectedFilesCount += trackPaths.Length;
          folderToTracksMap[folderPath] = trackPaths;
        }
        else
        {
          List<string> filesToUpdate = new ();
          foreach (var trackPath in trackPaths)
          {
            if (Directory.GetParent(trackPath).FullName != folderPath ||
              !Regex.IsMatch(Path.GetFileNameWithoutExtension(trackPath), onlyDigitsPattern))
            {
              filesToUpdate.Add(trackPath);
            }
          }

          if (filesToUpdate.Count > 0)
          {
            affectedFilesCount += filesToUpdate.Count;
            folderToTracksMap[folderPath] = filesToUpdate.ToArray();
          }
        }
      }

      return folderToTracksMap;
    }

    private uint GetNextNumber(uint localNumber, string filePath, ESearchLevel searchLevel)
    {
      if (_opMode != EOpMode.RenameNewOnly)
      {
        return ++localNumber;
      }

      var fileName = Path.GetFileNameWithoutExtension(filePath);
      if (searchLevel == ESearchLevel.Folder && uint.TryParse(fileName, out var parsedNumber))
      {
        return parsedNumber;
      }
      else
      {
        switch (searchLevel)
        {
          case ESearchLevel.Folder:
            _highestFolderNumber++;
            return _highestFolderNumber;
          case ESearchLevel.Track:
            _highestTrackNumber++;
            return _highestTrackNumber;
          default:
            throw new ArgumentOutOfRangeException(nameof(searchLevel));
        }
      }
    }

    private static string SanitizeName(string fullCharsetString, int maxLength = 0)
    {
      var sanitizedString = fullCharsetString
        .SanitizeByMap()                  // applies proper replacements like "ä -> ae"
        .SanitizeByEncoding()             // just in case the map is incomplete
        .WhitespaceNonBasicAscii()        // remove all control chars based on ASCII code
        .WhitespaceBlueAndMeUnsupported() // remove special chars not supported by Blue&Me
        .CollapseWhitespace();

      if (maxLength > 0 && sanitizedString.Length > maxLength)
      {
        sanitizedString = sanitizedString.Substring(0, maxLength);
      }
      
      return sanitizedString.Trim();
    }

    private static uint GetHighestFileNumber(IEnumerable<string> files)
    {
      var numbers = files.Select(file => Path.GetFileNameWithoutExtension(file)).Where(fileName => uint.TryParse(fileName, out var _)).Select(uint.Parse).ToArray();

      if (numbers.Length == 0)
      {
        return 0;
      }
      else
      {
        return numbers.Max();
      }
    }
  }
}
