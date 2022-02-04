using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using BlueAndMeManager.Core;
using Microsoft.Win32;
using WpfExtensions.Helpers;

namespace BlueAndMeManager
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
      var path = WorkingPath.Text;

      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
      {
        OnError($"Invalid path: {path}");
        return;
      }

      MusicDrive = new MusicDrive(path);
    }

    private void FixTagsButton_Click(object sender, RoutedEventArgs e)
    {
      TagFixer tagFixer;

      if (MusicDrive?.SelectedTracks?.Count > 0)
      {
        tagFixer = new TagFixer(MusicDrive.SelectedTracks.Select(file => file.FullPath), EFileSelectionMode.ExplicitFile, OnProgress, OnError);
      }
      else if (MusicDrive?.Tracks?.Count > 0)
      {
        tagFixer = new TagFixer(MusicDrive.Tracks.Select(file => file.FullPath), EFileSelectionMode.ExplicitFile, OnProgress, OnError);
      }
      else if (MusicDrive != null)
      {
        tagFixer = new TagFixer(new []{ MusicDrive.FullPath }, EFileSelectionMode.DirectoryRecursive, OnProgress, OnError);
      }
      else
      {
        OnError($"No path selected");
        return;
      }

      var task = tagFixer.RunAsync();

      task.OnCompletion(() => Dispatcher.Invoke(() => MusicDrive.RefreshTracks()));
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

      MusicDrive.Playlists.Add(new Playlist(MusicDrive, dialog.Value));
    }

    private void RemovePlaylistButton_Click(object sender, RoutedEventArgs e)
    {
      var playlist = PlaylistsBox.SelectedItem as Playlist;
      if (playlist == null)
      {
        return;
      }

      var playlistIndex = PlaylistsBox.SelectedIndex;

      playlist.Remove();

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
        ProgressBar.Value = percent;
      });
    }

    private void OnError(string message)
    {
      MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
