using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using BlueAndMeManager.ViewModel;
using Extensions.Wpf;

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

      var result =MessageBox.Show(this,
        "This will rewrite ID3 tags from the selected files. Data not supported by ID3v1 will be removed! Your files might be renamed and moved. Existing playlists will be updated accordingly. Do you want to continue?",
        "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      var rootPath = MusicDrive.FullPath;
      var playlists = MusicDrive.CorePlaylists;
      var fixer = new BlueAndMeFixer(rootPath, MusicDrive.TrackPathsInScope, OnProgress, OnError);
      var task = fixer.RunAsync();
      task.OnCompletion(() =>
      {
        foreach (var playlist in playlists)
        {
          OnProgress(-1, $"Updating playlist {playlist.Key}...");
          PlaylistUpdater.FormatFixerExecuted(playlist.Key, playlist.Value, task.Result);
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
      dialog.Owner = this;

      var result = dialog.ShowDialog();

      if (!result.HasValue || !result.Value || string.IsNullOrWhiteSpace(dialog.Value))
      {
        return;
      }

      var fullPath = PlaylistUpdater.GetFullPath(MusicDrive.FullPath, dialog.Value.RemoveInvalidFileNameChars());
      var newPlaylist = new Playlist(MusicDrive, fullPath);
      MusicDrive.Playlists.Add(newPlaylist);
      PlaylistsBox.SelectedItem = newPlaylist;
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
      dialog.Owner = this;

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
      MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
