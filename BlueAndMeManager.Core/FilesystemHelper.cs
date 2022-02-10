using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlueAndMeManager.Core
{
  public class FilesystemCache
  {
    public Dictionary<string, LinkedList<string>> MusicCache { get; } = new();

    public Dictionary<string, LinkedList<string>> PlaylistCache { get; } = new();
  }

  public class FilesystemHelper
  {
    public static bool CancelRequested { get; set; }

    public static Task<FilesystemCache> BuildCacheAsync(string rootPath, bool skipMissingTracks)
    {
      var task = new Task<FilesystemCache>(() =>
      {
        var cache = new FilesystemCache();
        try
        {
          MessagePresenter.UpdateProgress(-1, "Reading music files...");
          foreach (var musicFolder in Directory.GetDirectories(rootPath))
          {
            LinkedList<string> tracks = new();
            foreach (var track in Directory.GetFiles(musicFolder, "*.mp3", SearchOption.AllDirectories))
            {
              tracks.AddLast(track);
            }

            cache.MusicCache[musicFolder] = tracks;
          }

          MessagePresenter.UpdateProgress(-1, "Reading playlists...");
          foreach (var playlist in Directory.GetFiles(rootPath, "*.m3u", SearchOption.TopDirectoryOnly))
          {
            cache.PlaylistCache[playlist] = PlaylistUpdater.ReadM3U(rootPath, playlist, skipMissingTracks);
          }

          return cache;
        }
        catch (Exception e)
        {
          MessagePresenter.ShowError(e.Message);
          return null;
        }
        finally
        {
          MessagePresenter.UpdateProgress(0, "Idle");
        }
      });

      task.Start();
      return task;
    }

    public static Task DeleteFilesAsync(string rootPath, IEnumerable<string> filesToDelete)
    {
      var task = new Task(() =>
      {
        try
        {
          double ctr = 0;
          var trackPaths = filesToDelete.ToArray();

          foreach (var trackPath in trackPaths)
          {
            if (CancelRequested)
            {
              return;
            }

            MessagePresenter.UpdateProgress(ctr++ / trackPaths.Length * 100, $"Deleting {trackPath}...");
            File.Delete(trackPath);
          }

          CleanupFolders(rootPath);
        }
        catch (Exception e)
        {
          MessagePresenter.ShowError(e.Message);
        }
        finally
        {
          MessagePresenter.UpdateProgress(0, "Idle");
        }
      });

      task.Start();
      return task;
    }

    public static void CleanupFolders(string rootPath)
    {
      foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
      {
        if (Directory.Exists(directory) && Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories).Length == 0)
        {
          Directory.Delete(directory, true);
        }
      }
    }
  }
}
