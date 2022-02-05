using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BlueAndMeManager.Core;
using Extensions.Core.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Playlist>;

namespace BlueAndMeManager.ViewModel
{
  public class Playlist : DependencyObject
  {
    private readonly List<string> _entryPaths;

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

    public IEnumerable<string> EntryPaths => _entryPaths;

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
        _entryPaths = entryPaths.ToList();
      }
      else
      {
        _entryPaths = new List<string>();
      }
    }

    public void AddTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _entryPaths.Add(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      SaveToFile();
    }

    public void RemoveTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _entryPaths.Remove(relativePath);
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
      foreach (var entryPath in EntryPaths)
      {
        if (track.FullPath.EndsWith(entryPath))
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

      File.WriteAllLines(FullPath, EntryPaths);
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      var playlistFileName = e.NewValue + ".m3u";
      playlist.FullPath = Path.Combine(playlist.MusicDrive.FullPath, playlistFileName);
    }

    public void FilesMoved(Dictionary<string, string> taskResult)
    {
      if (taskResult.Count == 0)
      {
        return;
      }

      for (var i = 0; i < _entryPaths.Count; i++)
      {
        if (taskResult.ContainsKey(_entryPaths[i]))
        {
          _entryPaths[i] = taskResult[_entryPaths[i]];
        }
      }

      SaveToFile();
    }
  }
}
