using System;
using System.Windows;
using System.Windows.Data;

namespace Extensions.Wpf.Converters
{
  [ValueConversion(typeof(object), typeof(Visibility))]
  public class TrueToCollapsedVisibilityConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(Visibility))
      {
        throw new InvalidOperationException("The target must be Visibility");
      }

      return value != null && (bool)value ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
