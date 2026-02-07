using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using SMFTrace.Wpf.ViewModels;

namespace SMFTrace.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
public partial class MainWindow : Window
#pragma warning restore CA1001
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // Apply window position/size from settings
        ApplyWindowSettings();

        // Subscribe to property changes to update piano roll
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Closing += OnClosing;
        StateChanged += OnStateChanged;
        SizeChanged += OnSizeChanged;
        LocationChanged += OnLocationChanged;
    }

    private void ApplyWindowSettings()
    {
        var s = _viewModel.Settings.Current;

        // Only apply if the values are reasonable
        if (s.WindowWidth > 100 && s.WindowHeight > 100)
        {
            Width = s.WindowWidth;
            Height = s.WindowHeight;
        }

        // Ensure window is on screen
        if (s.WindowLeft >= 0 && s.WindowTop >= 0 &&
            s.WindowLeft < SystemParameters.VirtualScreenWidth &&
            s.WindowTop < SystemParameters.VirtualScreenHeight)
        {
            Left = s.WindowLeft;
            Top = s.WindowTop;
        }

        if (s.IsMaximized)
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void SaveWindowSettings()
    {
        var s = _viewModel.Settings.Current;
        s.IsMaximized = WindowState == WindowState.Maximized;

        // Only save size/position if not maximized
        if (WindowState == WindowState.Normal)
        {
            s.WindowWidth = Width;
            s.WindowHeight = Height;
            s.WindowLeft = Left;
            s.WindowTop = Top;
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        SaveWindowSettings();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            SaveWindowSettings();
        }
    }

    private void OnLocationChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Normal)
        {
            SaveWindowSettings();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.Events):
            case nameof(MainViewModel.Tracks):
                // Reload notes in piano roll
                PianoRoll.LoadNotes(_viewModel.Events, _viewModel.Tracks);
                // Also update instrument names since lanes were rebuilt
                PianoRoll.UpdateInstrumentNames(_viewModel.ChannelStates);
                break;

            case nameof(MainViewModel.ChannelStates):
                // Update instrument names
                PianoRoll.UpdateInstrumentNames(_viewModel.ChannelStates);
                break;

            case nameof(MainViewModel.CurrentTempo):
                // Update tempo display
                PianoRoll.UpdateTempo(_viewModel.CurrentTempo);
                break;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _viewModel.Dispose();
    }

    private void SeekSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
        _viewModel.BeginSeek();
    }

    private void SeekSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _viewModel.EndSeek();
    }
}
