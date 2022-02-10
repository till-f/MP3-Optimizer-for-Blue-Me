using System.Runtime.Versioning;
using Microsoft.Win32;

namespace BlueAndMeManager.Core
{
  public class RegistrySettings
  {
    private const string BlueAndMeManagerKey = "HKEY_CURRENT_USER\\SOFTWARE\\BlueAndMeManager";

    [SupportedOSPlatform("windows")]
    public static void SetLastPath(string lastPath)
    {
      Registry.SetValue(BlueAndMeManagerKey, "LastPath", lastPath);
    }

    [SupportedOSPlatform("windows")]
    public static string GetLastPath()
    {
      return Registry.GetValue(BlueAndMeManagerKey, "LastPath", "C:\\Musik") as string;
    }

  }
}
