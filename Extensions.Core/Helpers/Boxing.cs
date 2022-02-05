namespace Extensions.Core.Helpers
{
  public static class Boxing
  {
    public static object Box<T>(T thing)
    {
      return thing;
    }
  }
}