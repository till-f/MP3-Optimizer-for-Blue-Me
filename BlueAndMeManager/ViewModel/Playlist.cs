using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    public MusicDrive MusicDrive { get; }

    public string FullPath { get; private set; }

    public ObservableCollection<string> EntryPaths { get; } = new ();

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

      if (entryPaths == null)
      {
        return;
      }

      foreach (var entryPath in entryPaths)
      {
        EntryPaths.Add(entryPath);
      }
    }

    public void AddTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        if (!EntryPaths.Contains(relativePath))
        {
          EntryPaths.Add(relativePath);
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
        EntryPaths.Remove(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      Save();
    }

    public void Save()
    {
      PlaylistUpdater.Save(FullPath, EntryPaths);
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
