using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using BlueAndMeManager.Core;

namespace BlueAndMeManager
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    public MusicDrive MusicDrive => DataContext as MusicDrive;

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

      DataContext = new MusicDrive(path);
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

      tagFixer.RunAsync();
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
        MusicDrive.SelectedFolders.Add((MusicFolder)folder);
      }

      foreach (var folder in e.RemovedItems)
      {
        MusicDrive.SelectedFolders.Remove((MusicFolder)folder);
      }

      MusicDrive.RefreshTracks();
    }

    private void TracksBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (MusicDrive == null)
      {
        return;
      }

      foreach (var folder in e.AddedItems)
      {
        MusicDrive.SelectedTracks.Add((MusicFile)folder);
      }

      foreach (var folder in e.RemovedItems)
      {
        MusicDrive.SelectedTracks.Remove((MusicFile)folder);
      }
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
