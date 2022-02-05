namespace BlueAndMeManager.Core
{
  /// <summary>
  /// Used to report progress. Percentage may be smaller than zero to
  /// denote that overall progress cannot be determined.
  /// </summary>
  public delegate void OnProgress(double percent, string message);

  public delegate void OnError(string message);
}
