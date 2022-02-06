using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using BlueAndMeManager.ViewModel;
using Extensions.Wpf;
using Track = BlueAndMeManager.ViewModel.Track;

namespace BlueAndMeManager.View
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MusicDrive MusicDrive
    {
      get => DataContext as MusicDrive;
      set => DataContext = value;
    }

    public MainWindow()
    {
      InitializeComponent();
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
      var rootPath = WorkingPath.Text;

      if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
      {
        OnError($"Invalid path: {rootPath}");
        return;
      }

      MusicDrive = new MusicDrive(rootPath);
      RebuildCacheAsync(rootPath, Dispatcher, OnProgress, OnError);
    }

    private void FixTagsButton_Click(object sender, RoutedEventArgs e)
    {
      if (MusicDrive == null)
      {
        OnError($"No path selected");
        return;
      }

      var rootPath = MusicDrive.FullPath;
      var playlists = MusicDrive.CorePlaylists;
      var tagFixer = new TagFixer(rootPath, MusicDrive.TrackPathsInScope, OnProgress, OnError);
      var task = tagFixer.RunAsync();
      task.OnCompletion(() =>
      {
        foreach (var playlist in playlists)
        {
          OnProgress(-1, $"Updating playlist {playlist.Key}...");
          PlaylistUpdater.FilesMoved(playlist.Key, playlist.Value, task.Result);
        }

        RebuildCacheAsync(rootPath, Dispatcher, OnProgress, OnError);
      });
    }

    private void RebuildCacheAsync(string rootPath, Dispatcher dispatcher, OnProgress onProgress, OnError onError)
    {
      var task = FilesystemCache.BuildAsync(rootPath, onProgress, onError);
      task.OnCompletion(dispatcher, () =>
      {
        if (task.Result != null)
        {
          MusicDrive.UpdateFromCache(task.Result);
        }
      });
    }

    private void FoldersBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (MusicDrive == null)
      {
        return;
      }

      TracksBox.SelectedItems.Clear();

      foreach (var folder in e.AddedItems)
      {
        MusicDrive.SelectedMusicFolders.Add((MusicFolder)folder);
      }

      foreach (var folder in e.RemovedItems)
      {
        MusicDrive.SelectedMusicFolders.Remove((MusicFolder)folder);
      }

      MusicDrive.RefreshTracks();

      UpdateEditPlaylistButtonsEnabledState();
    }

    private void TracksBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (MusicDrive == null)
      {
        return;
      }

      foreach (var folder in e.AddedItems)
      {
        MusicDrive.SelectedTracks.Add((Track)folder);
      }

      foreach (var folder in e.RemovedItems)
      {
        MusicDrive.SelectedTracks.Remove((Track)folder);
      }
    }

    private void PlaylistsBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdateEditPlaylistButtonsEnabledState();
    }

    private void AddPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new PromptDialog("This will create a new playlist. Please choose a short name for the playlist.",
        "Playlist name: ", "New Playlist");

      var result = dialog.ShowDialog();

      if (!result.HasValue || !result.Value || string.IsNullOrWhiteSpace(dialog.Value))
      {
        return;
      }

      MusicDrive.Playlists.Add(new Playlist(MusicDrive, dialog.Value.RemoveInvalidFileNameChars()));
    }

    private void RemovePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var playlist = PlaylistsBox.SelectedItem as Playlist;
      if (playlist == null)
      {
        return;
      }

      var playlistIndex = PlaylistsBox.SelectedIndex;

      playlist.Delete();

      if (playlistIndex >= PlaylistsBox.Items.Count)
      {
        playlistIndex--;
      }

      PlaylistsBox.SelectedIndex = playlistIndex;
    }

    private void RenamePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var playlist = PlaylistsBox.SelectedItem as Playlist;
      if (playlist == null)
      {
        return;
      }

      var dialog = new PromptDialog("Please choose a short name for the playlist.",
        "Playlist name: ", "New Playlist", playlist.Name);

      var result = dialog.ShowDialog();

      if (!result.HasValue || !result.Value || string.IsNullOrWhiteSpace(dialog.Value))
      {
        return;
      }

      playlist.Name = dialog.Value;
    }

    private void AddTracksToPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      MusicDrive.AddTracksInScopeToPlaylist();
    }

    private void RemoveTracksFromPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      MusicDrive.RemoveTracksInScopeFromPlaylist();
    }

    private void UpdateEditPlaylistButtonsEnabledState()
    {
      var isEnabled = PlaylistsBox.SelectedItem != null && FoldersBox.SelectedItems.Count > 0;

      AddToPlaylistButton.IsEnabled = isEnabled;
      RemoveFromPlaylistButton.IsEnabled = isEnabled;
    }

    private void OnProgress(double percent, string message)
    {
      Debug.Print(message);
      Dispatcher.InvokeAsync(() =>
      {
        StatusBar.Content = message;
        if (percent < 0)
        {
          ProgressBar.IsIndeterminate = true;
        }
        else
        {
          ProgressBar.IsIndeterminate = false;
          ProgressBar.Value = percent;
        }
      });
    }

    private void OnError(string message)
    {
      MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
