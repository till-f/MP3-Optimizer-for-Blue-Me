using System.Windows;
using System.Windows.Input;
using static WpfExtensions.DependencyProperties.DependencyPropertyRegistrar<BlueAndMeManager.PromptDialog>;


namespace BlueAndMeManager
{
  /// <summary>
  /// Interaction logic for PromptDialog.xaml
  /// </summary>
  public partial class PromptDialog : Window
  {
    public static readonly DependencyProperty MessageProperty = RegisterProperty(x => x.Message);

    public string Message
    {
      get => (string)GetValue(MessageProperty);
      set => SetValue(MessageProperty, value);
    }

    public static readonly DependencyProperty QueryProperty = RegisterProperty(x => x.Query);

    public string Query
    {
      get => (string)GetValue(QueryProperty);
      set => SetValue(QueryProperty, value);
    }

    public static readonly DependencyProperty ValueProperty = RegisterProperty(x => x.Value);

    public string Value
    {
      get => (string)GetValue(ValueProperty);
      set => SetValue(ValueProperty, value);
    }

    public PromptDialog(string message, string query = null, string title = "Query", string initialValue = "")
    {
      InitializeComponent();

      Message = message;
      Title = title;
      Value = initialValue;
      Query = query;
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }

    private void Window_OnLoaded(object sender, RoutedEventArgs e)
    {
      ValueTextBox.Focus();
      ValueTextBox.SelectAll();
      //MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
    }
  }
}
