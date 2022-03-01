using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using BlueAndMeManager.Core;
using BlueAndMeManager.ViewModel;
using Extensions.Wpf;
using Extensions.Wpf.Interaction;
using Microsoft.WindowsAPICodePack.Dialogs;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.View.MainWindow>;

namespace BlueAndMeManager.View
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public static readonly DependencyProperty IsLockedProperty = RegisterProperty(x => x.IsLocked).Default(false).OnChange(OnIsLockedChanged);

    public bool IsLocked
    {
      get => (bool)GetValue(IsLockedProperty);
      set => SetValue(IsLockedProperty, value);
    }

    public static readonly DependencyProperty CanCancelProperty = RegisterProperty(x => x.CanCancel).Default(false).OnChange(OnCanCancelChanged);

    public bool CanCancel
    {
      get => (bool)GetValue(CanCancelProperty);
      set => SetValue(CanCancelProperty, value);
    }

    public static readonly DependencyProperty CancelButtonVisibilityProperty = RegisterProperty(x => x.CancelButtonVisibility).Default(Visibility.Collapsed).Coerce(CoerceCancelButtonVisibility);

    public Visibility CancelButtonVisibility
    {
      get => (Visibility)GetValue(CancelButtonVisibilityProperty);
      set => SetValue(CancelButtonVisibilityProperty, value);
    }

    public MusicDrive MusicDrive
    {
      get => DataContext as MusicDrive;
      set => DataContext = value;
    }

    public MainWindow()
    {
      InitializeComponent();

      MessagePresenter.Init(OnProgress, OnError);
      TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

      new ListBoxDragDropBehavior(PlaylistsBox).ApplyDropTargetBehaviorToItems(PlaylistBox_OnDrop);
      new ListBoxDragDropBehavior(FoldersBox).ApplyDragSourceBehaviorToItems();
      new ListBoxDragDropBehavior(TracksBox).ApplyDragSourceBehaviorToItems();

      WorkingPath.Text = RegistrySettings.GetLastPath();
      Title += " v" + GetType().Assembly.GetName().Version.ToString();
    }

    private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
    {
      if (e?.Exception?.InnerException is Exception innerExeption)
      {
        MessagePresenter.ShowError(innerExeption.Message);
      }
      else
      {
        MessagePresenter.ShowError(e?.Exception?.Message);
      }
    }

    private void PlaylistBox_OnDrop(ListBoxItem targetItem, DragEventArgs e)
    {
      if (IsLocked)
      {
        return;
      }

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

      List<Track> tracks = new();

      foreach (var trackContainer in listBox.SelectedItems)
      {
        tracks.AddRange(((ITracksContainer)trackContainer).Tracks);
      }

      playlist.AddTracks(tracks);
      PlaylistsBox.SelectedItem = playlist;
    }

    private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
    {
      var startPath = WorkingPath.Text;

      var dialog = new CommonOpenFileDialog()
      {
        InitialDirectory = startPath,
        IsFolderPicker = true
      };

      var result = dialog.ShowDialog();
      if (result != CommonFileDialogResult.Ok)
      {
        return;
      }

      WorkingPath.Text = dialog.FileName;

      OpenButton_Click(sender, e);
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
      var rootPath = WorkingPath.Text;

      if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
      {
        OnError($"Invalid path: {rootPath}");
        return;
      }

      IsLocked = true;
      RegistrySettings.SetLastPath(rootPath);
      MusicDrive = new MusicDrive(rootPath);
      var task = RebuildCacheAsync(rootPath, SkipMissingTracksCheckBox.IsChecked == true, Dispatcher);
      task.OnCompletion(Dispatcher, () => IsLocked = false);
    }

    private void FixTagsButton_Click(object sender, RoutedEventArgs e)
    {
      if (MusicDrive == null)
      {
        OnError($"No path selected");
        return;
      }

      if (RenameFilesCheckBox.IsChecked == true)
      {
        FoldersBox.SelectedItems.Clear();
      }

      var fixer = new BlueAndMeFixer(MusicDrive.FullPath, MusicDrive.TracksInScope.Select(x => x.FullPath), RenameFilesCheckBox.IsChecked == true, QuickRunCheckBox.IsChecked == true);

      var result =MessageBox.Show(this,
        $"CAUTION! This will overwrite the meta information (ID3 tags)! Some information may be removed or altered to fulfill Blue&Me restrictions!\n\n{fixer.AffectedFilesCount} files will be affected.\n\nDo you want to continue?",
        "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      IsLocked = true;
      CanCancel = true;
      var rootPath = MusicDrive.FullPath;
      var playlists = MusicDrive.CorePlaylists;
      var fixerTask = fixer.RunAsync();
      fixerTask.OnCompletion(() =>
      {
        foreach (var playlist in playlists)
        {
          MessagePresenter.UpdateProgress(-1, $"Updating playlist {playlist.Key}...");
          PlaylistService.FormatFixerExecuted(playlist.Key, playlist.Value, fixerTask.Result);
        }

        var rebuildTask = RebuildCacheAsync(rootPath, false, Dispatcher);
        rebuildTask.OnCompletion(Dispatcher, () => IsLocked = false);
      });
    }

    private void CancelWork_Clicked(object sender, RoutedEventArgs e)
    {
      ManagerService.CancelSource?.Cancel();
      CanCancel = false;
    }

    private Task RebuildCacheAsync(string rootPath, bool skipMissingTracks, Dispatcher dispatcher)
    {
      var task = ManagerService.BuildCacheAsync(rootPath, skipMissingTracks);
      return task.OnCompletion(dispatcher, () =>
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
      if (IsLocked)
      {
        return;
      }

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
      var dialog = new PromptDialog(
        "The available space for the playlist name is limited in most Blue&Me head units, so it is recommended to use a short name for the playlist (about 12 characters max).",
        "Name: ", "New Playlist")
      {
        Owner = this
      };

      var result = dialog.ShowDialog();

      if (!result.HasValue || !result.Value || string.IsNullOrWhiteSpace(dialog.Value))
      {
        return;
      }

      var fullPath = PlaylistService.GetFullPath(MusicDrive.FullPath, dialog.Value.RemoveInvalidFileNameChars());
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

      var isDeleted = playlist.Delete();
      if (!isDeleted)
      {
        return;
      }

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

      var dialog = new PromptDialog("The available space for the playlist name is limited in most Blue&Me head units, so it is recommended to use a short name for the playlist (about 12 characters max).",
        "New name: ", "Rename Playlist", playlist.Name)
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
        $"Do you really want to delete the selected {MusicDrive.TracksInScope.Count()} files?",
        "Delete Tracks",
        MessageBoxButton.YesNo,
        MessageBoxImage.Warning,
        MessageBoxResult.Yes);

      if (result != MessageBoxResult.Yes)
      {
        return;
      }

      IsLocked = true;
      CanCancel = true;

      foreach (var playlist in MusicDrive.Playlists)
      {
        playlist.RemoveTracks(MusicDrive.TracksInScope);
      }

      var rootPath = MusicDrive.FullPath;
      var tracksToDelete = MusicDrive.TracksInScope.ToList();
      var deleteTask = ManagerService.DeleteFilesAsync(rootPath, tracksToDelete.Select(x => x.FullPath));
      deleteTask.OnCompletion(Dispatcher, () =>
      {
        if (deleteTask.IsCanceled)
        {
          var rebuildTask = RebuildCacheAsync(rootPath, false, Dispatcher);
          rebuildTask.OnCompletion(Dispatcher, () => IsLocked = false);
        }
        else
        {
          foreach (var track in tracksToDelete)
          {
            MusicDrive.TrackByFullPath.Remove(track.FullPath);
          }
          foreach (var folder in MusicDrive.MusicFolders)
          {
            folder.RemoveTracks(tracksToDelete);
          }
          MusicDrive.RefreshTracks();
          IsLocked = false;
        }
      });
    }

    private static void OnIsLockedChanged(MainWindow window, DependencyPropertyChangedEventArgs e)
    {
      if ((bool)e.NewValue)
      {
        window.CanCancel = false;
      }
      ManagerService.CancelSource = null;

      window.CoerceValue(CancelButtonVisibilityProperty);
    }

    private static void OnCanCancelChanged(MainWindow window, DependencyPropertyChangedEventArgs e)
    {
      window.CoerceValue(CancelButtonVisibilityProperty);
    }

    private static Visibility CoerceCancelButtonVisibility(MainWindow window, Visibility value)
    {
      return window.IsLocked ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnProgress(double percent, string message)
    {
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
      Dispatcher.InvokeAsync(() =>
      {
        MessageBox.Show(this, message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
      });
    }
  }
}
