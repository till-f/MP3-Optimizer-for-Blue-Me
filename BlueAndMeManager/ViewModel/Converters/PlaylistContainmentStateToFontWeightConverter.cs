using System;
using System.Windows;
using System.Windows.Data;

namespace BlueAndMeManager.ViewModel.Converters
{
  [ValueConversion(typeof(object), typeof(FontWeight))]
  public class PlaylistContainmentStateToFontWeightConverter : IValueConverter
  {
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
      if (targetType != typeof(FontWeight))
      {
        throw new InvalidOperationException("The target must be Boolean");
      }

      switch ((EPlaylistContainmentState)value!)
      {
        case EPlaylistContainmentState.NotContained:
        case EPlaylistContainmentState.PartiallyContained:
          return FontWeights.Normal;
        case EPlaylistContainmentState.CompletelyContained:
          return FontWeights.Bold;
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
