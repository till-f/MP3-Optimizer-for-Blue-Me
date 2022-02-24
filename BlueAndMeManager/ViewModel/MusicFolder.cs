using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BlueAndMeManager.Core;
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

    public MusicFolder(MusicDrive musicDrive, string fullPath, IEnumerable<FilesystemCache.Track> coreTracks)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;

      foreach (var coreTrack in coreTracks)
      {
        var track = new Track(this, coreTrack);
        _tracks.Add(track);

        MusicDrive.TrackByFullPath[coreTrack.FullPath] = track;
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

    public void UpdateTracks(LinkedList<FilesystemCache.Track> newCoreTracks)
    {
      _tracks.RemoveWhere(track => !newCoreTracks.Select(coreTrack => coreTrack.FullPath).Contains(track.FullPath));

      var lastIdx = 0;
      foreach (var newCoreTrack in newCoreTracks)
      {
        var track = _tracks.Find(x => x.FullPath == newCoreTrack.FullPath);
        if (track == null)
        {
          track = new Track(this, newCoreTrack);
          _tracks.Insert(lastIdx, track);
        }
        MusicDrive.TrackByFullPath[newCoreTrack.FullPath] = track;

        lastIdx++;
      }
    }

    public void RemoveTracks(IEnumerable<Track> tracksToDelete)
    {
      foreach (var track in tracksToDelete)
      {
        _tracks.Remove(track);
      }
    }
  }
}
