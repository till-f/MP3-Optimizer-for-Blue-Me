using System.IO;
using System.Windows;

namespace BlueAndMeManager
{
  public class Track : DependencyObject
  {
    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public Track(MusicFolder musicFolder, string fullPath)
    {
      MusicFolder = musicFolder;
      FullPath = fullPath;
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }
  }
}
