using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BlueAndMeManager.Core
{
  public class FilesystemCache
  {
    private readonly string _rootPath;
    private readonly OnProgress _onProgress;
    private readonly OnError _onError;

    public Dictionary<string, LinkedList<string>> MusicCache { get; } = new ();

    public Dictionary<string, LinkedList<string>> PlaylistCache { get; } = new ();

    private FilesystemCache(string rootPath, OnProgress onProgress = null, OnError onError = null)
    {
      _rootPath = rootPath;
      _onProgress = onProgress;
      _onError = onError;
    }

    public static Task<FilesystemCache> BuildAsync(string rootPath, OnProgress onProgress, OnError onError)
    {
      FilesystemCache result = new FilesystemCache(rootPath, onProgress, onError);
      var task = new Task<FilesystemCache>(result.Build);
      task.Start();
      return task;
    }

    private FilesystemCache Build()
    {
      try
      {
        _onProgress?.Invoke(-1, "Reading music files...");
        foreach (var musicFolder in Directory.GetDirectories(_rootPath))
        {
          LinkedList<string> tracks = new();
          foreach (var track in Directory.GetFiles(musicFolder, "*.mp3", SearchOption.AllDirectories))
          {
            tracks.AddLast(track);
          }

          MusicCache[musicFolder] = tracks;
        }

        _onProgress?.Invoke(-1, "Reading playlists...");
        foreach (var playlist in Directory.GetFiles(_rootPath, "*.m3u", SearchOption.TopDirectoryOnly))
        {
          PlaylistCache[playlist] = PlaylistUpdater.ReadM3U(_rootPath, playlist);
        }

        return this;
      }
      catch (Exception e)
      {
        _onError?.Invoke($"{e.GetType().Name}: {e.Message}");
        return null;
      }
      finally
      {
        _onProgress?.Invoke(0, "Idle");
      }
    }

    public static Task DeleteFilesAsync(string rootPath, IEnumerable<string> filesToDelete, OnProgress onProgress, OnError onError)
    {
      var task = new Task(() =>
      {
        try
        {
          foreach (var trackPath in filesToDelete)
          {
            onProgress(0, $"Deleting {trackPath}...");
            File.Delete(trackPath);
          }

          CleanupFolders(rootPath);
        }
        catch (Exception e)
        {
          onError(e.Message);
        }
        finally
        {
          onProgress(0, "Idle");
        }
      });
      task.Start();

      return task;
    }

    public static void CleanupFolders(string rootPath)
    {
      foreach (var directory in Directory.GetDirectories(rootPath, "*", SearchOption.AllDirectories))
      {
        if (Directory.Exists(directory) && Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
        {
          Directory.Delete(directory, true);
        }
      }
    }

  }
}
