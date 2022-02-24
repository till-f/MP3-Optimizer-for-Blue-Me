using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Extensions.Core.Helpers;
using TagLib;
using File = System.IO.File;

namespace BlueAndMeManager.Core
{
  public class BlueAndMeFixer
  {
    private const string TmpFolderName = "_tmp";

    private readonly string _rootPath;
    private readonly string[] _mp3FilePaths;
    private readonly bool _minimizeFileNames;

    public BlueAndMeFixer(string rootPath, IEnumerable<string> mp3FilePaths, bool minimizeFileNames)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _rootPath = rootPath;
      _mp3FilePaths = mp3FilePaths.ToArray();
      _minimizeFileNames = minimizeFileNames;
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

      string currentFile = "";
      try
      {
        double allFilesCount = _mp3FilePaths.Length;
        var processedFilesCount = 0;

        int folderCounter = 0;
        uint trackCounter = 0;
        string lastFolderName = null;

        foreach (var mp3FilePath in _mp3FilePaths)
        {
          if (ManagerService.CancelSource != null && ManagerService.CancelSource.IsCancellationRequested)
          {
            return movedFiles;
          }

          currentFile = mp3FilePath;

          MessagePresenter.UpdateProgress(processedFilesCount++ / allFilesCount * 100, $"Processing {mp3FilePath}...");

          var folderName = Directory.GetParent(mp3FilePath).Name;
          if (folderName != lastFolderName)
          {
            lastFolderName = folderName;
            trackCounter = 0;
            folderCounter++;
          }
          trackCounter++;

          var mp3File = TagLib.File.Create(mp3FilePath);

          var album = SanitizeName(mp3File.Tag.Album, 14);
          var artist = SanitizeName(mp3File.Tag.FirstPerformer, 14);
          var title = SanitizeName(mp3File.Tag.Title, 30);
          var track = mp3File.Tag.Track;
          var genre = mp3File.Tag.FirstGenre;

          if (track == 0)
          {
            track = trackCounter;
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

          if (_minimizeFileNames)
          {
            var newFolderName = folderCounter.ToString("D3");
            var newFileName = trackCounter.ToString("D3") + ".mp3";
            var tmpRootPath = Path.Combine(_rootPath, TmpFolderName);
            var tmpFolderPath = Path.Combine(tmpRootPath, newFolderName);
            var tmpFilePath = Path.Combine(tmpFolderPath, newFileName);

            if (mp3FilePath != tmpFilePath)
            {
              if (!Directory.Exists(tmpFolderPath))
              {
                Directory.CreateDirectory(tmpFolderPath);
              }
              File.Move(mp3FilePath, tmpFilePath);
              var oldRelPath = Utilities.GetRelativePath(_rootPath, mp3FilePath);
              var newRelPath = Utilities.GetRelativePath(tmpRootPath, tmpFilePath);
              movedFiles.Add(oldRelPath, newRelPath);
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
      if (!_minimizeFileNames || !Directory.Exists(_rootPath))
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
  }
}
