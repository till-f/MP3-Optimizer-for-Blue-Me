using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BlueAndMeManager.Core
{
  public class FilesystemCache
  {
    public class Track
    {
      public string FullPath { get; set; }
      public string Album { get; set; }
      public string Artist { get; set; }
      public string Title { get; set; }
      public uint TrackNr { get; set; }
      public string Genre { get; set; }
    }

    public Dictionary<string, LinkedList<Track>> MusicCache { get; } = new();

    public Dictionary<string, LinkedList<string>> PlaylistCache { get; } = new();
  }

  public class ManagerService
  {
    public static CancellationTokenSource CancelSource { get; set; }

    public static Task<FilesystemCache> BuildCacheAsync(string rootPath, bool skipMissingTracks)
    {
      var task = new Task<FilesystemCache>(() =>
      {
        var cache = new FilesystemCache();
        try
        {
          MessagePresenter.UpdateProgress(0, "Reading music files...");

          double allFilesCount = Directory.GetFiles(rootPath, "*.mp3", SearchOption.AllDirectories).Length;
          var processedFilesCount = 0;

          foreach (var musicFolder in Directory.GetDirectories(rootPath))
          {
            LinkedList<FilesystemCache.Track> tracks = new();
            foreach (var mp3FilePath in Directory.GetFiles(musicFolder, "*.mp3", SearchOption.AllDirectories))
            {
              MessagePresenter.UpdateProgress(processedFilesCount++ / allFilesCount * 100, $"Reading file {mp3FilePath}...");

              var mp3File = TagLib.File.Create(mp3FilePath);

              var track = new FilesystemCache.Track()
              {
                FullPath = mp3FilePath,
                Album = mp3File.Tag.Album,
                Artist = mp3File.Tag.FirstPerformer,
                Title = mp3File.Tag.Title,
                TrackNr = mp3File.Tag.Track,
                Genre = mp3File.Tag.FirstGenre
              };
              tracks.AddLast(track);
            }

            cache.MusicCache[musicFolder] = tracks;
          }

          MessagePresenter.UpdateProgress(-1, "Reading playlists...");
          foreach (var playlist in Directory.GetFiles(rootPath, "*.m3u", SearchOption.TopDirectoryOnly))
          {
            cache.PlaylistCache[playlist] = PlaylistService.ReadM3U(rootPath, playlist, skipMissingTracks);
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
      CancelSource = new CancellationTokenSource();

      var task = new Task(() =>
      {
        try
        {
          double ctr = 0;
          var trackPaths = filesToDelete.ToArray();

          foreach (var trackPath in trackPaths)
          {
            if (CancelSource != null && CancelSource.IsCancellationRequested)
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
      }, CancelSource.Token);

      task.Start();
      return task;
    }

    public static void CleanupFolders(string rootPath)
    {
      var errors = false;
      foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
      {
        if (Directory.Exists(directory) && Directory.GetFiles(directory, "*.mp3", SearchOption.AllDirectories).Length == 0)
        {
          try
          {
            Directory.Delete(directory, true);
          }
          catch
          {
            errors = true;
          }
        }
      }

      if (errors)
      {
        MessagePresenter.ShowError($"Could not remove obsolete folders. You may have to delete some folders manually.");
      }
    }
  }
}
