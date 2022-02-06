using System.Collections.Generic;
using System.IO;
using System.Windows;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.MusicFolder>;

namespace BlueAndMeManager.ViewModel
{
  public enum EPlaylistContainmentState
  {
    NotContained,
    PartiallyContained,
    CompletelyContained
  }

  public class MusicFolder : DependencyObject, IPlaylistItem
  {
    private readonly List<Track> _tracks = new ();

    public MusicDrive MusicDrive { get; }

    public string FullPath { get; }

    public IEnumerable<Track> Tracks => _tracks;

    public static readonly DependencyProperty EPlaylistContainmentStateProperty = RegisterProperty(x => x.PlaylistContainmentState);

    public EPlaylistContainmentState PlaylistContainmentState
    {
      get => (EPlaylistContainmentState)GetValue(EPlaylistContainmentStateProperty);
      private set => SetValue(EPlaylistContainmentStateProperty, value);
    }

    public MusicFolder(MusicDrive musicDrive, string fullPath, IEnumerable<string> trackPaths)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;

      foreach (var trackPath in trackPaths)
      {
        var track = new Track(this, trackPath);
        _tracks.Add(track);
      }
    }

    public void UpdatePlaylistContainmentState(Playlist playlist)
    {
      var isFolderContained = false;
      var areAllTracksContained = true;

      foreach (var track in Tracks)
      {
        track.UpdatePlaylistContainmentState(playlist);

        if (track.PlaylistContainmentState == EPlaylistContainmentState.CompletelyContained)
        {
          isFolderContained = true;
        }
        else
        {
          areAllTracksContained = false;
        }
      }

      if (!isFolderContained)
      {
        PlaylistContainmentState = EPlaylistContainmentState.NotContained;
      }
      else if (areAllTracksContained)
      {
        PlaylistContainmentState = EPlaylistContainmentState.CompletelyContained;
      }
      else
      {
        PlaylistContainmentState = EPlaylistContainmentState.PartiallyContained;
      }
    }

    public override string ToString()
    {
      return Path.GetFileName(FullPath);
    }
  }
}
