using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueAndMeManager.Core
{
  public static class PlaylistUpdater
  {
    public static void FilesMoved(string fullPath, IEnumerable<string> oldEntryPaths, Dictionary<string, string> oldNewMapping)
    {
      if (oldNewMapping.Count == 0)
      {
        return;
      }

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

    public static void Save(string fullPath, IEnumerable<string> entryPaths)
    {
      File.WriteAllLines(fullPath, entryPaths);
    }
  }
}
