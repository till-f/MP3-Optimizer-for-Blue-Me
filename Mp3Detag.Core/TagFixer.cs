using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using File = System.IO.File;

namespace Mp3Detag.Core
{
  public enum EFileSelectionMode
  {
    ExplicitFile,
    Directory,
    DirectoryRecursive
  }

  public class TagFixer
  {
    private readonly string _filePath;

    private readonly EFileSelectionMode _fileSelectionMode;

    private string[] AllFiles
    {
      get
      {
        switch (_fileSelectionMode)
        {
          case EFileSelectionMode.ExplicitFile:
            return new[] { _filePath };
          case EFileSelectionMode.Directory:
            return Directory.GetFiles(_filePath, "*.mp3", SearchOption.TopDirectoryOnly);
          case EFileSelectionMode.DirectoryRecursive:
            return Directory.GetFiles(_filePath, "*.mp3", SearchOption.AllDirectories);
          default:
            throw new ArgumentOutOfRangeException();
        }

      }
    }

    public TagFixer(string filePath, EFileSelectionMode fileSelectionMode)
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      _filePath = filePath;
      _fileSelectionMode = fileSelectionMode;
    }

    public Task RunAsync()
    {
      var task = new Task(Run);
      task.Start();
      return task;
    }

    public void Run()
    {
      foreach (var mp3FilePath in AllFiles)
      {
        Debug.Print($"Processing file {mp3FilePath}...");

        var mp3File = TagLib.File.Create(mp3FilePath);

        var album = GetAlbumId3V1(mp3File.Tag.Album);
        var artist = GetArtistId3V1(mp3File.Tag.FirstPerformer);
        var title = GetTitleId3V1(mp3File.Tag.Title);
        var track = mp3File.Tag.Track;

        mp3File.RemoveTags(TagTypes.AllTags);

        var id3V1 = mp3File.GetTag(TagTypes.Id3v1, true);

        id3V1.Track = track;
        id3V1.Album = album;
        id3V1.Performers = new []{ artist };
        id3V1.Title = title;

        mp3File.Save();

        var newFileName = $"{track:00}-{artist}-{title}.mp3".RemoveInvalidFileNameChars();
        // ReSharper disable once PossibleNullReferenceException
        var newFilePath = Path.Combine(Directory.GetParent(mp3FilePath).FullName, newFileName);
        File.Move(mp3FilePath, newFilePath);
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
        .SanitizeByMap()                // applies proper replacements like "ä -> ae"
        .SanitizeByEncoding()           // just in case the map is incomplete
        .RemoveInvalidBlueAndMeChars1() // remove all control chars based on ASCII code
        .RemoveInvalidBlueAndMeChars2() // remove special chars not supported by Blue&Me
        .CollapseWhitespace();

      if (sanitizedString.Length > 30)
      {
        sanitizedString = sanitizedString.Substring(0, maxLength);
      }
      
      return sanitizedString;
    }
  }
}
