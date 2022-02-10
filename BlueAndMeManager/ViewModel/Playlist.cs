using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using BlueAndMeManager.Core;
using Extensions.Core;
using Extensions.Core.Helpers;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.Playlist>;

namespace BlueAndMeManager.ViewModel
{
  public class Playlist : DependencyObject
  {
    public MusicDrive MusicDrive { get; }

    public string FullPath { get; private set; }

    public ObservableCollection<string> EntryPaths { get; } = new ();

    public static readonly DependencyProperty NameProperty = RegisterProperty(x => x.Name).OnChange(OnNameChanged);

    public string Name
    {
      get => (string)GetValue(NameProperty);
      set => SetValue(NameProperty, value);
    }
    
    public Playlist(MusicDrive musicDrive, string fullPath, IEnumerable<string> entryPaths = null)
    {
      MusicDrive = musicDrive;
      Name = Path.GetFileNameWithoutExtension(fullPath).Trim();

      // fake rename (will add spaces to the file name)
      FullPath = PlaylistUpdater.Rename(fullPath, Name);

      if (!File.Exists(FullPath))
      {
        File.Create(FullPath);
      }

      if (entryPaths == null)
      {
        return;
      }

      foreach (var entryPath in entryPaths)
      {
        EntryPaths.Add(entryPath);
      }
    }

    public void AddTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        if (!EntryPaths.Contains(relativePath))
        {
          EntryPaths.Add(relativePath);
        }
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      SaveAsync();
    }

    public void RemoveTracks(IEnumerable<string> trackPaths)
    {
      foreach (var trackPath in trackPaths)
      {
        var relativePath = Utilities.GetRelativePath(MusicDrive.FullPath, trackPath);
        EntryPaths.Remove(relativePath);
      }

      MusicDrive.UpdatePlaylistContainmentStates();

      SaveAsync();
    }

    public Task SaveAsync()
    {
      return PlaylistUpdater.SaveAsync(FullPath, EntryPaths);
    }

    public void Delete()
    {
      MusicDrive.Playlists.Remove(this);
      File.Delete(FullPath);
    }

    public bool Contains(Track track)
    {
      foreach (var entryPath in EntryPaths)
      {
        if (track.FullPath.EndsWith(entryPath))
        {
          return true;
        }
      }

      return false;
    }

    private static void OnNameChanged(Playlist playlist, DependencyPropertyChangedEventArgs e)
    {
      if (e.OldValue == null)
      {
        return;
      }

      playlist.FullPath = PlaylistUpdater.Rename(playlist.FullPath, (string) e.NewValue);
    }

    public void UpdateTracks(LinkedList<string> newEntryPaths)
    {
      EntryPaths.RemoveWhere(x => !newEntryPaths.Contains(x));

      var lastIdx = 0;
      foreach (var newEntryPath in newEntryPaths)
      {
        if (!EntryPaths.Contains(newEntryPath))
        {
          EntryPaths.Insert(lastIdx, newEntryPath);
        }
        lastIdx++;
      }
    }
  }
}
