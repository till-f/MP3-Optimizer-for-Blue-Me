using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TagLib;

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

        var album = mp3File.Tag.Album;
        var artist = mp3File.Tag.FirstPerformer;
        var title = mp3File.Tag.Title;

        mp3File.RemoveTags(TagTypes.AllTags);

        var id3V1 = mp3File.GetTag(TagTypes.Id3v1, true);

        id3V1.Album = GetAlbumId3V1(album);
        id3V1.Performers[0] = GetAlbumId3V1(artist);
        id3V1.Title = GetAlbumId3V1(title);

        mp3File.Save();
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
      var tempBytes = Encoding.GetEncoding("ISO-8859-8").GetBytes(fullCharsetString);
      var sanitizedString = Encoding.UTF8.GetString(tempBytes);
      if (sanitizedString.Length > 30)
      {
        sanitizedString = sanitizedString.Substring(0, 30);
      }
      return sanitizedString;
    }
  }
}
