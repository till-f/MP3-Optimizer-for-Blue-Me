using System.IO;
using System.Windows;
using Mp3Detag.Core;

namespace Mp3Detag
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
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

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
      var path = WorkingPath.Text;

      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
      {
        OnError($"Invalid path: {path}");
        return;
      }

      var tagFixer = new TagFixer(path, EFileSelectionMode.DirectoryRecursive, OnProgress, OnError);

      tagFixer.RunAsync();
    }

    private void OnProgress(double perent, string message)
    {
      Dispatcher.InvokeAsync(() =>
      {
        StatusBar.Content = message;
        ProgressBar.Value = perent;
      });
    }

    private void OnError(string message)
    {
      MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
  }
}
