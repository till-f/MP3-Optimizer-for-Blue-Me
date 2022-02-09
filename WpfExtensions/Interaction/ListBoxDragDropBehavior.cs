using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Extensions.Wpf.Interaction
{
  public class ListBoxDragDropBehavior
  {
    public enum EDropTargetKind
    {
      OnItem,
      BetweenItems
    }

    private readonly ListBox _listBox;

    // only used for DragSource
    private Action<ListBox, ListBoxItem> _customOnDrag;
    private Point _startPos;
    private ListBoxItem _startDragItem;
    private ListBoxItem _lastMouseDownItem;
    private ListBoxItem _lastMouseDownAlreadySelectedItem;

    // only used for DropTarget
    private Action<ListBoxItem, DragEventArgs> _onDrop;
    private EDropTargetKind _dropTargetKind;

    public ListBoxDragDropBehavior(ListBox listBox)
    {
      _listBox = listBox;
    }

    public ListBoxDragDropBehavior ApplyDragSourceBehaviorToItems(Action<ListBox, ListBoxItem> customOnDrag = null)
    {
      _customOnDrag = customOnDrag;

      _listBox.SelectionMode = SelectionMode.Extended;

      var contentStyle = _listBox.ItemContainerStyle ?? new Style(typeof(ContentControl));
      contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown)));
      contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonUp)));
      contentStyle.Setters.Add(new EventSetter(UIElement.MouseMoveEvent, new MouseEventHandler(ListBoxItem_MouseMove)));
      _listBox.ItemContainerStyle = contentStyle;

      return this;
    }

    public ListBoxDragDropBehavior ApplyDropTargetBehaviorToItems(Action<ListBoxItem, DragEventArgs> onDrop, EDropTargetKind dropTargetKind = EDropTargetKind.OnItem)
    {
      _onDrop = onDrop;
      _dropTargetKind = dropTargetKind;

      var contentStyle = _listBox.ItemContainerStyle ?? new Style(typeof(ContentControl));
      contentStyle.Setters.Add(new Setter(UIElement.AllowDropProperty, true));
      contentStyle.Setters.Add(new Setter(Control.BorderBrushProperty, new SolidColorBrush(Colors.Transparent)));
      contentStyle.Setters.Add(new EventSetter(UIElement.DragOverEvent, new DragEventHandler(ListBoxItem_DragOver)));
      contentStyle.Setters.Add(new EventSetter(UIElement.DragEnterEvent, new DragEventHandler(ListBoxItem_DragOver)));
      contentStyle.Setters.Add(new EventSetter(UIElement.DragLeaveEvent, new DragEventHandler(ListBoxItem_DragLeave)));
      contentStyle.Setters.Add(new EventSetter(UIElement.DropEvent, new DragEventHandler(ListBoxItem_Drop)));
      _listBox.ItemContainerStyle = contentStyle;

      _listBox.DragOver += ListBox_DragOver;

      return this;
    }

    private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
      if (sender is not ListBoxItem item)
      {
        return;
      }

      e.Handled = true;

      item.Focus();
      _lastMouseDownAlreadySelectedItem = null;
      _startDragItem = null;

      if (_lastMouseDownItem != null && _listBox.SelectedItems.Count > 0 && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)))
      {
        var i1 = _listBox.Items.IndexOf(_lastMouseDownItem.DataContext);
        var i2 = _listBox.Items.IndexOf(item.DataContext);

        if (i1 == i2)
        {
          item.IsSelected = !item.IsSelected;
          return;
        }
        else if (i1 > i2)
        {
          (i1, i2) = (i2, i1);
        }

        _listBox.UnselectAll();

        for (int i = i1; i <= i2; i++)
        {
          var otherItem = (ListBoxItem)_listBox.ItemContainerGenerator.ContainerFromIndex(i);
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
        _listBox.UnselectAll();
        item.IsSelected = true;
        item.Focus();
      }
      else
      {
        _lastMouseDownAlreadySelectedItem = item;
      }

      _startDragItem = item;
      _startPos = e.GetPosition(null);
    }

    private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
      if (sender is not ListBoxItem item)
      {
        return;
      }

      e.Handled = true;

      _startDragItem = null;

      if (_lastMouseDownAlreadySelectedItem == item
          && !Keyboard.IsKeyDown(Key.LeftCtrl)
          && !Keyboard.IsKeyDown(Key.RightCtrl)
          && !Keyboard.IsKeyDown(Key.LeftShift)
          && !Keyboard.IsKeyDown(Key.RightShift))
      {
        _listBox.UnselectAll();
        item.IsSelected = true;
      }
    }

    private void ListBoxItem_MouseMove(object sender, MouseEventArgs e)
    {
      if (e.LeftButton != MouseButtonState.Pressed)
      {
        return;
      }

      e.Handled = true;

      var diff = _startPos - e.GetPosition(null);
      var isDraggedByMinimum = Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                               Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance;

      if (isDraggedByMinimum && _startDragItem != null)
      {
        if (_customOnDrag != null)
        {
          _customOnDrag?.Invoke(_listBox, _startDragItem);
        }
        else
        {
          DragDrop.DoDragDrop(_startDragItem, _startDragItem, DragDropEffects.Move);
        }
      }
    }

    private void ListBoxItem_DragOver(object sender, DragEventArgs e)
    {
      if (sender is not ListBoxItem item)
      {
        return;
      }

      var pos = e.GetPosition(item);
      item.BorderBrush = GetBrushForKind(_dropTargetKind, pos.Y < item.ActualHeight / 2);
    }

    private void ListBoxItem_DragLeave(object sender, DragEventArgs e)
    {
      if (sender is not ListBoxItem item)
      {
        return;
      }

      e.Handled = true;
      item.BorderBrush = new SolidColorBrush(Colors.Transparent);
    }

    private void ListBoxItem_Drop(object sender, DragEventArgs e)
    {
      if (sender is not ListBoxItem item)
      {
        return;
      }

      e.Handled = true;
      item.BorderBrush = new SolidColorBrush(Colors.Transparent);
      _onDrop.Invoke(item, e);
    }

    private void ListBox_DragOver(object sender, DragEventArgs e)
    {
      if (sender is not ListBox listBox)
      {
        return;
      }

      e.Handled = true;

      var sv = listBox.FindVisualChild<ScrollViewer>();

      double tolerance = 10;
      double verticalPos = e.GetPosition(listBox).Y;
      double offset = 3;

      if (verticalPos < tolerance)
      {
        //Top of visible list, scroll up
        sv.ScrollToVerticalOffset(sv.VerticalOffset - offset);
      }
      else if (verticalPos > listBox.ActualHeight - tolerance)
      {
        //Bottom of visible list, scroll down
        sv.ScrollToVerticalOffset(sv.VerticalOffset + offset);
      }
    }

    private static Brush GetBrushForKind(EDropTargetKind kind, bool isDraggingInUpperHalf)
    {
      var borderColor1 = Colors.RoyalBlue;
      var borderColor2 = Colors.RoyalBlue;
      borderColor2.A = 80;

      switch (kind)
      {
        case EDropTargetKind.OnItem:
          return new LinearGradientBrush(
            new GradientStopCollection(3) 
              {
              new (borderColor1, 0),
              new (borderColor2, 0.5),
              new (borderColor1, 1)
              }, 
            new Point(0, 0.5), 
            new Point(1, 0.5)
              );
        case EDropTargetKind.BetweenItems:
          if (isDraggingInUpperHalf)
          {
            // dragged up
            return new LinearGradientBrush(
              Colors.Transparent,
              borderColor1,
              new Point(0.5, 0.4),
              new Point(0.5, 0));
          }
          else
          {
            // dragged down
            return new LinearGradientBrush(
              Colors.Transparent,
              borderColor1,
              new Point(0.5, 0.6),
              new Point(0.5, 1));
          }
        default:
          throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
      }

    }
  }
}
