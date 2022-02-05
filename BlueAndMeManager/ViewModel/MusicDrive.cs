using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using Extensions.Wpf;
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

    public IEnumerable<string> TrackPathsInScope
    {
      get
      {
        if (SelectedTracks?.Count > 0)
        {
          return SelectedTracks.Select(file => file.FullPath);
        }
        else if (TracksInSelectedFolders?.Count > 0)
        {
          return TracksInSelectedFolders.Select(file => file.FullPath);
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

    public void RebuildCacheAsync(Dispatcher dispatcher, OnProgress onProgress, OnError onError)
    {
      var task = FilesystemCache.BuildAsync(FullPath, onProgress, onError);
      task.OnCompletion(dispatcher, () =>
      {
        if (task.Result != null)
        {
          UpdateFromCache(task.Result);
        }
      });
    }

    public void UpdateFromCache(FilesystemCache filesystemCache)
    {
      MusicFolders.Clear();

      foreach (var kvp in filesystemCache.MusicCache)
      {
        var musicFolder = new MusicFolder(this, kvp.Key, kvp.Value);
        MusicFolders.Add(musicFolder);
      }

      foreach (var kvp in filesystemCache.PlaylistCache)
      {
        var playlist = new Playlist(this, Path.GetFileNameWithoutExtension(kvp.Key), kvp.Value);
        Playlists.Add(playlist);
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
