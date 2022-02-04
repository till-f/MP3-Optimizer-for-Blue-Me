using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.MusicDrive>;

namespace BlueAndMeManager
{
  public class MusicDrive : DependencyObject
  {
    public static readonly DependencyProperty FullPathProperty = RegisterProperty(musicDrive => musicDrive.FullPath).OnChange(OnFullPathChanged);

    public string FullPath
    {
      get => (string)GetValue(FullPathProperty);
      set => SetValue(FullPathProperty, value);
    }

    public ObservableCollection<MusicFolder> Folders { get; } = new ();

    public ObservableCollection<MusicFile> Tracks { get; } = new();

    public ObservableCollection<MusicFolder> SelectedFolders { get; } = new();

    public ObservableCollection<MusicFile> SelectedTracks { get; } = new();

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

    public void RefreshTracks()
    {
      Tracks.Clear();

      foreach (var folder in SelectedFolders)
      {
        foreach (var musicFile in Directory.GetFiles(folder.FullPath, "*.mp3", SearchOption.AllDirectories).Select(s => new MusicFile(folder, s)))
        {
          Tracks.Add(musicFile);
        }
      }
    }
  }
}
