using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using BlueAndMeManager.Core;
using WpfExtensions.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.Playlist>;

namespace BlueAndMeManager
{
  public class Playlist : DependencyObject
  {
    private readonly HashSet<string> _relativeFilePaths = new ();

    private string _fullPath;

    public MusicDrive MusicDrive { get; }

    public string FullPath
    {
      get => _fullPath;
      private set
      {
        if (_fullPath != null)
        {
          File.Move(_fullPath, value);
        }

        _fullPath = value;
      }
    }

    public IEnumerable<string> RelativeFilePaths => _relativeFilePaths;

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value.RemoveInvalidFileNameChars());
    }
    
    public Playlist(MusicDrive musicDrive, string name)
    {
      MusicDrive = musicDrive;
      Name = name;

      Initialize();
    }

    private void Initialize()
    {
      if (File.Exists(FullPath))
      {
        ReadFromFile();
      }
      else
      {
        File.Create(FullPath);
      }
    }

    public void AddTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _relativeFilePaths.Add(relativePath);
      }

      MusicDrive.UpdateIsInCurrentListMark();

      SaveToFile();
    }

    public void RemoveTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        _relativeFilePaths.Remove(relativePath);
      }

      MusicDrive.UpdateIsInCurrentListMark();

      SaveToFile();
    }

    public void Remove()
    {
      MusicDrive.Playlists.Remove(this);
      File.Delete(FullPath);
    }


    public void MarkIfContained(MusicFolder musicFolder)
    {
      var searchedFolderName = Path.GetFileName(musicFolder.FullPath);
      foreach (var relativePath in RelativeFilePaths)
      {
        var folderName = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[0];
        if (searchedFolderName == folderName)
        {
          musicFolder.IsInCurrentList = true;
          return;
        }
      }
    }

    public void MarkIfContained(Track track)
    {
      foreach (var relativePath in RelativeFilePaths)
      {
        if (track.FullPath.EndsWith(relativePath))
        {
          track.IsInCurrentList = true;
          return;
        }
      }
    }

    private void SaveToFile()
    {
      File.WriteAllLines(FullPath, RelativeFilePaths);
    }

    private void ReadFromFile()
    {
      _relativeFilePaths.Clear();

      foreach (var relativePath in File.ReadAllLines(FullPath))
      {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
          continue;
        }

        var fullPath = Path.Combine(MusicDrive.FullPath, relativePath);

        if (!File.Exists(fullPath))
        {
          continue;
        }

        _relativeFilePaths.Add(relativePath);
      }
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      var playlistFileName = e.NewValue + ".m3u";
      playlist.FullPath = Path.Combine(playlist.MusicDrive.FullPath, playlistFileName);
    }
  }
}
