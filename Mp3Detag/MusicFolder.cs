using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<Mp3Detag.MusicFolder>;

namespace Mp3Detag
{
  public class MusicFolder : DependencyObject
  {
    public ObservableCollection<MusicFile> Files { get; } = new();

    public static readonly DependencyProperty FullPathProperty = RegisterProperty(musicFolder => musicFolder.FullPath).OnChange(OnFullPathChanged);

    public MusicDrive MusicDrive { get; }

    public string FullPath
    {
      get => (string)GetValue(FullPathProperty);
      set => SetValue(FullPathProperty, value);
    }

    public static readonly DependencyProperty SelectedFileProperty = RegisterProperty(musicFolder => musicFolder.SelectedFile);

    public MusicFile SelectedFile
    {
      get => (MusicFile)GetValue(SelectedFileProperty);
      set => SetValue(SelectedFileProperty, value);
    }

    public MusicFolder(MusicDrive musicDrive, string fullPath)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }

    private static void OnFullPathChanged(MusicFolder musicFolder, DependencyPropertyChangedEventArgs e)
    {
      var fullPath = (string)e.NewValue;

      musicFolder.Files.Clear();

      foreach (var musicFile in Directory.GetFiles(fullPath, "*.mp3").Select(s => new MusicFile(musicFolder, s)))
      {
        musicFolder.Files.Add(musicFile);
      }
    }
  }
}
