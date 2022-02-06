﻿using System;
using System.Threading.Tasks;
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
  }
}