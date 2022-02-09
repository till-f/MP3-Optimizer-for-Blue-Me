using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace BlueAndMeManager.Core
{
  public class RegistrySettings
  {
    private const string BlueAndMeManagerKey = "HKEY_CURRENT_USER\\SOFTWARE\\BlueAndMeManager";
    
    public static void SetLastPath(string lastPath)
    {
      Registry.SetValue(BlueAndMeManagerKey, "LastPath", lastPath);
    }

    public static string GetLastPath()
    {
      return Registry.GetValue(BlueAndMeManagerKey, "LastPath", "C:\\Musik") as string;
    }

  }
}
