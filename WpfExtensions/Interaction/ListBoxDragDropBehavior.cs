using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Extensions.Wpf.Interaction
{
  public class ListBoxDragDropBehavior
  {
    private Point _startPos;
    private ListBoxItem _startDragItem;
    private ListBoxItem _lastMouseDownItem;
    private ListBoxItem _lastMouseDownAlreadySelectedItem;

    private readonly Action<DependencyObject> _onDragStarted;
    private readonly Action<object, DragEventArgs> _onDrop;

    public ListBoxDragDropBehavior(Action<DependencyObject> onDragStarted, Action<object, DragEventArgs> onDrop)
    {
      _onDragStarted = onDragStarted;
      _onDrop = onDrop;
    }

    public ListBoxDragDropBehavior Register(ListBox listBox)
    {
      var contentStyle = listBox.ItemContainerStyle ?? new Style(typeof(ContentControl));

      contentStyle.Setters.Add(new Setter(UIElement.AllowDropProperty, true));

      contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((sender, args) =>
      {
        if (sender is not ListBoxItem item)
        {
          return;
        }

        args.Handled = true;

        _lastMouseDownAlreadySelectedItem = null;
        _startDragItem = null;

        if (_lastMouseDownItem != null && listBox.SelectedItems.Count > 0 && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
        {
          var i1 = listBox.Items.IndexOf(_lastMouseDownItem.DataContext);
          var i2 = listBox.Items.IndexOf(item.DataContext);

          if (i1 == i2)
          {
            item.IsSelected = !item.IsSelected;
            return;
          }
          else if (i1 > i2)
          {
            (i1, i2) = (i2, i1);
          }

          listBox.UnselectAll();

          for (int i = i1; i <= i2; i++)
          {
            var otherItem = (ListBoxItem)listBox.ItemContainerGenerator.ContainerFromIndex(i);
            otherItem.IsSelected = true;
          }

          return;
        }

        _lastMouseDownItem = item;

        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        {
          item.IsSelected = !item.IsSelected;
          return;
        }

        if (!item.IsSelected)
        {
          listBox.UnselectAll();
          item.IsSelected = true;
          item.Focus();
        }
        else
        {
          _lastMouseDownAlreadySelectedItem = item;
        }

        _startDragItem = item;
        _startPos = args.GetPosition(null);
      })));

      contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((sender, args) =>
      {
        if (sender is not ListBoxItem item)
        {
          return;
        }

        args.Handled = true;

        _startDragItem = null;

        if (_lastMouseDownAlreadySelectedItem == item
            && !Keyboard.IsKeyDown(Key.LeftCtrl)
            && !Keyboard.IsKeyDown(Key.RightCtrl)
            && !Keyboard.IsKeyDown(Key.LeftShift)
            && !Keyboard.IsKeyDown(Key.RightShift))
        {
          listBox.UnselectAll();
          item.IsSelected = true;
        }
      })));
      
      contentStyle.Setters.Add(new EventSetter(UIElement.MouseMoveEvent, new MouseEventHandler((sender, args) =>
      {
        if (args.LeftButton != MouseButtonState.Pressed)
        {
          return;
        }

        args.Handled = true;

        var diff = _startPos - args.GetPosition(null);
        var isDraggedByMinimum = Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

        if (isDraggedByMinimum && _startDragItem != null)
        {
          _onDragStarted.Invoke(_startDragItem);
        }
      })));

      contentStyle.Setters.Add(new EventSetter(UIElement.DragEnterEvent, new DragEventHandler((sender, args) =>
      {
        if (sender is not ListBoxItem item)
        {
          return;
        }

        args.Handled = true;

        var i1 = listBox.Items.IndexOf(_startDragItem.DataContext);
        var i2 = listBox.Items.IndexOf(item.DataContext);

        Brush borderBrush;
        if (i1 == i2)
        {
          return;
        }
        else if (i2 < i1)
        {
          // dragged up
          borderBrush = new LinearGradientBrush(
            Colors.Transparent,
            Colors.RoyalBlue,
            new Point(0.5, 0.4),
            new Point(0.5, 0));
        }
        else
        {
          // dragged down
          borderBrush = new LinearGradientBrush(
            Colors.Transparent,
            Colors.RoyalBlue,
            new Point(0.5, 0.6),
            new Point(0.5, 1));
        }

        item.BorderBrush = borderBrush;
      })));

      contentStyle.Setters.Add(new EventSetter(UIElement.DragLeaveEvent, new DragEventHandler((sender, args) =>
      {
        if (sender is not ListBoxItem item)
        {
          return;
        }

        args.Handled = true;

        item.BorderBrush = null;

        // Point p = args.GetPosition(item);
        // p = item.TranslatePoint(p, Window.GetWindow(listBox));
        // Debug.Print($"Point: {p.Y}, ListHeight: {listBox.ActualHeight}, ListBox Point: {listBox.TranslatePoint(new Point(0, 0), Window.GetWindow(listBox)).Y}");
      })));

      contentStyle.Setters.Add(new EventSetter(UIElement.DropEvent, new DragEventHandler((sender, args) =>
      {
        if (sender is not ListBoxItem item)
        {
          return;
        }

        args.Handled = true;

        item.BorderBrush = null;

        _onDrop.Invoke(sender, args);
      })));

      listBox.ItemContainerStyle = contentStyle;

      return this;
    }
  }
}
