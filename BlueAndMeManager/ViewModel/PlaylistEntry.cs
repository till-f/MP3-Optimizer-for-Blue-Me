
using System.Windows;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.ViewModel.PlaylistEntry>;

namespace BlueAndMeManager.ViewModel
{
  public class PlaylistEntry : DependencyObject
  {
    public string RelativePath { get; }

    public static readonly DependencyProperty IsSelectedProperty = RegisterProperty(x => x.IsSelected);

    public bool IsSelected
    {
      get => (bool)GetValue(IsSelectedProperty);
      set => SetValue(IsSelectedProperty, value);
    }

    public PlaylistEntry(string relativePath)
    {
      RelativePath = relativePath;
    }

    public override string ToString()
    {
      return RelativePath;
    }
  }
}
