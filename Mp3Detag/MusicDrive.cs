using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<Mp3Detag.MusicDrive>;

namespace Mp3Detag
{
  public class MusicDrive : DependencyObject
  {
    public ObservableCollection<MusicFolder> Folders { get; } = new ();

    public static readonly DependencyProperty FullPathProperty = RegisterProperty(musicDrive => musicDrive.FullPath).OnChange(OnFullPathChanged);

    public string FullPath
    {
      get => (string)GetValue(FullPathProperty);
      set => SetValue(FullPathProperty, value);
    }

    public static readonly DependencyProperty SelectedFolderProperty = RegisterProperty(musicDrive => musicDrive.SelectedFolder);

    public MusicFolder SelectedFolder
    {
      get => (MusicFolder)GetValue(SelectedFolderProperty);
      set => SetValue(SelectedFolderProperty, value);
    }

    public MusicDrive(string fullPath)
    {
      FullPath = fullPath;
    }

    private static void OnFullPathChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      var rootFolder = (string)e.NewValue;

      musicDrive.Folders.Clear();

      foreach (var musicFolder in Directory.GetDirectories(rootFolder).Select(s => new MusicFolder(musicDrive, s)))
      {
        musicDrive.Folders.Add(musicFolder);
      }
    }
  }
}
