using System.IO;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.Track>;

namespace BlueAndMeManager
{
  public class Track : DependencyObject
  {
    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public static readonly DependencyProperty IsInCurrentListProperty = RegisterProperty(x => x.IsInCurrentList);

    public bool IsInCurrentList
    {
      get => (bool)GetValue(IsInCurrentListProperty);
      set => SetValue(IsInCurrentListProperty, value);
    }

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
