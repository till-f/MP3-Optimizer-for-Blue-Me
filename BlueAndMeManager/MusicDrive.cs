using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using WpfExtensions.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.MusicDrive>;

namespace BlueAndMeManager
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

    public Task RebuildCacheAsync(Dispatcher dispatcher)
    {
      var musicFolders = new LinkedList<MusicFolder>();
      var playlists = new LinkedList<Playlist>();

      var task = new Task(() => RebuildCache(musicFolders, playlists));

      task.OnCompletion(dispatcher, () =>
      {
        UpdateUiAfterRebuildCacheCompletion(musicFolders, playlists);
      });

      task.Start();

      return task;
    }

    private void RebuildCache(LinkedList<MusicFolder> musicFolders, LinkedList<Playlist> playlists)
    {
      foreach (var musicFolder in Directory.GetDirectories(FullPath).Select(s => new MusicFolder(this, s)))
      {
        musicFolder.RebuildCache();
        musicFolders.AddLast(musicFolder);
      }

      foreach (var playlist in Directory.GetFiles(FullPath, "*.m3u", SearchOption.TopDirectoryOnly).Select(s => new Playlist(this, Path.GetFileNameWithoutExtension(s))))
      {
        playlists.AddLast(playlist);
      }
    }

    public void UpdateUiAfterRebuildCacheCompletion(LinkedList<MusicFolder> musicFolders, LinkedList<Playlist> playlists)
    {
      MusicFolders.Clear();

      foreach (var musicFolder in musicFolders)
      {
        MusicFolders.Add(musicFolder);
      }

      foreach (var playlist in playlists)
      {
        Playlists.Add(playlist);
      }

      UpdatePlaylistContainmentStates(SelectedPlaylist);
    }

    private static void OnSelectedPlaylistChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      musicDrive.UpdatePlaylistContainmentStates(e.NewValue as Playlist);
    }
    
    private void UpdatePlaylistContainmentStates(Playlist playlist)
    {
      foreach (var musicFolder in MusicFolders)
      {
        musicFolder.UpdatePlaylistContainmentState(playlist);
      }
    }
  }
}
