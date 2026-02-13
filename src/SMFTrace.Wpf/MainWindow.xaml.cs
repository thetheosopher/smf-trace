using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using SMFTrace.Wpf.Controls;
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
        ApplyThemeResources(_viewModel.IsDarkTheme);

        // Apply window position/size from settings
        ApplyWindowSettings();

        // Subscribe to property changes to update piano roll
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        _viewModel.LiveNoteChanged += OnLiveNoteChanged;
        _viewModel.AllNotesCleared += OnAllNotesCleared;
        PianoRoll.SeekDragStarted += OnPianoRollSeekDragStarted;
        PianoRoll.SeekDragDelta += OnPianoRollSeekDragDelta;
        PianoRoll.SeekDragCompleted += OnPianoRollSeekDragCompleted;

        Closing += OnClosing;
        StateChanged += OnStateChanged;
        SizeChanged += OnSizeChanged;
        LocationChanged += OnLocationChanged;
        AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
        AddHandler(Mouse.PreviewMouseWheelEvent, new MouseWheelEventHandler(OnPreviewMouseWheel), true);
        DragOver += OnDragOver;
        Drop += OnDrop;
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
            case nameof(MainViewModel.EffectiveTempo):
            case nameof(MainViewModel.TempoAdjustmentBpm):
                // Update tempo display
                PianoRoll.UpdateTempo(_viewModel.EffectiveTempo);
                break;

            case nameof(MainViewModel.IsLoading):
            case nameof(MainViewModel.IsBusy):
                Mouse.OverrideCursor = _viewModel.IsBusy ? Cursors.Wait : null;
                break;

            case nameof(MainViewModel.IsDarkTheme):
                ApplyThemeResources(_viewModel.IsDarkTheme);
                break;
        }
    }

    private void ApplyThemeResources(bool isDarkTheme)
    {
        if (isDarkTheme)
        {
            SetBrushResource("WindowBackgroundBrush", "#1E1E23");
            SetBrushResource("PanelBackgroundBrush", "#2D2D30");
            SetBrushResource("ControlBackgroundBrush", "#3C3C3C");
            SetBrushResource("ControlHoverBackgroundBrush", "#505050");
            SetBrushResource("ControlPressedBackgroundBrush", "#007ACC");
            SetBrushResource("ControlDisabledBackgroundBrush", "#2A2A2A");
            SetBrushResource("ControlBorderBrush", "#555555");
            SetBrushResource("TextPrimaryBrush", "#FFFFFF");
            SetBrushResource("TextMutedBrush", "#666666");
            SetBrushResource("SurfaceBackgroundBrush", "#121212");
            SetBrushResource("SurfacePanelBrush", "#1B1B20");
            SetBrushResource("SurfaceBorderBrush", "#2A2A2A");
            SetBrushResource("TabItemBackgroundBrush", "#111111");
            SetBrushResource("TabItemHoverBrush", "#161616");
            SetBrushResource("TabItemSelectedBrush", "#000000");
            SetBrushResource("TabItemSelectedBorderBrush", "#3A3A3A");
            SetBrushResource("AccentBrush", "#007ACC");
            SetBrushResource("TempoBadgeBrush", "#C83C3C41");
            SetBrushResource("PlaylistRowBrush", "#252526");
            SetBrushResource("PlaylistAltRowBrush", "#1F1F23");
            SetBrushResource("PlaylistHeaderBrush", "#2D2D30");
            SetBrushResource("PlaylistSelectionBrush", "#2E3A52");
            SetBrushResource("PlaylistNowPlayingGlyphBrush", "#5DA9FF");
        }
        else
        {
            SetBrushResource("WindowBackgroundBrush", "#F3F4F8");
            SetBrushResource("PanelBackgroundBrush", "#E6E8EF");
            SetBrushResource("ControlBackgroundBrush", "#FFFFFF");
            SetBrushResource("ControlHoverBackgroundBrush", "#F1F3F8");
            SetBrushResource("ControlPressedBackgroundBrush", "#2F7FD8");
            SetBrushResource("ControlDisabledBackgroundBrush", "#DADDE6");
            SetBrushResource("ControlBorderBrush", "#B8BECC");
            SetBrushResource("TextPrimaryBrush", "#1F2430");
            SetBrushResource("TextMutedBrush", "#747B8B");
            SetBrushResource("SurfaceBackgroundBrush", "#EEF1F7");
            SetBrushResource("SurfacePanelBrush", "#E4E8F1");
            SetBrushResource("SurfaceBorderBrush", "#C8CEDC");
            SetBrushResource("TabItemBackgroundBrush", "#E0E5EF");
            SetBrushResource("TabItemHoverBrush", "#D5DCE9");
            SetBrushResource("TabItemSelectedBrush", "#FFFFFF");
            SetBrushResource("TabItemSelectedBorderBrush", "#B8BECC");
            SetBrushResource("AccentBrush", "#2F7FD8");
            SetBrushResource("TempoBadgeBrush", "#BFD7EE");
            SetBrushResource("PlaylistRowBrush", "#FFFFFF");
            SetBrushResource("PlaylistAltRowBrush", "#F4F6FB");
            SetBrushResource("PlaylistHeaderBrush", "#E0E5EF");
            SetBrushResource("PlaylistSelectionBrush", "#D5E6FA");
            SetBrushResource("PlaylistNowPlayingGlyphBrush", "#2F7FD8");
        }
    }

    private void SetBrushResource(string key, string hexColor)
    {
        var color = (Color)ColorConverter.ConvertFromString(hexColor);
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        Resources[key] = brush;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        Mouse.OverrideCursor = null;
        _viewModel.LiveNoteChanged -= OnLiveNoteChanged;
        _viewModel.AllNotesCleared -= OnAllNotesCleared;
        PianoRoll.SeekDragStarted -= OnPianoRollSeekDragStarted;
        PianoRoll.SeekDragDelta -= OnPianoRollSeekDragDelta;
        PianoRoll.SeekDragCompleted -= OnPianoRollSeekDragCompleted;
        _viewModel.Dispose();
    }

    private void OnLiveNoteChanged(object? sender, LiveNoteChanged e)
    {
        PianoRoll.UpdateLiveActiveNote(e.Channel, e.Note, e.IsActive);
    }

    private void OnAllNotesCleared(object? sender, EventArgs e)
    {
        PianoRoll.ClearLiveActiveNotes();
    }

    private void SeekSlider_DragStarted(object sender, DragStartedEventArgs e)
    {
        _viewModel.BeginSeek();
    }

    private void SeekSlider_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        _viewModel.EndSeek();
    }

    private void OnPianoRollSeekDragStarted(object? sender, PianoRollSeekEventArgs e)
    {
        _viewModel.BeginSeek();
    }

    private void OnPianoRollSeekDragDelta(object? sender, PianoRollSeekEventArgs e)
    {
        _viewModel.UpdateSeekPosition(e.Time);
    }

    private void OnPianoRollSeekDragCompleted(object? sender, PianoRollSeekEventArgs e)
    {
        _viewModel.UpdateSeekPosition(e.Time);
        _viewModel.EndSeek();
    }

    private void TempoAdjustmentSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            _viewModel.TempoAdjustmentBpm = 0;
            e.Handled = true;
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.P && Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _viewModel.PanicCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.O && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _viewModel.AddFilesCommand.Execute(null);
            }
            else
            {
                _viewModel.OpenCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Space)
        {
            // Toggle play/pause with spacebar
            if (_viewModel.CanPause)
            {
                _viewModel.PauseCommand.Execute(null);
            }
            else if (_viewModel.CanPlay)
            {
                _viewModel.PlayCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S)
        {
            _viewModel.StopCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Left)
        {
            _viewModel.PreviousCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Right)
        {
            _viewModel.NextCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.L)
        {
            _viewModel.LoopPlayback = !_viewModel.LoopPlayback;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemPlus || e.Key == Key.Add)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _viewModel.VerticalZoomInCommand.Execute(null);
            }
            else
            {
                _viewModel.ZoomInCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                _viewModel.VerticalZoomOutCommand.Execute(null);
            }
            else
            {
                _viewModel.ZoomOutCommand.Execute(null);
            }
            e.Handled = true;
            return;
        }
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            return;
        }

        if (e.Delta > 0)
        {
            _viewModel.VerticalZoomInCommand.Execute(null);
        }
        else if (e.Delta < 0)
        {
            _viewModel.VerticalZoomOutCommand.Execute(null);
        }

        e.Handled = true;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = DragDropEffects.None;

        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any(IsMidiFile))
            {
                e.Effects = DragDropEffects.Copy;
            }
        }

        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[]?)e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Any(IsMidiFile))
            {
                var isShift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
                if (isShift)
                {
                    await _viewModel.AddToPlaylistAsync(files);
                }
                else
                {
                    await _viewModel.ReplacePlaylistAsync(files, autoPlay: true);
                }
            }
        }
    }

    public async Task OpenFileFromCommandLineAsync(string filePath)
    {
        if (IsMidiFile(filePath))
        {
            await _viewModel.ReplacePlaylistAsync(new[] { filePath }, autoPlay: true);
        }
    }

    private async void PlaylistGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (PlaylistGrid.SelectedItem is PlaylistEntry entry)
        {
            var index = _viewModel.PlaylistEntries.IndexOf(entry);
            if (index >= 0)
            {
                await _viewModel.PlayPlaylistIndexAsync(index);
                MainTabs.SelectedIndex = 0;
            }
        }
    }


    private static bool IsMidiFile(string filePath)
    {
        var ext = Path.GetExtension(filePath);
        return ext.Equals(".mid", StringComparison.OrdinalIgnoreCase) ||
               ext.Equals(".midi", StringComparison.OrdinalIgnoreCase);
    }
}
