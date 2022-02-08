namespace BlueAndMeManager.Core
{
  /// <summary>
  /// Used to report progress. Percentage may be smaller than zero to
  /// denote that overall progress cannot be determined.
  /// </summary>
  public delegate void OnProgress(double percent, string message);

  public delegate void OnError(string message);

  public class MessagePresenter
  {
    private static MessagePresenter _instance;

    private readonly OnProgress _onProgress;
    private readonly OnError _onError;

    public static void Init(OnProgress onProgress, OnError onError)
    {
      _instance = new MessagePresenter(onProgress, onError);
    }

    public static void ShowError(string message)
    {
      _instance?._onError.Invoke(message);
    }

    public static void UpdateProgress(double percent, string message)
    {
      _instance?._onProgress.Invoke(percent, message);
    }

    private MessagePresenter(OnProgress onProgress, OnError onError)
    {
      _onProgress = onProgress;
      _onError = onError;
    }
  }
}
