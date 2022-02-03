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

    private void ApplyButton_Click(object sender, RoutedEventArgs e)
    {
      var path = WorkingPath.Text;

      if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
      {
        MessageBox.Show("Invalid Path");
        return;
      }

      var tagFixer = new TagFixer(path, EFileSelectionMode.DirectoryRecursive);

      tagFixer.Run();
    }
  }
}
