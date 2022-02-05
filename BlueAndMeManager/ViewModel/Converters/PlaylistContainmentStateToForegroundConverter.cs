using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace BlueAndMeManager.ViewModel.Converters
{
  [ValueConversion(typeof(object), typeof(FontWeight))]
  public class PlaylistContainmentStateToForegroundConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(Brush))
      {
        throw new InvalidOperationException("The target must be Boolean");
      }

      switch ((EPlaylistContainmentState)value!)
      {
        case EPlaylistContainmentState.NotContained:
          return new SolidColorBrush(Colors.Gray);
        case EPlaylistContainmentState.PartiallyContained:
        case EPlaylistContainmentState.CompletelyContained:
          return new SolidColorBrush(Colors.Black);
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      throw new NotSupportedException();
    }
  }
}
