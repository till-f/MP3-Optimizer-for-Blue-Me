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
  public class TagFixer
  {
    private readonly string _rootPath;
    private readonly string[] _mp3FilePaths;
    private readonly OnProgress _onProgress;
    private readonly OnError _onError;

    public TagFixer(string rootPath, IEnumerable<string> mp3FilePaths, OnProgress onProgress = null, OnError onError = null)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _rootPath = rootPath;
      _mp3FilePaths = mp3FilePaths.ToArray();
      _onProgress = onProgress;
      _onError = onError;
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

      try
      {
        _onProgress?.Invoke(0, "Processing...");

        int idsCount = 0;
        Dictionary<string, int> albumIds = new();

        double allFilesCount = _mp3FilePaths.Length;
        var processedFilesCount = 0;

        foreach (var mp3FilePath in _mp3FilePaths)
        {
          _onProgress?.Invoke(processedFilesCount++ / allFilesCount * 100, $"Fixing tags of {mp3FilePath}...");

          var mp3File = TagLib.File.Create(mp3FilePath);

          var album = GetAlbumId3V1(mp3File.Tag.Album);
          var artist = GetArtistId3V1(mp3File.Tag.FirstPerformer);
          var title = GetTitleId3V1(mp3File.Tag.Title);
          var track = mp3File.Tag.Track;

          mp3File.RemoveTags(TagTypes.AllTags);

          var id3V1 = mp3File.GetTag(TagTypes.Id3v1, true);

          id3V1.Track = track;
          id3V1.Album = album;
          id3V1.Performers = new[] { artist };
          id3V1.Title = title;

          mp3File.Save();

          if (!albumIds.ContainsKey(album))
          {
            albumIds[album] = idsCount++;
          }
          var newFolderName = $"{albumIds[album]:00}-{album}".RemoveInvalidFileNameChars(true);
          var newFileName = $"{track:00}-{artist}-{title}.mp3".RemoveInvalidFileNameChars(true);
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

        _onProgress?.Invoke(-1, "Cleanup folders...");
        CleanupFolders();

        return movedFiles;
      }
      catch (Exception e)
      {
        _onError?.Invoke($"{e.GetType().Name}: {e.Message}");

        return movedFiles;
      }
      finally
      {
        _onProgress?.Invoke(0, "Idle");
      }
    }

    private void CleanupFolders()
    {
      foreach (var directory in Directory.GetDirectories(_rootPath, "*", SearchOption.AllDirectories))
      {
        if (Directory.Exists(directory) && Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
        {
          Directory.Delete(directory, true);
        }
      }
    }

    private static string GetAlbumId3V1(string album)
    {
      return SanitizeName(album, 30);
    }

    private static string GetArtistId3V1(string artist)
    {
      return SanitizeName(artist, 30);
    }

    private static string GetTitleId3V1(string title)
    {
      return SanitizeName(title, 30);
    }

    private static string SanitizeName(string fullCharsetString, int maxLength)
    {
      var sanitizedString = fullCharsetString
        .SanitizeByMap()                  // applies proper replacements like "ä -> ae"
        .SanitizeByEncoding()             // just in case the map is incomplete
        .WhitespaceNonBasicAscii()        // remove all control chars based on ASCII code
        .WhitespaceBlueAndMeUnsupported() // remove special chars not supported by Blue&Me
        .CollapseWhitespace();

      if (sanitizedString.Length > 30)
      {
        sanitizedString = sanitizedString.Substring(0, maxLength);
      }
      
      return sanitizedString;
    }
  }
}
