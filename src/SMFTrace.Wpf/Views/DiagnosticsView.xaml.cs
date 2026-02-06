using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SMFTrace.Wpf.ViewModels;

namespace SMFTrace.Wpf.Views;

/// <summary>
/// Interaction logic for DiagnosticsView.xaml
/// </summary>
public partial class DiagnosticsView : UserControl
{
    private DiagnosticsViewModel? _viewModel;
    private bool _isUserScrolling;

    public DiagnosticsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.ScrollToIndexRequested -= OnScrollToIndexRequested;
        }

        _viewModel = e.NewValue as DiagnosticsViewModel;

        if (_viewModel != null)
        {
            _viewModel.ScrollToIndexRequested += OnScrollToIndexRequested;
        }
    }

    private void OnScrollToIndexRequested(object? sender, int index)
    {
        if (_isUserScrolling) return;

        Dispatcher.BeginInvoke(() =>
        {
            if (index >= 0 && index < EventList.Items.Count)
            {
                EventList.ScrollIntoView(EventList.Items[index]);
            }
        });
    }

    private void EventList_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Click in list re-enables auto-scroll
        _viewModel?.ReEnableAutoScroll();
        _isUserScrolling = false;
    }

    private void EventList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // If user manually scrolls (not programmatic), disable auto-scroll
        if (e.VerticalChange != 0 && Mouse.LeftButton == MouseButtonState.Pressed)
        {
            _isUserScrolling = true;
            _viewModel?.DisableAutoScroll();
        }
    }
}
