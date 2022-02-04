using System.IO;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<Mp3Detag.MusicFile>;

namespace Mp3Detag
{
  public class MusicFile : DependencyObject
  {
    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public MusicFile(MusicFolder musicFolder, string fullPath)
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
