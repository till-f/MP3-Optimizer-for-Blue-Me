using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using File = System.IO.File;

namespace BlueAndMeManager.Core
{
  public enum EFileSelectionMode
  {
    ExplicitFile,
    Directory,
    DirectoryRecursive
  }

  public class TagFixer
  {
    private readonly string[] _paths;
    private readonly EFileSelectionMode _fileSelectionMode;
    private readonly OnProgress _onProgress;
    private readonly OnError _onError;

    private IEnumerable<string> AllFiles
    {
      get
      {
        switch (_fileSelectionMode)
        {
          case EFileSelectionMode.ExplicitFile:
            return _paths;
          case EFileSelectionMode.Directory:
            return _paths.SelectMany(path => Directory.GetFiles(path, "*.mp3", SearchOption.TopDirectoryOnly));
          case EFileSelectionMode.DirectoryRecursive:
            return _paths.SelectMany(path => Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories));
          default:
            throw new ArgumentOutOfRangeException();
        }

      }
    }

    public TagFixer(IEnumerable<string> paths, EFileSelectionMode fileSelectionMode, OnProgress onProgress = null, OnError onError = null)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _paths = paths.ToArray();
      _fileSelectionMode = fileSelectionMode;
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

        double allFilesCount = AllFiles.Count();
        var processedFilesCount = 0;
        foreach (var mp3FilePath in AllFiles)
        {
          _onProgress?.Invoke(processedFilesCount++ / allFilesCount * 100, $"Processing {mp3FilePath}...");

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

          var newFileName = $"{track:00}-{artist}-{title}.mp3".RemoveInvalidFileNameChars();
          // ReSharper disable once PossibleNullReferenceException
          var newFilePath = Path.Combine(Directory.GetParent(mp3FilePath).FullName, newFileName);

          if (mp3FilePath != newFilePath)
          {
            File.Move(mp3FilePath, newFilePath);

            movedFiles.Add(mp3FilePath, newFilePath);
          }
        }

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
