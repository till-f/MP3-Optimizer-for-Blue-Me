using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using BlueAndMeManager.Core;
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

    public Dictionary<string, IEnumerable<string>> CorePlaylists
    {
      get
      {
        var playlists = new Dictionary<string, IEnumerable<string>>();
        foreach (var playlist in Playlists)
        {
          playlists[playlist.FullPath] = playlist.EntryPaths;
        }

        return playlists;
      }
    }

    public IEnumerable<string> TrackPathsInScope
    {
      get
      {
        if (SelectedTracks?.Count > 0)
        {
          return SelectedTracks.Select(x => x.FullPath);
        }
        else if (TracksInSelectedFolders?.Count > 0)
        {
          return TracksInSelectedFolders.Select(x => x.FullPath);
        }
        else
        {
          return Directory.GetFiles(FullPath, "*.mp3", SearchOption.AllDirectories);
        }
      }
    }

    public MusicDrive(string fullPath)
    {
      FullPath = fullPath;
    }

    public void RefreshTracks()
    {
      TracksInSelectedFolders.Clear();

      foreach (var folder in SelectedMusicFolders)
      {
        foreach (var track in folder.Tracks)
        {
          TracksInSelectedFolders.Add(track);
        }
      }
    }
    
    public void AddTracksInScopeToPlaylist()
    {
      SelectedPlaylist?.AddTracks(TrackPathsInScope);
    }

    public void RemoveTracksInScopeFromPlaylist()
    {
      SelectedPlaylist?.RemoveTracks(TrackPathsInScope);
    }

    public void UpdatePlaylistContainmentStates()
    {
      UpdatePlaylistContainmentStates(SelectedPlaylist);
    }

    public void UpdateFromCache(FilesystemCache filesystemCache)
    {
      Playlists.Clear();
      MusicFolders.Clear();

      foreach (var kvp in filesystemCache.MusicCache)
      {
        var musicFolder = new MusicFolder(this, kvp.Key, kvp.Value);
        MusicFolders.Add(musicFolder);
      }

      foreach (var kvp in filesystemCache.PlaylistCache)
      {
        var playlist = new Playlist(this, kvp.Key, kvp.Value);
        Playlists.Add(playlist);
        playlist.SaveAsync();
      }

      UpdatePlaylistContainmentStates(SelectedPlaylist);
    }

    private void UpdatePlaylistContainmentStates(Playlist playlist)
    {
      foreach (var musicFolder in MusicFolders)
      {
        musicFolder.UpdatePlaylistContainmentState(playlist);
      }
    }

    private static void OnSelectedPlaylistChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      musicDrive.UpdatePlaylistContainmentStates(e.NewValue as Playlist);
    }
   
  }
}
