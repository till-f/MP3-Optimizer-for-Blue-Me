using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using BlueAndMeManager.ViewModel;
using Extensions.Wpf;
using Extensions.Wpf.Interaction;

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

      MessagePresenter.Init(OnProgress, OnError);

      new ListBoxDragDropBehavior(PlaylistsBox).ApplyDropTargetBehaviorToItems(PlaylistBox_OnDrop);
      new ListBoxDragDropBehavior(FoldersBox).ApplyDragSourceBehaviorToItems();
      new ListBoxDragDropBehavior(TracksBox).ApplyDragSourceBehaviorToItems();
    }

    private void PlaylistBox_OnDrop(ListBoxItem targetItem, DragEventArgs e)
    {
      var playlist = (Playlist)targetItem.DataContext;

      if (!targetItem.IsSelected)
      {
        var result =MessageBox.Show(this,
          $"The target playlist is not the active one.\n\nDo you really want to add the tracks to playlist '{playlist.Name}'?",
          "Add to Inactive Playlist",
          MessageBoxButton.YesNo,
          MessageBoxImage.Question,
          MessageBoxResult.Yes);

        if (result != MessageBoxResult.Yes)
        {
          return;
        }
      }

      var sourceItem = (ListBoxItem)e.Data.GetData(typeof(ListBoxItem));
      var listBox = sourceItem.FindVisualParent<ListBox>();

      List<string> trackPaths = new();

      foreach (var trackContainer in listBox.SelectedItems)
      {
        trackPaths.AddRange(((ITracksContainer)trackContainer).Tracks.Select(x => x.FullPath));
      }

      playlist.AddTracks(trackPaths);
      PlaylistsBox.SelectedItem = playlist;
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
      RebuildCacheAsync(rootPath, SkipMissingTracksCheckBox.IsChecked == true, Dispatcher);
    }

    private void FixTagsButton_Click(object sender, RoutedEventArgs e)
    {
      if (MusicDrive == null)
      {
        OnError($"No path selected");
        return;
      }

      var result =MessageBox.Show(this,
        $"CAUTION! This will overwrite the meta information (ID3 tags) of the {MusicDrive.TrackPathsInScope.Count()} selected files. Information may be removed or altered to fulfill Blue&Me restrictions!\n\nYour files might be renamed, but the loaded playlists will be updated accordingly.\n\nDo you want to continue?",
        "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      var rootPath = MusicDrive.FullPath;
      var playlists = MusicDrive.CorePlaylists;
      var fixer = new BlueAndMeFixer(rootPath, MusicDrive.TrackPathsInScope);
      var task = fixer.RunAsync();
      task.OnCompletion(() =>
      {
        foreach (var playlist in playlists)
        {
          MessagePresenter.UpdateProgress(-1, $"Updating playlist {playlist.Key}...");
          PlaylistUpdater.FormatFixerExecuted(playlist.Key, playlist.Value, task.Result);
        }

        RebuildCacheAsync(rootPath, false, Dispatcher);
      });
    }

    private void RebuildCacheAsync(string rootPath, bool skipMissingTracks, Dispatcher dispatcher)
    {
      var task = FilesystemHelper.BuildCacheAsync(rootPath, skipMissingTracks);
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

      UpdateTrackButtonsEnabledState();
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

      UpdateTrackButtonsEnabledState();
    }

    private void PlaylistsBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdateTrackButtonsEnabledState();
    }

    private void FoldersOrTracksBox_KeyDown(object sender, KeyEventArgs e)
    {
      if (e.Key == Key.Return)
      {
        MusicDrive.AddTracksInScopeToPlaylist();
      }

      if (e.Key == Key.Delete)
      {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
          AskAndDeleteTracksInScope();
        }
        else
        {
          MusicDrive.RemoveTracksInScopeFromPlaylist();
        }
      }
    }

    private void AddPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var dialog = new PromptDialog("This will create a new playlist. Please choose a short name for the playlist.",
        "Playlist name: ", "New Playlist")
      {
        Owner = this
      };

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

    private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
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
        "Playlist name: ", "New Playlist", playlist.Name)
      {
        Owner = this
      };

      var result = dialog.ShowDialog();

      if (!result.HasValue || !result.Value || string.IsNullOrWhiteSpace(dialog.Value))
      {
        return;
      }

      playlist.Name = dialog.Value;
    }
    
    private void ReorderPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var playlist = PlaylistsBox.SelectedItem as Playlist;
      if (playlist == null)
      {
        return;
      }

      var dialog = new ReorderWindow(playlist)
      {
        Owner = this,
        Title = $"Reorder Playlist {playlist.Name}"
      };

      dialog.ShowDialog();
    }

    private void AddTracksToPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      MusicDrive.AddTracksInScopeToPlaylist();
    }

    private void RemoveTracksFromPlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      MusicDrive.RemoveTracksInScopeFromPlaylist();
    }
    
    private void DeleteTracksFromDriveButton_Click(object sender, RoutedEventArgs e)
    {
      AskAndDeleteTracksInScope();
    }

    private void UpdateTrackButtonsEnabledState()
    {
      var canAddRemoveToPlaylist = PlaylistsBox.SelectedItem != null && FoldersBox.SelectedItems.Count > 0;

      AddToPlaylistButton.IsEnabled = canAddRemoveToPlaylist;
      RemoveFromPlaylistButton.IsEnabled = canAddRemoveToPlaylist;

      var canDeleteTracks = FoldersBox.SelectedItems.Count > 0 || TracksBox.SelectedItems.Count > 0;
      DeleteTracksButton.IsEnabled = canDeleteTracks;
    }

    private void AskAndDeleteTracksInScope()
    {
      if (FoldersBox.SelectedItems.Count == 0 && TracksBox.SelectedItems.Count == 0)
      {
        return;
      }

      var result = MessageBox.Show(this,
        $"Do you really want to delete the selected {MusicDrive.TrackPathsInScope.Count()} files?",
        "Delete Tracks",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.Yes);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      foreach (var playlist in MusicDrive.Playlists)
      {
        playlist.RemoveTracks(MusicDrive.TrackPathsInScope);
      }

      var rootPath = MusicDrive.FullPath;
      var task = FilesystemHelper.DeleteFilesAsync(rootPath, MusicDrive.TrackPathsInScope.ToList());
      task.OnCompletion(() => RebuildCacheAsync(rootPath, false, Dispatcher));
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
      Debug.Print($"Error: {message}");
      Dispatcher.InvokeAsync(() =>
      {
        MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      });
    }
  }
}
