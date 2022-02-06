using System.Collections.Generic;
using System.Dynamic;
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

    public MusicDrive MusicDrive { get; }

    public string FullPath { get; private set; }

    public IEnumerable<string> EntryPaths => _entryPaths;

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value);
    }
    
    public Playlist(MusicDrive musicDrive, string fullPath, IEnumerable<string> entryPaths = null)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;
      Name = Path.GetFileNameWithoutExtension(fullPath).Trim();

      if (!File.Exists(FullPath))
      {
        File.Create(FullPath);
      }

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
        if (!_entryPaths.Contains(relativePath))
        {
          _entryPaths.Add(relativePath);
        }
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      Save();
    }

    public void RemoveTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _entryPaths.Remove(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      Save();
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

    private void Save()
    {
      PlaylistUpdater.Save(FullPath, EntryPaths);
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue == null)
      {
        return;
      }

      playlist.FullPath = PlaylistUpdater.Rename(playlist.FullPath, (string) e.NewValue);
    }
  }
}
