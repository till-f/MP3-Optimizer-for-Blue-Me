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
