using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Extensions.Core.Helpers;

namespace BlueAndMeManager.Core
{
  public static class PlaylistService
  { 
    // trailing whitespaces to hide the extension on the car screen
    private const string PlaylistExtension = "            .m3u";

    // lock to ensure that only one playlist is saved at a time (changes executed one-by-one)
    private static readonly object SaveLock = new ();

    public static string GetFullPath(string rootPath, string name)
    {
      return Path.Combine(rootPath, name + PlaylistExtension);
    }

    public static string CreateOrRename(string oldFullPath, string newName)
    {
      // ReSharper disable once PossibleNullReferenceException
      var rootPath = Directory.GetParent(oldFullPath).FullName;
      var newFullPath = GetFullPath(rootPath, newName);

      lock (SaveLock)
      {
        if (File.Exists(oldFullPath) && oldFullPath != newFullPath)
        {
          File.Move(oldFullPath, newFullPath);
        }
        else if (!File.Exists(newFullPath))
        {
          File.Create(newFullPath);
        }
      }

      return newFullPath;
    }

    public static void FormatFixerExecuted(string fullPath, IEnumerable<string> oldEntryPaths, Dictionary<string, string> oldNewMapping)
    {
      if (oldNewMapping.Count > 0)
      {
        var entryPaths = oldEntryPaths.ToList();

        for (var i = 0; i < entryPaths.Count; i++)
        {
          if (oldNewMapping.ContainsKey(entryPaths[i]))
          {
            entryPaths[i] = oldNewMapping[entryPaths[i]];
          }
        }

        SaveAsync(fullPath, entryPaths);
      }

      CreateOrRename(fullPath, Path.GetFileNameWithoutExtension(fullPath).Trim());
    }

    public static LinkedList<string> ReadM3U(string rootPath, string fullPlaylistPath, bool skipMissingTracks)
    {
      LinkedList<string> entries = new();

      foreach (var line in File.ReadAllLines(fullPlaylistPath))
      {
        var playlistName = Path.GetFileNameWithoutExtension(fullPlaylistPath).Trim();

        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        if (line.StartsWith("#"))
        {
          throw new Exception($"Error in playlist '{playlistName}':\nExtended M3U not supported.");
        }

        var relativePath = line.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(relativePath))
        {
          try
          {
            relativePath = Utilities.GetRelativePath(rootPath, relativePath);
          }
          catch (Exception e)
          {
            throw new Exception($"Error in playlist '{playlistName}':\n{e.Message}", e);
          }
        }

        var fullPath = Path.Combine(rootPath, relativePath);

        if (File.Exists(fullPath))
        {
          entries.AddLast(relativePath);
        }
        else if (!skipMissingTracks)
        {
          throw new Exception($"Error in playlist '{playlistName}':\nFile does not exist: '{line}'.");
        }
      }

      return entries;
    }

    public static Task SaveAsync(string fullPath, IEnumerable<string> entryPaths)
    {
      var task = new Task(() =>
      {
        try
        {
          MessagePresenter.UpdateProgress(-1, $"Saving Playlist {fullPath}");
          lock (SaveLock)
          {
            File.WriteAllLines(fullPath, entryPaths);
          }
        }
        catch (Exception ex)
        {
          MessagePresenter.ShowAndLogError($"Could not save playlist '{fullPath}': {ex.Message}", ex);
        }
        finally
        {
          MessagePresenter.UpdateProgress(0, "Idle");
        }
      });
      
      task.Start();
      return task;
    }

    public static bool Delete(string fullPath)
    {
      try
      {
        MessagePresenter.UpdateProgress(-1, $"Deleting Playlist {fullPath}");

        lock (SaveLock)
        {
          File.Delete(fullPath);
        }

        return true;
      }
      catch (Exception ex)
      {
        MessagePresenter.ShowAndLogError($"Could not delete '{fullPath}': {ex.Message}", ex);
        return false;
      }
      finally
      {
        MessagePresenter.UpdateProgress(0, "Idle");
      }
    }
  }
}
