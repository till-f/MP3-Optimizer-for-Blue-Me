using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions.Core.Helpers;
using TagLib;
using File = System.IO.File;

namespace BlueAndMeManager.Core
{
  public class BlueAndMeFixer
  {
    private readonly string _rootPath;
    private readonly string[] _mp3FilePaths;

    public BlueAndMeFixer(string rootPath, IEnumerable<string> mp3FilePaths)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _rootPath = rootPath;
      _mp3FilePaths = mp3FilePaths.ToArray();
    }

    public Task<Dictionary<string, string>> RunAsync()
    {
      var task = new Task<Dictionary<string, string>>(Run);
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

        foreach (var mp3FilePath in _mp3FilePaths)
        {
          if (ManagerService.CancelRequested)
          {
            return movedFiles;
          }

          currentFile = mp3FilePath;

          MessagePresenter.UpdateProgress(processedFilesCount++ / allFilesCount * 100, $"Fixing tags of {mp3FilePath}...");

          var mp3File = TagLib.File.Create(mp3FilePath);

          var album = SanitizeName(mp3File.Tag.Album, 30);
          var artist = SanitizeName(mp3File.Tag.FirstPerformer, 30);
          var title = SanitizeName(mp3File.Tag.Title, 30);
          var track = mp3File.Tag.Track;
          var genre = mp3File.Tag.FirstGenre;

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

          // ReSharper disable once PossibleNullReferenceException
          var newFolderName = SanitizeName(Directory.GetParent(mp3FilePath).Name).RemoveInvalidFileNameChars();
          var newFileName = SanitizeName(Path.GetFileNameWithoutExtension(mp3FilePath)).RemoveInvalidFileNameChars() + ".mp3";
          var newFolderPath = Path.Combine(_rootPath, newFolderName);
          var newFilePath = Path.Combine(newFolderPath, newFileName);

          if (mp3FilePath != newFilePath)
          {
            if (!Directory.Exists(newFolderPath))
            {
              Directory.CreateDirectory(newFolderPath);
            }
            File.Move(mp3FilePath, newFilePath);
            var oldRelPath = Utilities.GetRelativePath(_rootPath, mp3FilePath);
            var newRelPath = Utilities.GetRelativePath(_rootPath, newFilePath);
            movedFiles.Add(oldRelPath, newRelPath);
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
        MessagePresenter.UpdateProgress(0, "Idle");
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
