using System.Collections.Generic;
using System.IO;
using System.Windows;
using BlueAndMeManager.Core;
using WpfExtensions.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.Playlist>;

namespace BlueAndMeManager
{
  public class Playlist : DependencyObject
  {
    private readonly HashSet<string> _relativeFilePaths = new ();

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

    public IEnumerable<string> RelativeFilePaths => _relativeFilePaths;

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value.RemoveInvalidFileNameChars());
    }
    
    public Playlist(MusicDrive musicDrive, string name, IEnumerable<string> entryPaths = null)
    {
      MusicDrive = musicDrive;
      Name = name;

      if (entryPaths != null)
      {
        foreach (var entryPath in entryPaths)
        {
          _relativeFilePaths.Add(entryPath);
        }
      }
    }

    public void AddTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _relativeFilePaths.Add(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      SaveToFile();
    }

    public void RemoveTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _relativeFilePaths.Remove(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      SaveToFile();
    }

    public void Delete()
    {
      MusicDrive.Playlists.Remove(this);
      File.Delete(FullPath);
    }

    public bool Contains(Track track)
    {
      foreach (var relativePath in RelativeFilePaths)
      {
        if (track.FullPath.EndsWith(relativePath))
        {
          return true;
        }
      }

      return false;
    }

    private void SaveToFile()
    {
      if (!File.Exists(FullPath))
      {
        File.Create(FullPath);
      }

      File.WriteAllLines(FullPath, RelativeFilePaths);
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      var playlistFileName = e.NewValue + ".m3u";
      playlist.FullPath = Path.Combine(playlist.MusicDrive.FullPath, playlistFileName);
    }
  }
}
