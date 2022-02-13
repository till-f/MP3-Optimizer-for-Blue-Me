using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Extensions.Core;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.MusicFolder>;

namespace BlueAndMeManager.ViewModel
{
  public enum EPlaylistContainmentState
  {
    NotContained,
    PartiallyContained,
    CompletelyContained
  }

  public class MusicFolder : DependencyObject, ITracksContainer
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

        MusicDrive.TrackByFullPath[trackPath] = track;
      }
    }

    public void UpdatePlaylistContainmentState(bool includeTracks)
    {
      var isFolderContained = false;
      var areAllTracksContained = true;

      foreach (var track in Tracks)
      {
        if (includeTracks)
        {
          track.UpdatePlaylistContainmentState();
        }

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

    public void UpdateTracks(LinkedList<string> newTrackPaths)
    {
      _tracks.RemoveWhere(x => !newTrackPaths.Contains(x.FullPath));

      var lastIdx = 0;
      foreach (var newTrackPath in newTrackPaths)
      {
        var track = _tracks.Find(x => x.FullPath == newTrackPath);
        if (track == null)
        {
          track = new Track(this, newTrackPath);
          _tracks.Insert(lastIdx, track);
        }
        MusicDrive.TrackByFullPath[newTrackPath] = track;

        lastIdx++;
      }
    }
  }
}
