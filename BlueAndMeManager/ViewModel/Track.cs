using System.IO;
using System.Windows;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Track>;

namespace BlueAndMeManager.ViewModel
{
  public class Track : DependencyObject, IPlaylistItem
  {
    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public static readonly DependencyProperty EPlaylistContainmentStateProperty = RegisterProperty(x => x.PlaylistContainmentState);

    public EPlaylistContainmentState PlaylistContainmentState
    {
      get => (EPlaylistContainmentState)GetValue(EPlaylistContainmentStateProperty);
      private set => SetValue(EPlaylistContainmentStateProperty, value);
    }

    public Track(MusicFolder musicFolder, string fullPath)
    {
      MusicFolder = musicFolder;
      FullPath = fullPath;
    }

    public void UpdatePlaylistContainmentState(Playlist playlist)
    {
      PlaylistContainmentState = playlist?.Contains(this) == true
        ? EPlaylistContainmentState.CompletelyContained
        : EPlaylistContainmentState.NotContained;
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }
  }
}
