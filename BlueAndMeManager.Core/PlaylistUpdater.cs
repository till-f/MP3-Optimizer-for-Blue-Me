using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Extensions.Core.Helpers;

namespace BlueAndMeManager.Core
{
  public static class PlaylistUpdater
  { 
    // trailing whitespaces to hide the extension on the car screen
    private const string PlaylistExtension = "            .m3u";

    public static string GetFullPath(string rootPath, string name)
    {
      return Path.Combine(rootPath, name + PlaylistExtension);
    }

    public static string Rename(string oldFullPath, string newName)
    {
      // ReSharper disable once PossibleNullReferenceException
      var rootPath = Directory.GetParent(oldFullPath).FullName;
      var newFullPath = GetFullPath(rootPath, newName);

      if (oldFullPath != newFullPath)
      {
        File.Move(oldFullPath, newFullPath);
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

        Save(fullPath, entryPaths);
      }

      Rename(fullPath, Path.GetFileNameWithoutExtension(fullPath).Trim());
    }

    public static LinkedList<string> ReadM3U(string rootPath, string fullPlaylistPath)
    {
      LinkedList<string> entries = new();

      foreach (var line in File.ReadAllLines(fullPlaylistPath))
      {
        var playlistName = Path.GetFileName(fullPlaylistPath);

        if (string.IsNullOrWhiteSpace(line))
        {
          continue;
        }

        if (line.StartsWith("#"))
        {
          throw new Exception($"Error in playlist {playlistName}: Extended M3U not supported.");
        }

        var relativePath = line;
        if (Path.IsPathRooted(relativePath))
        {
          try
          {
            relativePath = Utilities.GetRelativePath(rootPath, relativePath);
          }
          catch (Exception e)
          {
            throw new Exception($"Error in playlist {playlistName}: {e.Message}", e);
          }
        }

        var fullPath = Path.Combine(rootPath, relativePath);
        if (!File.Exists(fullPath))
        {
          throw new Exception($"Error in playlist {playlistName}: File does not exist: '{line}'.");
        }

        entries.AddLast(relativePath);
      }

      return entries;
    }

    public static void Save(string fullPath, IEnumerable<string> entryPaths)
    {
      File.WriteAllLines(fullPath, entryPaths);
    }
  }
}
