using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfExtensions.Helpers
{
  public static class Extensions
  {
    public static void OnCompletion(this Task t, Action a)
    {
      new Task(() =>
      {
        t.Wait();
        a.Invoke();
      }).Start();
    }
  }
}
