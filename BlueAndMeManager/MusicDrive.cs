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

    public ObservableCollection<Playlist> Playlists { get; } = new();

    public ObservableCollection<MusicFolder> MusicFolders { get; } = new ();

    public ObservableCollection<Track> Tracks { get; } = new();

    public ObservableCollection<MusicFolder> SelectedMusicFolders { get; } = new();

    public ObservableCollection<Track> SelectedTracks { get; } = new();

    public MusicDrive(string fullPath)
    {
      FullPath = fullPath;
    }

    private static void OnFullPathChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      var rootFolder = (string)e.NewValue;

      musicDrive.MusicFolders.Clear();

      foreach (var musicFolder in Directory.GetDirectories(rootFolder).Select(s => new MusicFolder(musicDrive, s)))
      {
        musicDrive.MusicFolders.Add(musicFolder);
      }

      foreach (var playlist in Directory.GetFiles(rootFolder, "*.m3u", SearchOption.TopDirectoryOnly).Select(s => new Playlist(musicDrive, Path.GetFileNameWithoutExtension(s))))
      {
        musicDrive.Playlists.Add(playlist);
      }

    }

    public void RefreshTracks()
    {
      Tracks.Clear();

      foreach (var folder in SelectedMusicFolders)
      {
        foreach (var musicFile in Directory.GetFiles(folder.FullPath, "*.mp3", SearchOption.AllDirectories).Select(s => new Track(folder, s)))
        {
          Tracks.Add(musicFile);
        }
      }
    }
  }
}
