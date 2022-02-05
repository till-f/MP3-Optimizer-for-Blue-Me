using System.IO;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.MusicFolder>;

namespace BlueAndMeManager
{
  public class MusicFolder : DependencyObject
  {
    public MusicDrive MusicDrive { get; }

    public string FullPath { get; }

    public static readonly DependencyProperty IsInCurrentListProperty = RegisterProperty(x => x.IsInCurrentList);

    public bool IsInCurrentList
    {
      get => (bool)GetValue(IsInCurrentListProperty);
      set => SetValue(IsInCurrentListProperty, value);
    }

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
