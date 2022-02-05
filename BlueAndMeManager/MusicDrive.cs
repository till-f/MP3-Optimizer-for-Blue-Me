using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using BlueAndMeManager.Core;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.MusicDrive>;

namespace BlueAndMeManager
{
  public class MusicDrive : DependencyObject
  {
    public static readonly DependencyProperty FullPathProperty = RegisterProperty(x => x.FullPath).OnChange(OnFullPathChanged);

    public string FullPath
    {
      get => (string)GetValue(FullPathProperty);
      set => SetValue(FullPathProperty, value);
    }

    public static readonly DependencyProperty SelectedPlaylistProperty = RegisterProperty(x => x.SelectedPlaylist).OnChange(OnSelectedPlaylistChanged);

    public Playlist SelectedPlaylist
    {
      get => (Playlist)GetValue(SelectedPlaylistProperty);
      set => SetValue(SelectedPlaylistProperty, value);
    }

    public ObservableCollection<Playlist> Playlists { get; } = new();

    public ObservableCollection<MusicFolder> MusicFolders { get; } = new ();

    public ObservableCollection<Track> Tracks { get; } = new();

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
        else if (Tracks?.Count > 0)
        {
          return Tracks.Select(file => file.FullPath);
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
      Tracks.Clear();

      foreach (var folder in SelectedMusicFolders)
      {
        foreach (var track in Directory.GetFiles(folder.FullPath, "*.mp3", SearchOption.AllDirectories).Select(s => new Track(folder, s)))
        {
          SelectedPlaylist?.MarkIfContained(track);
          Tracks.Add(track);
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

    public void UpdateIsInCurrentListMark()
    {
      UpdateIsInCurrentListMark(SelectedPlaylist);
    }

    private static void OnFullPathChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      var rootFolder = (string)e.NewValue;

      musicDrive.MusicFolders.Clear();

      foreach (var musicFolder in Directory.GetDirectories(rootFolder).Select(s => new MusicFolder(musicDrive, s)))
      {
        musicDrive.SelectedPlaylist?.MarkIfContained(musicFolder);
        musicDrive.MusicFolders.Add(musicFolder);
      }

      foreach (var playlist in Directory.GetFiles(rootFolder, "*.m3u", SearchOption.TopDirectoryOnly).Select(s => new Playlist(musicDrive, Path.GetFileNameWithoutExtension(s))))
      {
        musicDrive.Playlists.Add(playlist);
      }
    }

    private static void OnSelectedPlaylistChanged(MusicDrive musicDrive, DependencyPropertyChangedEventArgs e)
    {
      musicDrive.UpdateIsInCurrentListMark(e.NewValue as Playlist);
    }
    
    private void UpdateIsInCurrentListMark(Playlist playlist)
    {
      ClearMarkedItems();

      if (playlist == null)
      {
        return;
      }

      var folderNames = new HashSet<string>();
      foreach (var relativePath in playlist.RelativeFilePaths)
      {
        var folderName = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[0];
        folderNames.Add(folderName);

        MarkTrack(relativePath);
      }

      foreach (var folderName in folderNames)
      {
        MarkFolder(folderName);
      }
    }

    private void ClearMarkedItems()
    {
      foreach (var track in Tracks)
      {
        track.IsInCurrentList = false;
      }

      foreach (var musicFolder in MusicFolders)
      {
        musicFolder.IsInCurrentList = false;
      }
    }

    private void MarkTrack(string relativePath)
    {
      var track = Tracks.FirstOrDefault(track => track.FullPath.EndsWith(relativePath));
      if (track != null)
      {
        track.IsInCurrentList = true;
      }
    }

    private void MarkFolder(string folderName)
    {
      var musicFolder = MusicFolders.FirstOrDefault(musicFolder => Path.GetFileName(musicFolder.FullPath) == folderName);
      if (musicFolder != null)
      {
        musicFolder.IsInCurrentList = true;
      }
    }
  }
}
