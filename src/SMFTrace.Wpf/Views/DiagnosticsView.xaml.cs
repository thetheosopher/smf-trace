using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SMFTrace.Wpf.ViewModels;

namespace SMFTrace.Wpf.Views;

/// <summary>
/// Interaction logic for DiagnosticsView.xaml
/// </summary>
public partial class DiagnosticsView : UserControl
{
    private DiagnosticsViewModel? _viewModel;
    private bool _isSynchronizingPlaybackSelection;

    public DiagnosticsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        DataContextChanged += OnDataContextChanged;
        EventList.SelectionChanged += OnEventListSelectionChanged;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        SubscribeToViewModel(DataContext as DiagnosticsViewModel);
    }

    private void OnUnloaded(object sender, System.Windows.RoutedEventArgs e)
    {
        SubscribeToViewModel(null);
    }

    private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
    {
        if (IsLoaded)
        {
            SubscribeToViewModel(e.NewValue as DiagnosticsViewModel);
        }
    }

    private void SubscribeToViewModel(DiagnosticsViewModel? viewModel)
    {
        if (ReferenceEquals(_viewModel, viewModel))
        {
            return;
        }

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = viewModel;
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DiagnosticsViewModel.PlaybackActiveEvents))
        {
            SynchronizePlaybackSelection();
            CenterPlaybackSelection();
            return;
        }
    }

    private void SynchronizePlaybackSelection()
    {
        if (_viewModel == null)
        {
            return;
        }

        _isSynchronizingPlaybackSelection = true;
        try
        {
            EventList.UnselectAll();

            if (_viewModel.FollowPlayback)
            {
                foreach (var eventViewModel in _viewModel.PlaybackActiveEvents)
                {
                    EventList.SelectedItems.Add(eventViewModel);
                }
            }
            else if (_viewModel.SelectedEvent != null)
            {
                EventList.SelectedItem = _viewModel.SelectedEvent;
            }
        }
        finally
        {
            _isSynchronizingPlaybackSelection = false;
        }
    }

    private void OnEventListSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isSynchronizingPlaybackSelection && _viewModel != null)
        {
            _viewModel.SelectedEvent = EventList.SelectedItem as DiagnosticEventViewModel;
        }
    }

    internal void RevealSelection(DiagnosticEventViewModel row)
    {
        if (!IsLoaded || _viewModel?.FollowPlayback != false || !EventList.Items.Contains(row))
        {
            return;
        }

        EventList.UnselectAll();
        EventList.SelectedItem = row;
        EventList.ScrollIntoView(row);
    }

    private void CenterPlaybackSelection()
    {
        if (_viewModel is not { FollowPlayback: true } || _viewModel.PlaybackActiveEvents.Count == 0)
        {
            return;
        }

        var centerEvent = _viewModel.PlaybackActiveEvents[_viewModel.PlaybackActiveEvents.Count / 2];
        EventList.ScrollIntoView(centerEvent);

        Dispatcher.BeginInvoke(() =>
        {
            if (EventList.ItemContainerGenerator.ContainerFromItem(centerEvent) is not DataGridRow row ||
                FindVisualChild<ScrollViewer>(EventList) is not { } scrollViewer)
            {
                return;
            }

            var rowCenter = row.TransformToAncestor(scrollViewer)
                .Transform(new Point(0, row.ActualHeight / 2))
                .Y;
            var centeredOffset = scrollViewer.VerticalOffset + rowCenter - (scrollViewer.ViewportHeight / 2);
            scrollViewer.ScrollToVerticalOffset(Math.Max(0, centeredOffset));
        }, DispatcherPriority.Loaded);
    }

    private static T? FindVisualChild<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                return match;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }
}
