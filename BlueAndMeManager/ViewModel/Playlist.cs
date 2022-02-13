using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BlueAndMeManager.Core;
using Extensions.Core;
using Extensions.Core.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Playlist>;

namespace BlueAndMeManager.ViewModel
{
  public class Playlist : DependencyObject
  {
    private readonly HashSet<Track> _tracks = new HashSet<Track>();

    public MusicDrive MusicDrive { get; }

    public string FullPath { get; private set; }

    public ObservableCollection<Track> Tracks { get; } = new ();

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value);
    }
    
    public Playlist(MusicDrive musicDrive, string fullPath, IEnumerable<Track> tracks = null)
    {
      MusicDrive = musicDrive;
      Name = Path.GetFileNameWithoutExtension(fullPath).Trim();

      // fake rename (will add spaces to the file name)
      FullPath = PlaylistService.CreateOrRename(fullPath, Name);

      if (tracks == null)
      {
        return;
      }

      foreach (var track in tracks)
      {
        _tracks.Add(track);
        Tracks.Add(track);
      }
    }

    public void Clear()
    {
      Tracks.Clear();
      _tracks.Clear();
    }

    public void AddTracks(IEnumerable<Track> tracks)
    {
      foreach (var track in tracks)
      {
        if (!_tracks.Contains(track))
        {
          _tracks.Add(track);
          Tracks.Add(track);
          track.UpdatePlaylistContainmentState();
        }
      }

      MusicDrive.UpdatePlaylistContainmentStates(false);

      SaveAsync();
    }

    public void RemoveTracks(IEnumerable<Track> tracks)
    {
      foreach (var track in tracks)
      {
        _tracks.Remove(track);
        Tracks.Remove(track);
        track.UpdatePlaylistContainmentState();
      }

      MusicDrive.UpdatePlaylistContainmentStates(false);

      SaveAsync();
    }

    public Task SaveAsync()
    {
      return PlaylistService.SaveAsync(FullPath, Tracks.Select(x => x.RelativePath));
    }

    public bool Delete()
    {
      var isDelted = PlaylistService.Delete(FullPath);

      if (isDelted)
      {
        MusicDrive.Playlists.Remove(this);
      }

      return isDelted;
    }

    public bool Contains(Track track)
    {
      return _tracks.Contains(track);
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue == null)
      {
        return;
      }

      playlist.FullPath = PlaylistService.CreateOrRename(playlist.FullPath, (string) e.NewValue);
    }

    public void UpdateTracks(IEnumerable<Track> newTracks)
    {
      _tracks.RemoveWhere(x => !newTracks.Contains(x));
      Tracks.RemoveWhere(x => !newTracks.Contains(x));

      var lastIdx = 0;
      foreach (var newTrack in newTracks)
      {
        if (!_tracks.Contains(newTrack))
        {
          _tracks.Add(newTrack);
          Tracks.Insert(lastIdx, newTrack);
        }
        lastIdx++;
      }
    }
  }
}
