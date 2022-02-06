using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BlueAndMeManager.ViewModel;
using Extensions.Wpf.Interaction;

namespace BlueAndMeManager.View
{
  /// <summary>
  /// Interaction logic for PromptDialog.xaml
  /// </summary>
  public partial class ReorderWindow : Window
  {
    private readonly Playlist _playlist;

    public ObservableCollection<PlaylistEntry> PlaylistEntries { get; } = new ();

    public ReorderWindow(Playlist playlist)
    {
      InitializeComponent();

      _playlist = playlist;

      foreach (var entryPath in _playlist.EntryPaths)
      {
        PlaylistEntries.Add(new PlaylistEntry(entryPath));
      }

      new ItemsControlDragDropBehavior(PlaylistsBox_OnDragStarted, PlaylistBox_OnDrop).Register(PlaylistsBox);
    }

    private void PlaylistsBox_OnDragStarted(DependencyObject source)
    {
      DragDrop.DoDragDrop(source, source, DragDropEffects.Move);
    }

    private void PlaylistBox_OnDrop(object sender, DragEventArgs dragEventArgs)
    {
      var source = (ListBoxItem) dragEventArgs.Data.GetData(typeof(ListBoxItem));
      var target = (ListBoxItem) sender;

      if (target.IsSelected)
      {
        return;
      }

      // ReSharper disable once PossibleNullReferenceException
      var sourceEntry = (PlaylistEntry) source.DataContext;
      var targetEntry = (PlaylistEntry) target.DataContext;
      var sourceIndex = PlaylistEntries.IndexOf(sourceEntry);
      var targetIndex = PlaylistEntries.IndexOf(targetEntry);

      // get entries to move in original order
      LinkedList<PlaylistEntry> entriesToMoveInOriginalOrder = new ();
      foreach (var entry in PlaylistEntries)
      {
        if (PlaylistsBox.SelectedItems.Contains(entry))
        {
          entriesToMoveInOriginalOrder.AddLast(entry);
        }
      }

      // remove from current position
      foreach (var selectedItem in entriesToMoveInOriginalOrder)
      {
        PlaylistEntries.Remove(selectedItem);
      }

      // calculate new start position
      var realTargetIndex = PlaylistEntries.IndexOf(targetEntry);
      if (targetIndex > sourceIndex)
      {
        realTargetIndex += 1;
      }

      // add at new position
      foreach (var entry in entriesToMoveInOriginalOrder)
      {
        PlaylistEntries.Insert(realTargetIndex++, entry);
      }
    }

    private void Ok_Click(object sender, RoutedEventArgs e)
    {
      _playlist.EntryPaths.Clear();

      foreach (var entry in PlaylistEntries)
      {
        _playlist.EntryPaths.Add(entry.RelativePath);
      }

      _playlist.Save();

      DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      DialogResult = false;
    }
  }
}
