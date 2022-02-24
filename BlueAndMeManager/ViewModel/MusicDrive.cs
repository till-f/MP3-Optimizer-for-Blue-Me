using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using BlueAndMeManager.Core;
using Extensions.Core;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.MusicDrive>;

namespace BlueAndMeManager.ViewModel
{
  public class MusicDrive : DependencyObject
  {
    public string FullPath { get; }

    public static readonly DependencyProperty SelectedPlaylistProperty = RegisterProperty(x => x.SelectedPlaylist).OnChange(OnSelectedPlaylistChanged);

    public Playlist SelectedPlaylist
    {
      get => (Playlist)GetValue(SelectedPlaylistProperty);
      set => SetValue(SelectedPlaylistProperty, value);
    }

    public ObservableCollection<Playlist> Playlists { get; } = new();

    public ObservableCollection<MusicFolder> MusicFolders { get; } = new ();

    public ObservableCollection<Track> TracksInSelectedFolders { get; } = new();

    public ObservableCollection<MusicFolder> SelectedMusicFolders { get; } = new();

    public ObservableCollection<Track> SelectedTracks { get; } = new();

    public Dictionary<string, Track> TrackByFullPath { get; } = new Dictionary<string, Track>();

    public Dictionary<string, IEnumerable<string>> CorePlaylists
    {
      get
      {
        var playlists = new Dictionary<string, IEnumerable<string>>();
        foreach (var playlist in Playlists)
        {
          playlists[playlist.FullPath] = playlist.Tracks.Select(x => x.RelativePath);
        }

        return playlists;
      }
    }

    public IEnumerable<Track> TracksInScope
    {
      get
      {
        if (SelectedTracks?.Count > 0)
        {
          return SelectedTracks;
        }
        else if (TracksInSelectedFolders?.Count > 0)
        {
          return TracksInSelectedFolders;
        }
        else
        {
          List<Track> tracks = new ();
          foreach (var folder in MusicFolders)
          {
            tracks.AddRange(folder.Tracks);
          }
          return tracks;
        }
      }
    }

    public MusicDrive(string fullPath)
    {
      FullPath = fullPath;
    }

    public void RefreshTracks()
    {
      LinkedList<Track> newTracks = new(SelectedMusicFolders.SelectMany(x => x.Tracks));

      TracksInSelectedFolders.RemoveWhere(x => !newTracks.Contains(x));

      int lastIdx = 0;
      foreach (var newTrack in newTracks)
      {
        if (!TracksInSelectedFolders.Contains(newTrack))
        {
          TracksInSelectedFolders.Insert(lastIdx, newTrack);
        }

        lastIdx++;
      }
    }
    
    public void AddTracksInScopeToPlaylist()
    {
      SelectedPlaylist?.AddTracks(TracksInScope);
    }

    public void RemoveTracksInScopeFromPlaylist()
    {
      SelectedPlaylist?.RemoveTracks(TracksInScope);
    }

    public void UpdatePlaylistContainmentStates(bool includeTracks)
    {
      foreach (var musicFolder in MusicFolders)
      {
        musicFolder.UpdatePlaylistContainmentState(includeTracks);
      }
    }

    public void UpdateFromCache(FilesystemCache filesystemCache)
    {
      // remove all cached track paths, will be filled from MusicFolder.UpdateTracks()
      TrackByFullPath.Clear();

      // remove deleted folders
      MusicFolders.RemoveWhere(x => !filesystemCache.MusicCache.ContainsKey(x.FullPath));

      // remove deleted playlists
      Playlists.RemoveWhere(x => !filesystemCache.PlaylistCache.ContainsKey(x.FullPath));

      // insert folders in correct order, reusing existing objects
      var lastIdx = 0;
      foreach (var kvp in filesystemCache.MusicCache)
      {
        var musicFolder = MusicFolders.FirstOrDefault(x => x.FullPath == kvp.Key);
        if (musicFolder == null)
        {
          musicFolder = new MusicFolder(this, kvp.Key, kvp.Value);
          MusicFolders.Insert(lastIdx, musicFolder);
        }
        else
        {
          musicFolder.UpdateTracks(kvp.Value);
        }

        lastIdx++;
      }

      // insert playlists in correct order, reusing existing objects
      lastIdx = 0;
      foreach (var kvp in filesystemCache.PlaylistCache)
      {
        var playlist = Playlists.FirstOrDefault(x => x.FullPath == kvp.Key);
        bool isNewList = false;
        var newTracks = kvp.Value.Select(x => TrackByFullPath[Path.Combine(FullPath, x)]);
        if (playlist == null)
        {
          isNewList = true;
          playlist = new Playlist(this, kvp.Key, newTracks);
          Playlists.Insert(lastIdx, playlist);
        }
        else
        {
          playlist.UpdateTracks(newTracks);
        }

        if (isNewList)
        {
          // always save new list to make sure that files skipped during parsing are permanently removed
          playlist.SaveAsync();
        }

        lastIdx++;
      }

      RefreshTracks();

      UpdatePlaylistContainmentStates(true);
    }

    private static void OnSelectedPlaylistChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      musicDrive.UpdatePlaylistContainmentStates(true);
    }
   
  }
}
