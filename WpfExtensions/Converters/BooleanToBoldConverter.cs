﻿using System;
using System.Windows;
using System.Windows.Data;

namespace WpfExtensions.Converters
{
  [ValueConversion(typeof(object), typeof(FontWeight))]
  public class BooleanToBoldConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(FontWeight))
      {
        throw new InvalidOperationException("The target must be Boolean");
      }

      return (bool)value! ? FontWeights.Bold : FontWeights.Normal;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}