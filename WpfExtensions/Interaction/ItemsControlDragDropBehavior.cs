using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Extensions.Wpf.Interaction
{
  public class ItemsControlDragDropBehavior
  {
    private Point _startPos;
    private DependencyObject _startDragObject;

    private readonly Action<DependencyObject> _onDragStarted;
    private readonly Action<object, DragEventArgs> _onDrop;

    public ItemsControlDragDropBehavior(Action<DependencyObject> onDragStarted, Action<object, DragEventArgs> onDrop)
    {
      _onDragStarted = onDragStarted;
      _onDrop = onDrop;
    }

    public ItemsControlDragDropBehavior Register(ItemsControl itemsControl)
    {
      // itemsControl.MouseMove += OnMouseMove;

      var contentStyle = itemsControl.ItemContainerStyle ?? new Style(typeof(ContentControl));

      contentStyle.Setters.Add(new Setter(UIElement.AllowDropProperty, true));

      contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonDownEvent, new MouseButtonEventHandler((sender, args) =>
        {
          if (sender is ListBoxItem item)
          {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            {
              // let multiselect be handled by WPF
              return;
            }
            
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
              item.IsSelected = !item.IsSelected;
              args.Handled = true;
            }
            else if (item.IsSelected)
            {
              _startPos = args.GetPosition(null);
              _startDragObject = item;
              item.IsSelected = true;
              args.Handled = true;
            }
          }
        })));

      contentStyle.Setters.Add(new EventSetter(UIElement.MouseMoveEvent, new MouseEventHandler((sender, args) =>
      {
        var diff = _startPos - args.GetPosition(null);

        var isDraggedByMinimum = args.LeftButton == MouseButtonState.Pressed &&
                                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                                 Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance);

        if (isDraggedByMinimum && _startDragObject != null)
        {
          _onDragStarted.Invoke(_startDragObject);
          _startDragObject = null;
        }
      })));

      // contentStyle.Setters.Add(new EventSetter(UIElement.PreviewMouseLeftButtonUpEvent, new MouseButtonEventHandler((sender, args) =>
      // {
      //   if (sender is ListBoxItem item)
      //   {
      //     if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
      //     {
      //       item.IsSelected = !item.IsSelected;
      //       args.Handled = true;
      //     }
      //   }
      // })));

      contentStyle.Setters.Add(new EventSetter(UIElement.DropEvent, new DragEventHandler(_onDrop.Invoke)));

      itemsControl.ItemContainerStyle = contentStyle;

      return this;
    }

    // public void OnMouseMove(object sender, MouseEventArgs e)
    // {
    //   var diff = _startPos - e.GetPosition(null);
    //
    //   var isDraggedByMinmum = e.LeftButton == MouseButtonState.Pressed &&
    //                            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
    //                            Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance);
    //
    //   if (isDraggedByMinmum)
    //   {
    //     _onDragStarted.Invoke(_startDragObject);
    //   }
    // }
  }
}
