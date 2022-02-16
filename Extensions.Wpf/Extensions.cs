using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Extensions.Wpf
{
  public static class Extensions
  {
    public static Task OnCompletion(this Task t, Dispatcher dispatcher, Action action)
    {
      var task = new Task(() =>
      {
        // we are not doing t.Wait() here because this would silently catch any
        // exception that caused the task to terminate and any global excepiton
        // handler would not have a chance to intercept.
        ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
        t = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        dispatcher.Invoke(action);
      });
        
      task.Start();
      return task;
    }

    public static Task OnCompletion(this Task t, Action action)
    {
      var task = new Task(() =>
      {
        // we are not doing t.Wait() here because this would silently catch any
        // exception that caused the task to terminate and any global excepiton
        // handler would not have a chance to intercept.
        ((IAsyncResult)t).AsyncWaitHandle.WaitOne();
        t = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        action.Invoke();
      });
      
      task.Start();
      return task;
    }

    public static T FindVisualChild<T>(this DependencyObject obj) where T : DependencyObject
    {
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

    public static T FindVisualParent<T>(this DependencyObject dependencyObject) where T : DependencyObject
    {
      var parent = VisualTreeHelper.GetParent(dependencyObject);
      if (parent == null) return null;
      var parentT = parent as T;
      return parentT ?? FindVisualParent<T>(parent);
    }
  }
}
