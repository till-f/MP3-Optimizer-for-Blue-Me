using System;
using System.Windows.Data;

namespace Extensions.Wpf.Converters
{
  [ValueConversion(typeof(object), typeof(bool))]
  public class ObjectRefToBooleanConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(bool))
      {
        throw new InvalidOperationException("The target must be Boolean");
      }

      return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
