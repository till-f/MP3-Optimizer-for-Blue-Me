using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Mp3Detag.Core;
using Path = System.IO.Path;

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
      var path = DrivePath.Text;

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
