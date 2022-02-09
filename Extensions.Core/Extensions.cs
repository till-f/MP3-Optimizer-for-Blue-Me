using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extensions.Core
{
  public static class Extensions
  {
    public static void RemoveWhere<T>(this ICollection<T> collection, Func<T, bool> predicate)
    {
      foreach (var element in collection.ToArray())
      {
        if (predicate(element))
        {
          collection.Remove(element);
        }
      }
    }
  }
}
