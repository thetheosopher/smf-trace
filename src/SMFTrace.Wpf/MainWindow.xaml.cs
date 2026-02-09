using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
        _viewModel.LiveNoteChanged += OnLiveNoteChanged;
        _viewModel.AllNotesCleared += OnAllNotesCleared;

        Closing += OnClosing;
        StateChanged += OnStateChanged;
        SizeChanged += OnSizeChanged;
        LocationChanged += OnLocationChanged;
        PreviewKeyDown += OnPreviewKeyDown;
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
            case nameof(MainViewModel.PlaybackRate):
                // Update tempo display
                PianoRoll.UpdateTempo(_viewModel.EffectiveTempo);
                break;
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        _viewModel.LiveNoteChanged -= OnLiveNoteChanged;
        _viewModel.AllNotesCleared -= OnAllNotesCleared;
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

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
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

        if (e.Key == Key.OemPlus || e.Key == Key.Add)
        {
            _viewModel.ZoomInCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemMinus || e.Key == Key.Subtract)
        {
            _viewModel.ZoomOutCommand.Execute(null);
            e.Handled = true;
            return;
        }
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
