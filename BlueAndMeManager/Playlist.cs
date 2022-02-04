using System.IO;

namespace BlueAndMeManager
{
  public class Playlist
  {
    public MusicDrive MusicDrive { get; }

    public string FullPath { get; }

    public Playlist(MusicDrive musicDrive, string fullPath)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }
  }
}
