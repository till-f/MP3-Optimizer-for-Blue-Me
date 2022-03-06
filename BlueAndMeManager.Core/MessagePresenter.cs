using System;
using System.Diagnostics;

namespace BlueAndMeManager.Core
{
  public class MessagePresenter
  {
    public delegate void OnProgress(double percent, string message);

    public delegate void OnError(string message);
    
    private static MessagePresenter _instance;

    private readonly OnProgress _onProgress;
    private readonly OnError _onError;

    public static void Init(OnProgress onProgress, OnError onError)
    {
      _instance = new MessagePresenter(onProgress, onError);
    }

    public static void ShowAndLogError(string message, Exception ex = null)
    {
#if DEBUG
      Debug.Print($"Error: {message}");
#endif

      Logger.LogError(message, ex);

      message += $"{Environment.NewLine}{Environment.NewLine}For details see log file: '{Logger.LogFilePath}'";
      _instance?._onError.Invoke(message);
    }

    public static void UpdateProgress(double percent, string message)
    {
#if DEBUG
      string percentPrefix = percent >= 0 ? string.Format("{000:0.0}%: ", percent) : string.Empty;
      Debug.Print(percentPrefix + message);
#endif

      _instance?._onProgress.Invoke(percent, message);
    }

    private MessagePresenter(OnProgress onProgress, OnError onError)
    {
      _onProgress = onProgress;
      _onError = onError;
    }
  }
}
