using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.MusicFolder>;

namespace BlueAndMeManager
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

    public MusicFolder(MusicDrive musicDrive, string fullPath)
    {
      MusicDrive = musicDrive;
      FullPath = fullPath;
    }

    public void RebuildCache()
    {
      foreach (var track in Directory.GetFiles(FullPath, "*.mp3", SearchOption.AllDirectories).Select(s => new Track(this, s)))
      {
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
