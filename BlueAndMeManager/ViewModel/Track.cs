using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using BlueAndMeManager.Core;
using Extensions.Core.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Track>;

namespace BlueAndMeManager.ViewModel
{
  public class Track : DependencyObject, ITracksContainer, IComparable
  {
    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int StrCmpLogicalW(string psz1, string psz2);

    public MusicFolder MusicFolder { get; }

    public string FullPath { get; }

    public string RelativePath { get; }

    public string Album { get; }

    public string Artist { get; }

    public string Title { get; }

    public uint TrackNr { get; }

    public string Genre { get; }

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

    public Track(MusicFolder musicFolder, FilesystemCache.Track coreTrack)
    {
      MusicFolder = musicFolder;

      FullPath = coreTrack.FullPath;
      Album = coreTrack.Album;
      Artist = coreTrack.Artist;
      Title = coreTrack.Title;
      TrackNr = coreTrack.TrackNr;
      Genre = coreTrack.Genre;

      RelativePath = Utilities.GetRelativePath(musicFolder.MusicDrive.FullPath, coreTrack.FullPath);
    }

    public void UpdatePlaylistContainmentState()
    {
      CoerceValue(PlaylistContainmentStateProperty);
    }

    public override string ToString()
    {
      return $"{TrackNr} - {Artist} - {Title}";
    }

    public int CompareTo(object obj)
    {
      return StrCmpLogicalW(ToString(), obj.ToString());
    }
  }
}
