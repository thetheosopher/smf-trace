using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using SMFTrace.Core.Models;
using SMFTrace.Core.Sequencer;
using SMFTrace.MidiInterop;
using SMFTrace.Wpf.Settings;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// Main view model for the SMF Trace application.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly SettingsService _settings;

    private MidiFileData? _fileData;
    private SequencerEngine? _engine;
    private WinMidiOutput? _midiOutput;
    private StateSnapshotBuilder? _snapshotBuilder;

    private string _windowTitle = "SMF Trace";
    private TimeSpan _currentTime;
    private TimeSpan _totalDuration = TimeSpan.FromSeconds(60);
    private double _windowSeconds = 30.0;
    private bool _showTempo = true;
    private bool _showBarsBeatsGrid = true;
    private bool _showNoteNames;
    private bool _compactPitchRange;
    private bool _overlayMode;
    private PlaybackState _playbackState = PlaybackState.Stopped;
    private MidiDeviceInfo? _selectedDevice;
    private bool _isFileLoaded;
    private ChannelState[] _channelStates = new ChannelState[16];
    private double _currentTempo = 120.0;
    private bool _isSeeking;
    private DateTime _lastChannelStateUpdate;

    /// <summary>Diagnostics tab view model.</summary>
    public DiagnosticsViewModel Diagnostics { get; } = new();

    /// <summary>Settings service for persistence.</summary>
    public SettingsService Settings => _settings;

    public MainViewModel()
        : this(new SettingsService())
    {
    }

    public MainViewModel(SettingsService settings)
    {
        _settings = settings;

        // Load settings
        _settings.Load();
        ApplySettingsToViewModel();

        // Initialize default channel states
        for (var i = 0; i < 16; i++)
        {
            _channelStates[i] = new ChannelState();
        }

        // Initialize commands
        OpenCommand = new RelayCommand(Open);
        PlayCommand = new RelayCommand(Play, () => CanPlay);
        PauseCommand = new RelayCommand(Pause, () => CanPause);
        StopCommand = new RelayCommand(Stop, () => CanStop);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);

        // Start device enumeration
        RefreshDevices();
    }

    private void ApplySettingsToViewModel()
    {
        var s = _settings.Current;
        _windowSeconds = s.PianoRollWindowSeconds;
        _showTempo = s.ShowTempo;
        _showBarsBeatsGrid = s.ShowBarsBeatsGrid;
        _showNoteNames = s.ShowNoteNames;
        _compactPitchRange = s.CompactPitchRange;
        _overlayMode = s.OverlayMode;

        // Apply diagnostics filter states
        Diagnostics.ShowNotes = s.DiagShowNotes;
        Diagnostics.ShowControlChanges = s.DiagShowControlChanges;
        Diagnostics.ShowProgramChanges = s.DiagShowProgramChanges;
        Diagnostics.MetaOnlyMode = s.DiagMetaOnlyMode;
    }

    /// <summary>
    /// Saves current settings to disk.
    /// </summary>
    public void SaveSettings()
    {
        var s = _settings.Current;
        s.PianoRollWindowSeconds = WindowSeconds;
        s.ShowTempo = ShowTempo;
        s.ShowBarsBeatsGrid = ShowBarsBeatsGrid;
        s.ShowNoteNames = ShowNoteNames;
        s.CompactPitchRange = CompactPitchRange;
        s.OverlayMode = OverlayMode;
        s.LastDeviceName = SelectedDevice?.Name;

        // Save diagnostics filter states
        s.DiagShowNotes = Diagnostics.ShowNotes;
        s.DiagShowControlChanges = Diagnostics.ShowControlChanges;
        s.DiagShowProgramChanges = Diagnostics.ShowProgramChanges;
        s.DiagMetaOnlyMode = Diagnostics.MetaOnlyMode;

        _settings.Save();
    }

    #region Properties

    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetField(ref _windowTitle, value);
    }

    public TimeSpan CurrentTime
    {
        get => _currentTime;
        private set
        {
            if (SetField(ref _currentTime, value))
            {
                // Only notify SeekPosition when not actively seeking (prevents feedback loop)
                if (!_isSeeking)
                {
                    OnPropertyChanged(nameof(SeekPosition));
                }
            }
        }
    }

    public TimeSpan TotalDuration
    {
        get => _totalDuration;
        private set => SetField(ref _totalDuration, value);
    }

    public double SeekPosition
    {
        get => TotalDuration.TotalSeconds > 0 ? CurrentTime.TotalSeconds / TotalDuration.TotalSeconds : 0;
        set
        {
            // Only seek when not playing - during playback, the engine controls position
            // and the TwoWay binding would otherwise create a feedback loop
            if (TotalDuration.TotalSeconds > 0 && !IsPlaying)
            {
                SeekTo(TimeSpan.FromSeconds(value * TotalDuration.TotalSeconds));
            }
        }
    }

    public double WindowSeconds
    {
        get => _windowSeconds;
        set => SetField(ref _windowSeconds, value);
    }

    public bool ShowTempo
    {
        get => _showTempo;
        set => SetField(ref _showTempo, value);
    }

    public bool ShowBarsBeatsGrid
    {
        get => _showBarsBeatsGrid;
        set => SetField(ref _showBarsBeatsGrid, value);
    }

    public bool ShowNoteNames
    {
        get => _showNoteNames;
        set => SetField(ref _showNoteNames, value);
    }

    public bool CompactPitchRange
    {
        get => _compactPitchRange;
        set => SetField(ref _compactPitchRange, value);
    }

    public bool OverlayMode
    {
        get => _overlayMode;
        set => SetField(ref _overlayMode, value);
    }

    public PlaybackState PlaybackState
    {
        get => _playbackState;
        private set
        {
            if (SetField(ref _playbackState, value))
            {
                OnPropertyChanged(nameof(CanPlay));
                OnPropertyChanged(nameof(CanPause));
                OnPropertyChanged(nameof(CanStop));
                OnPropertyChanged(nameof(IsPlaying));
                OnPropertyChanged(nameof(IsNotPlaying));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// Whether playback is currently active (for smooth scrolling).
    /// </summary>
    public bool IsPlaying => PlaybackState == PlaybackState.Playing;

    /// <summary>
    /// Inverse of IsPlaying - used to disable seek slider during playback.
    /// </summary>
    public bool IsNotPlaying => !IsPlaying;

    public ObservableCollection<MidiDeviceInfo> Devices { get; } = [];

    public MidiDeviceInfo? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetField(ref _selectedDevice, value))
            {
                OnDeviceChanged();
            }
        }
    }

    public bool IsFileLoaded
    {
        get => _isFileLoaded;
        private set => SetField(ref _isFileLoaded, value);
    }

    public double CurrentTempo
    {
        get => _currentTempo;
        private set => SetField(ref _currentTempo, value);
    }

    public IReadOnlyList<MidiEventBase> Events => _fileData?.Events ?? [];
    public IReadOnlyList<TrackInfo> Tracks => _fileData?.Tracks ?? [];
    public ChannelState[] ChannelStates => _channelStates;

    public bool CanPlay => IsFileLoaded && PlaybackState != PlaybackState.Playing && SelectedDevice != null;
    public bool CanPause => PlaybackState == PlaybackState.Playing;
    public bool CanStop => PlaybackState != PlaybackState.Stopped;

    #endregion

    #region Commands

    public ICommand OpenCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }

    #endregion

    #region Command Implementations

    private void Open()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "MIDI Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*",
            Title = "Open MIDI File"
        };

        if (dialog.ShowDialog() == true)
        {
            LoadFile(dialog.FileName);
        }
    }

    public void LoadFile(string filePath)
    {
        try
        {
            // Stop any existing playback
            _engine?.Stop();
            _engine?.Dispose();

            // Load the file
            _fileData = MidiFileLoader.Load(filePath);
            _snapshotBuilder = new StateSnapshotBuilder(_fileData.Events);

            // Create new engine
            _engine = new SequencerEngine(_fileData);
            _engine.PositionChanged += OnPositionChanged;
            _engine.StateChanged += OnStateChanged;

            // Update UI
            TotalDuration = _fileData.Duration;
            CurrentTime = TimeSpan.Zero;
            IsFileLoaded = true;
            PlaybackState = PlaybackState.Stopped;
            WindowTitle = $"SMF Trace - {Path.GetFileName(filePath)}";

            // Initialize channel states
            _channelStates = _snapshotBuilder.RebuildStateAtTick(0);

            // Initialize tempo from start of file
            if (_fileData.TempoMap != null)
            {
                var tempo = _fileData.TempoMap.GetTempoAtTime(new Melanchall.DryWetMidi.Interaction.MidiTimeSpan(0));
                CurrentTempo = tempo.BeatsPerMinute;
            }
            else
            {
                CurrentTempo = 120.0; // Default MIDI tempo
            }

            // Load events into diagnostics view
            Diagnostics.LoadEvents(_fileData.Events);

            // Notify UI to reload notes
            OnPropertyChanged(nameof(Events));
            OnPropertyChanged(nameof(Tracks));
            OnPropertyChanged(nameof(ChannelStates));

            // Connect to output if available
            if (_midiOutput != null)
            {
                _engine.SetOutput(new MidiOutputAdapter(_midiOutput));
            }
        }
        catch (Core.MidiFileException mfex)
        {
            var title = mfex.ErrorType switch
            {
                Core.MidiFileErrorType.FileNotFound => "File Not Found",
                Core.MidiFileErrorType.InvalidFormat => "Invalid MIDI File",
                Core.MidiFileErrorType.UnsupportedFormat => "Unsupported Format",
                Core.MidiFileErrorType.EmptyFile => "Empty File",
                Core.MidiFileErrorType.CorruptedData => "Corrupted File",
                Core.MidiFileErrorType.FileTooLarge => "File Too Large",
                _ => "Error"
            };

            System.Windows.MessageBox.Show(
                mfex.Message,
                title,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"An unexpected error occurred while loading the file:\n\n{ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void Play()
    {
        _engine?.Play();
    }

    private void Pause()
    {
        _engine?.Pause();
    }

    private void Stop()
    {
        _engine?.Stop();
    }

    private void SeekTo(TimeSpan time)
    {
        _engine?.SeekTo(time);

        // Update channel states
        if (_snapshotBuilder != null && _engine != null)
        {
            _channelStates = _snapshotBuilder.RebuildStateAtTick(_engine.CurrentTick);
            OnPropertyChanged(nameof(ChannelStates));
        }
    }

    private void ZoomIn()
    {
        WindowSeconds = Math.Max(WindowSeconds * 0.8, 1.0);
    }

    private void ZoomOut()
    {
        WindowSeconds = Math.Min(WindowSeconds * 1.25, 300.0);
    }

    /// <summary>
    /// Called when the user begins dragging the seek slider.
    /// </summary>
    public void BeginSeek()
    {
        _isSeeking = true;
    }

    /// <summary>
    /// Called when the user finishes dragging the seek slider.
    /// </summary>
    public void EndSeek()
    {
        _isSeeking = false;
        OnPropertyChanged(nameof(SeekPosition));
    }

    #endregion

    #region Device Management

    private void RefreshDevices()
    {
        Devices.Clear();

        var devices = DeviceEnumerator.GetOutputDevices();
        foreach (var device in devices)
        {
            Devices.Add(device);
        }

        // Try to select last used device, or first device if available
        var lastDeviceName = _settings.Current.LastDeviceName;
        MidiDeviceInfo? targetDevice = null;

        if (!string.IsNullOrEmpty(lastDeviceName))
        {
            targetDevice = Devices.FirstOrDefault(d => d.Name == lastDeviceName);
        }

        if (targetDevice == null && Devices.Count > 0)
        {
            targetDevice = Devices[0];
        }

        if (targetDevice != null && SelectedDevice == null)
        {
            SelectedDevice = targetDevice;
        }
    }

    private void OnDeviceChanged()
    {
        // Close existing output
        _midiOutput?.Dispose();
        _midiOutput = null;

        if (SelectedDevice != null)
        {
            try
            {
                _midiOutput = WinMidiOutput.Open(SelectedDevice.Value.DeviceId);
                _engine?.SetOutput(new MidiOutputAdapter(_midiOutput));
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to open device: {ex.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
        else
        {
            _engine?.SetOutput(null);
        }

        OnPropertyChanged(nameof(CanPlay));
    }

    #endregion

    #region Event Handlers

    private void OnPositionChanged(object? sender, PositionChangedEventArgs e)
    {
        // Update on UI thread
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            CurrentTime = e.Time;

            // Update diagnostics current tick for auto-scroll
            Diagnostics.CurrentTick = e.Tick;

            // Update current tempo from tempo map
            if (_fileData?.TempoMap != null)
            {
                var tempo = _fileData.TempoMap.GetTempoAtTime(new Melanchall.DryWetMidi.Interaction.MidiTimeSpan(e.Tick));
                CurrentTempo = tempo.BeatsPerMinute;
            }

            // Throttle channel state updates to once per second during playback
            // (instrument names only change on program change events)
            var now = DateTime.UtcNow;
            if (_snapshotBuilder != null && (now - _lastChannelStateUpdate).TotalMilliseconds > 1000)
            {
                _lastChannelStateUpdate = now;
                _channelStates = _snapshotBuilder.RebuildStateAtTick(e.Tick);
                OnPropertyChanged(nameof(ChannelStates));
            }
        });
    }

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            PlaybackState = e.NewState;
        });
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        SaveSettings();
        _engine?.Stop();
        _engine?.Dispose();
        _midiOutput?.Dispose();
    }

    #endregion
}

/// <summary>
/// Simple relay command implementation.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();
}
