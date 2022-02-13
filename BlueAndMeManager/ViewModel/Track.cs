using System.Collections.Generic;
using System.IO;
using System.Windows;
using Extensions.Core.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Track>;

namespace BlueAndMeManager.ViewModel
{
  public class Track : DependencyObject, ITracksContainer
  {
    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public string RelativePath { get; }

    public IEnumerable<Track> Tracks => new[] { this };

    public static readonly DependencyProperty PlaylistContainmentStateProperty = RegisterProperty(x => x.PlaylistContainmentState).Coerce(CoercePlaylistContainmentStateProperty);

    public static EPlaylistContainmentState CoercePlaylistContainmentStateProperty(Track track, EPlaylistContainmentState visibility)
    {
      var isContained = track.MusicFolder.MusicDrive.SelectedPlaylist?.Contains(track) ?? false;
      return isContained ? EPlaylistContainmentState.CompletelyContained : EPlaylistContainmentState.NotContained;
    }

    public EPlaylistContainmentState PlaylistContainmentState
    {
      get => (EPlaylistContainmentState)GetValue(PlaylistContainmentStateProperty);
    }

    public Track(MusicFolder musicFolder, string fullPath)
    {
      MusicFolder = musicFolder;
      FullPath = fullPath;
      RelativePath = Utilities.GetRelativePath(musicFolder.MusicDrive.FullPath, fullPath);
    }

    public void UpdatePlaylistContainmentState()
    {
      CoerceValue(PlaylistContainmentStateProperty);
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }
  }
}
