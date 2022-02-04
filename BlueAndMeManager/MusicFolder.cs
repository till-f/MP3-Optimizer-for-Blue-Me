using System.IO;
using System.Windows;

namespace BlueAndMeManager
{
  public class MusicFolder : DependencyObject
  {
    public MusicDrive MusicDrive { get; }

    public string FullPath { get; }

    public MusicFolder(MusicDrive musicDrive, string fullPath)
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
