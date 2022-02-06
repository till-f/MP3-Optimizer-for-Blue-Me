using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Extensions.Wpf
{
  public static class Extensions
  {
    public static void OnCompletion(this Task t, Dispatcher dispatcher, Action action)
    {
      new Task(() =>
      {
        t.Wait();
        dispatcher.Invoke(action);
      }).Start();
    }

    public static void OnCompletion(this Task t, Action action)
    {
      new Task(() =>
      {
        t.Wait();
        action.Invoke();
      }).Start();
    }

    public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
    {
      // Search immediate children first (breadth-first)
      for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
      {
        DependencyObject child = VisualTreeHelper.GetChild(obj, i);

        if (child != null && child is T)
          return (T)child;

        else
        {
          T childOfChild = FindVisualChild<T>(child);

          if (childOfChild != null)
            return childOfChild;
        }
      }

      return null;
    }
  }
}
