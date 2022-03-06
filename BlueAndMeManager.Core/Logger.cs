using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueAndMeManager.Core
{
  public class Logger
  {
    private static Logger _instance = new ();

    public static string LogFilePath => _instance._logFilePath;

    public static void LogError(string message, Exception ex)
    {
      _instance.LogErrorInternal(message, ex);
    }

    private readonly string _logFilePath;

    private Logger()
    {
      try
      {
        _logFilePath = Path.GetFullPath(DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".log");
        File.WriteAllText(_logFilePath, "");
      }
      catch
      {
        _logFilePath = Path.GetTempFileName();
      }
    }

    private void LogErrorInternal(string message, Exception ex)
    {
      StringBuilder sb = new();

      if (message != null)
      {
        sb.AppendLine(message);
      }

      if (ex != null)
      {
        sb.AppendLine(ex.GetType().FullName);
        sb.AppendLine(ex.StackTrace);
        sb.AppendLine();
      }

      File.AppendAllText(_logFilePath, sb.ToString());
    }
  }
}
