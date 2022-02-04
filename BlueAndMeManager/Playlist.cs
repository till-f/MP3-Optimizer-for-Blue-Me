using System.IO;
using System.Windows;
using BlueAndMeManager.Core;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.Playlist>;

namespace BlueAndMeManager
{
  public class Playlist : DependencyObject
  {
    private string _fullPath;

    public MusicDrive MusicDrive { get; }

    public string FullPath
    {
      get => _fullPath;
      private set
      {
        if (_fullPath != null)
        {
          File.Move(_fullPath, value);
        }

        _fullPath = value;
      }
    }

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value.RemoveInvalidFileNameChars());
    }


    public Playlist(MusicDrive musicDrive, string name)
    {
      MusicDrive = musicDrive;
      Name = name;

      Initialize();
    }

    private void Initialize()
    {
      if (File.Exists(FullPath))
      {
        // todo: parse file
      }
      else
      {
        // todo: create empty file
        File.Create(FullPath);
      }
    }

    public void Remove()
    {
      MusicDrive.Playlists.Remove(this);
      File.Delete(FullPath);
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      var playlistFileName = e.NewValue + ".m3u";
      playlist.FullPath = Path.Combine(playlist.MusicDrive.FullPath, playlistFileName);
    }
  }
}
