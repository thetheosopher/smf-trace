using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
    private bool _showTrackControls;
    private bool _showLyricsLane;
    private double _pitchRowHeight = 8.0;
    private bool _loopPlayback;
    private bool _disableSysExOutput;
    private double _tempoAdjustmentBpm;
    private int _defaultInstrumentProgram;
    private bool _fileHasProgramChanges;
    private byte[] _fileUsedChannels = [];
    private bool _showNoteNames;
    private bool _showPianoKeys;
    private bool _compactPitchRange;
    private bool _overlayMode;
    private int _midiFormat = 1;
    private PlaybackState _playbackState = PlaybackState.Stopped;
    private MidiDeviceInfo? _selectedDevice;
    private bool _isFileLoaded;
    private bool _isLoading;
    private ChannelState[] _channelStates = new ChannelState[16];
    private double _currentTempo = 120.0;
    private double _effectiveTempo = 120.0;
    private double _minTempoLimit = 10.0;
    private double _maxTempoLimit = 480.0;
    private double _minTempoAdjustmentBpm = -110.0;
    private double _maxTempoAdjustmentBpm = 360.0;
    private bool _isSeeking;
    private bool _forceStopPosition;
    private bool _userStopRequested;
    private bool _isPlaylistTransition;
    private int _currentPlaylistIndex = -1;
    private int _nowPlayingIndex = -1;
    private CancellationTokenSource? _playlistParseCts;
    private int _playlistParseInFlight;
    private readonly Dictionary<string, PlaylistMetadataCache> _playlistMetadataCache = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastChannelStateUpdate;
    private InstrumentOption? _selectedDefaultInstrument;
    private bool _hasLyrics;
    private int _currentLyricIndex = -1;
    private volatile bool _disposed;

    private const double MinPitchRowHeight = 8.0;
    private const double MaxPitchRowHeight = 24.0;
    private const double PitchRowHeightStep = 2.0;

    /// <summary>Diagnostics tab view model.</summary>
    public DiagnosticsViewModel Diagnostics { get; } = new();

    public ObservableCollection<TrackPlaybackViewModel> TrackPlaybackStates { get; } = new();

    public ObservableCollection<LyricLineViewModel> Lyrics { get; } = new();

    public ObservableCollection<PlaylistEntry> PlaylistEntries { get; } = new();

    /// <summary>Settings service for persistence.</summary>
    public SettingsService Settings => _settings;

    public MainViewModel()
        : this(new SettingsService())
    {
    }

    public MainViewModel(SettingsService settings)
    {
        _settings = settings;

        DefaultInstruments = new ObservableCollection<InstrumentOption>(
            Enumerable.Range(0, 128)
                .Select(program => new InstrumentOption((byte)program, ChannelState.GetGmInstrumentName((byte)program))));

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
        PanicCommand = new RelayCommand(Panic);
        PreviousCommand = new RelayCommand(PlayPrevious, () => CanNavigatePlaylist);
        NextCommand = new RelayCommand(PlayNext, () => CanNavigatePlaylist);
        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        VerticalZoomInCommand = new RelayCommand(VerticalZoomIn);
        VerticalZoomOutCommand = new RelayCommand(VerticalZoomOut);
        AddFilesCommand = new RelayCommand(AddFiles);

        // Start device enumeration
        RefreshDevices();

        PlaylistEntries.CollectionChanged += OnPlaylistChanged;
        TrackPlaybackStates.CollectionChanged += OnTrackPlaybackStatesChanged;
    }

    private void ApplySettingsToViewModel()
    {
        var s = _settings.Current;
        _windowSeconds = s.PianoRollWindowSeconds;
        _showTempo = s.ShowTempo;
        _showBarsBeatsGrid = s.ShowBarsBeatsGrid;
        _loopPlayback = s.LoopPlayback;
        _disableSysExOutput = s.DisableSysExOutput;
        _tempoAdjustmentBpm = s.TempoAdjustmentBpm;
        _defaultInstrumentProgram = Math.Clamp(s.DefaultInstrumentProgram, 0, 127);
        _showNoteNames = s.ShowNoteNames;
        _showPianoKeys = s.ShowPianoKeys;
        _compactPitchRange = s.CompactPitchRange;
        _overlayMode = s.OverlayMode;

        // Apply diagnostics filter states
        Diagnostics.ShowNotes = s.DiagShowNotes;
        Diagnostics.ShowControlChanges = s.DiagShowControlChanges;
        Diagnostics.ShowProgramChanges = s.DiagShowProgramChanges;
        Diagnostics.MetaOnlyMode = s.DiagMetaOnlyMode;

        _selectedDefaultInstrument = DefaultInstruments.FirstOrDefault(
            instrument => instrument.Program == _defaultInstrumentProgram)
            ?? DefaultInstruments.FirstOrDefault();
        OnPropertyChanged(nameof(SelectedDefaultInstrument));
        UpdateTempoLimitsFromFile();
        OnPropertyChanged(nameof(TempoAdjustmentBpm));
        OnPropertyChanged(nameof(TempoAdjustmentLabel));
        UpdateEffectiveTempo();
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
        s.LoopPlayback = LoopPlayback;
        s.DisableSysExOutput = DisableSysExOutput;
        s.TempoAdjustmentBpm = TempoAdjustmentBpm;
        s.DefaultInstrumentProgram = _defaultInstrumentProgram;
        s.ShowNoteNames = ShowNoteNames;
        s.ShowPianoKeys = ShowPianoKeys;
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

    public bool ShowTrackControls
    {
        get => _showTrackControls;
        set => SetField(ref _showTrackControls, value);
    }

    public bool ShowBarsBeatsGrid
    {
        get => _showBarsBeatsGrid;
        set => SetField(ref _showBarsBeatsGrid, value);
    }

    public double PitchRowHeight
    {
        get => _pitchRowHeight;
        set
        {
            var clamped = Math.Clamp(value, MinPitchRowHeight, MaxPitchRowHeight);
            SetField(ref _pitchRowHeight, clamped);
        }
    }

    public bool ShowLyricsLane
    {
        get => _showLyricsLane;
        set => SetField(ref _showLyricsLane, value);
    }

    public bool LoopPlayback
    {
        get => _loopPlayback;
        set
        {
            if (SetField(ref _loopPlayback, value))
            {
                UpdateEngineLoopMode();
            }
        }
    }

#pragma warning disable CA1711 // SysEx is an industry-standard term
    public bool DisableSysExOutput
#pragma warning restore CA1711
    {
        get => _disableSysExOutput;
        set
        {
            if (SetField(ref _disableSysExOutput, value) && _engine != null)
            {
                _engine.DisableSysExOutput = value;
            }
        }
    }

    public double TempoAdjustmentBpm
    {
        get => _tempoAdjustmentBpm;
        set
        {
            var clamped = Math.Clamp(value, MinTempoAdjustmentBpm, MaxTempoAdjustmentBpm);
            var rounded = Math.Round(clamped, 0, MidpointRounding.AwayFromZero);
            if (SetField(ref _tempoAdjustmentBpm, rounded))
            {
                if (_engine != null)
                {
                    _engine.TempoAdjustmentBpm = rounded;
                }

                OnPropertyChanged(nameof(TempoAdjustmentLabel));
                UpdateEffectiveTempo();
            }
        }
    }

    public double MinTempoAdjustmentBpm
    {
        get => _minTempoAdjustmentBpm;
        private set => SetField(ref _minTempoAdjustmentBpm, value);
    }

    public double MaxTempoAdjustmentBpm
    {
        get => _maxTempoAdjustmentBpm;
        private set => SetField(ref _maxTempoAdjustmentBpm, value);
    }

    public string TempoAdjustmentLabel
    {
        get
        {
            var rounded = Math.Round(TempoAdjustmentBpm, 0, MidpointRounding.AwayFromZero);
            if (Math.Abs(rounded) < 0.5)
            {
                return "0 BPM";
            }

            var sign = rounded > 0 ? "+" : string.Empty;
            return $"{sign}{rounded:0} BPM";
        }
    }

    public bool ShowNoteNames
    {
        get => _showNoteNames;
        set => SetField(ref _showNoteNames, value);
    }

    public bool ShowPianoKeys
    {
        get => _showPianoKeys;
        set => SetField(ref _showPianoKeys, value);
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

    public int MidiFormat
    {
        get => _midiFormat;
        private set => SetField(ref _midiFormat, value);
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

    public ObservableCollection<InstrumentOption> DefaultInstruments { get; }

    public InstrumentOption? SelectedDefaultInstrument
    {
        get => _selectedDefaultInstrument;
        set
        {
            if (SetField(ref _selectedDefaultInstrument, value) && value != null)
            {
                _defaultInstrumentProgram = value.Program;
                ApplyDefaultInstrumentToChannelStates();
                SendDefaultInstrumentToDevice();
                OnPropertyChanged(nameof(ChannelStates));
            }
        }
    }

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

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetField(ref _isLoading, value))
            {
                OnPropertyChanged(nameof(IsBusy));
            }
        }
    }

    public bool IsPlaylistMetadataParsing => _playlistParseInFlight > 0;

    public bool IsBusy => IsLoading || IsPlaylistMetadataParsing;

    public double CurrentTempo
    {
        get => _currentTempo;
        private set
        {
            if (SetField(ref _currentTempo, value))
            {
                UpdateTempoAdjustmentRange();
                UpdateEffectiveTempo();
            }
        }
    }

    public double EffectiveTempo
    {
        get => _effectiveTempo;
        private set => SetField(ref _effectiveTempo, value);
    }

    private void UpdateEffectiveTempo()
    {
        var minTempo = _minTempoLimit;
        var maxTempo = _maxTempoLimit;
        var targetTempo = _currentTempo + _tempoAdjustmentBpm;
        EffectiveTempo = Math.Clamp(targetTempo, minTempo, maxTempo);
    }

    private void UpdateTempoLimitsFromFile()
    {
        if (_fileData == null)
        {
            _minTempoLimit = 10.0;
            _maxTempoLimit = 480.0;
        }
        else
        {
            var (min, max) = GetTempoRange(_fileData);
            _minTempoLimit = Math.Min(10.0, min);
            _maxTempoLimit = Math.Max(480.0, max);
        }

        UpdateTempoAdjustmentRange();
    }

    private void UpdateTempoAdjustmentRange()
    {
        var minAdjustment = _minTempoLimit - _currentTempo;
        var maxAdjustment = _maxTempoLimit - _currentTempo;

        MinTempoAdjustmentBpm = minAdjustment;
        MaxTempoAdjustmentBpm = maxAdjustment;

        var clamped = Math.Clamp(_tempoAdjustmentBpm, minAdjustment, maxAdjustment);
        if (Math.Abs(clamped - _tempoAdjustmentBpm) > 0.001)
        {
            TempoAdjustmentBpm = clamped;
        }
        else
        {
            OnPropertyChanged(nameof(TempoAdjustmentLabel));
        }
    }

    private static (double Min, double Max) GetTempoRange(MidiFileData data)
    {
        var tempos = data.Events
            .OfType<MetaEvent>()
            .Where(evt => evt.IsSetTempo)
            .Select(evt => evt.Bpm)
            .ToList();

        if (tempos.Count == 0)
        {
            return (120.0, 120.0);
        }

        return (tempos.Min(), tempos.Max());
    }

    public IReadOnlyList<MidiEventBase> Events => _fileData?.Events ?? [];
    public IReadOnlyList<TrackInfo> Tracks => _fileData?.Tracks ?? [];
    public ChannelState[] ChannelStates => _channelStates;
    public bool HasTrackPlaybackStates => TrackPlaybackStates.Count > 0;
    public bool HasLyrics
    {
        get => _hasLyrics;
        private set => SetField(ref _hasLyrics, value);
    }

    public event EventHandler<LiveNoteChanged>? LiveNoteChanged;

    /// <summary>
    /// Raised when all notes should be cleared (stop, new file load, etc.).
    /// </summary>
    public event EventHandler? AllNotesCleared;

    public bool CanPlay => SelectedDevice != null && PlaybackState != PlaybackState.Playing && (IsFileLoaded || PlaylistEntries.Count > 0);
    public bool CanPause => PlaybackState == PlaybackState.Playing;
    public bool CanStop => PlaybackState != PlaybackState.Stopped;
    public bool ShowPlaylistNavigation => PlaylistEntries.Count > 1;
    private bool CanNavigatePlaylist => PlaylistEntries.Count > 1 && !_isPlaylistTransition && !IsLoading;

    #endregion

    #region Commands

    public ICommand OpenCommand { get; }
    public ICommand PlayCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand PanicCommand { get; }
    public ICommand PreviousCommand { get; }
    public ICommand NextCommand { get; }
    public ICommand ZoomInCommand { get; }
    public ICommand ZoomOutCommand { get; }
    public ICommand VerticalZoomInCommand { get; }
    public ICommand VerticalZoomOutCommand { get; }
    public ICommand AddFilesCommand { get; }

    #endregion

    #region Command Implementations

    private async void Open()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "MIDI Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*",
            Title = "Open MIDI File",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            await ReplacePlaylistAsync(dialog.FileNames, autoPlay: true);
        }
    }

    private async void AddFiles()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "MIDI Files (*.mid;*.midi)|*.mid;*.midi|All Files (*.*)|*.*",
            Title = "Add MIDI Files",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            await AddToPlaylistAsync(dialog.FileNames);
        }
    }

    public async Task<bool> LoadFileAsync(string filePath)
    {
        IsLoading = true;
        try
        {
            IsFileLoaded = false;
            ClearLyrics();
            ResetTrackPlaybackStates();
            // Stop any existing playback
            var oldEngine = _engine;
            if (_engine != null)
            {
                _engine.PositionChanged -= OnPositionChanged;
                _engine.StateChanged -= OnStateChanged;
                _engine.NoteActivityChanged -= OnNoteActivityChanged;
            }
            _engine = null;
            await Task.Run(() =>
            {
                oldEngine?.Stop();
                StopAllNotesOnOutput();
                oldEngine?.Dispose();
            });

            // Clear any active note highlights
            RaiseAllNotesCleared();

            var loadResult = await Task.Run(() => LoadMidiFile(filePath));

            _fileData = loadResult.FileData;
            _snapshotBuilder = loadResult.SnapshotBuilder;
            _fileHasProgramChanges = loadResult.FileHasProgramChanges;
            _fileUsedChannels = loadResult.FileUsedChannels;
            MidiFormat = _fileData.Format;

            // Create new engine
            _engine = new SequencerEngine(_fileData, new SMFTrace.Core.Configuration.PlaybackOptions
            {
                LoopPlayback = LoopPlayback,
                DisableSysExOutput = DisableSysExOutput,
                TempoAdjustmentBpm = TempoAdjustmentBpm
            });
            _engine.PositionChanged += OnPositionChanged;
            _engine.StateChanged += OnStateChanged;
            _engine.NoteActivityChanged += OnNoteActivityChanged;
            UpdateEngineLoopMode();

            // Update UI
            TotalDuration = _fileData.Duration;
            CurrentTime = TimeSpan.Zero;
            IsFileLoaded = true;
            PlaybackState = PlaybackState.Stopped;
            WindowTitle = $"SMF Trace - {Path.GetFileName(filePath)}";
            OnPropertyChanged(nameof(TempoAdjustmentLabel));

            // Initialize channel states
            _channelStates = loadResult.InitialChannelStates;
            ApplyDefaultInstrumentToChannelStates();

            RebuildTrackPlaybackStates();
            LoadLyricsFromEvents(_fileData.Events);

            // Initialize tempo from start of file
            UpdateTempoLimitsFromFile();
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
            await Diagnostics.LoadEventsAsync(_fileData.Events);

            // Notify UI to reload notes
            OnPropertyChanged(nameof(Events));
            OnPropertyChanged(nameof(Tracks));
            OnPropertyChanged(nameof(ChannelStates));

            // Connect to output if available
            if (_midiOutput != null)
            {
                _engine.SetOutput(new MidiOutputAdapter(_midiOutput));
                SendDefaultInstrumentToDevice();
            }

            UpdateTrackActivityMask();

            return true;
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
            return false;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"An unexpected error occurred while loading the file:\n\n{ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static LoadResult LoadMidiFile(string filePath)
    {
        var fileData = MidiFileLoader.Load(filePath);
        var snapshotBuilder = new StateSnapshotBuilder(fileData.Events);
        var fileHasProgramChanges = fileData.Events.OfType<ProgramChangeEvent>().Any();
        var fileUsedChannels = fileData.Events
            .OfType<ChannelEventBase>()
            .Select(evt => evt.Channel)
            .Distinct()
            .OrderBy(channel => channel)
            .ToArray();

        var initialChannelStates = snapshotBuilder.RebuildStateAtTick(0);

        return new LoadResult(
            fileData,
            snapshotBuilder,
            fileHasProgramChanges,
            fileUsedChannels,
            initialChannelStates);
    }

    public Task AddToPlaylistAsync(IEnumerable<string> filePaths)
    {
        var files = FilterMidiFiles(filePaths).ToList();
        if (files.Count == 0)
        {
            return Task.CompletedTask;
        }

        EnsurePlaylistParser();
        foreach (var file in files)
        {
            var entry = CreatePlaylistEntry(file);
            PlaylistEntries.Add(entry);
            StartMetadataParse(entry, _playlistParseCts!.Token);
        }

        return Task.CompletedTask;
    }

    public async Task ReplacePlaylistAsync(IEnumerable<string> filePaths, bool autoPlay)
    {
        var files = FilterMidiFiles(filePaths).ToList();
        await StopPlaybackForReplaceAsync();

        CancelPlaylistParsing();
        ClearPlaylist();

        if (files.Count == 0)
        {
            return;
        }

        EnsurePlaylistParser();
        foreach (var file in files)
        {
            var entry = CreatePlaylistEntry(file);
            PlaylistEntries.Add(entry);
            StartMetadataParse(entry, _playlistParseCts!.Token);
        }

        if (autoPlay)
        {
            await PlayPlaylistIndexAsync(0);
        }
    }

    private async Task StopPlaybackForReplaceAsync()
    {
        _userStopRequested = true;

        var oldEngine = _engine;
        if (oldEngine != null)
        {
            oldEngine.PositionChanged -= OnPositionChanged;
            oldEngine.StateChanged -= OnStateChanged;
            oldEngine.NoteActivityChanged -= OnNoteActivityChanged;
        }

        await Task.Run(() =>
        {
            oldEngine?.Stop();
            StopAllNotesOnOutput();
        });

        // Clear any active note highlights
        RaiseAllNotesCleared();

        _userStopRequested = false;
    }

    public async Task PlayPlaylistIndexAsync(int index)
    {
        if (index < 0 || index >= PlaylistEntries.Count)
        {
            return;
        }

        if (_isPlaylistTransition)
        {
            return;
        }

        _isPlaylistTransition = true;
        _userStopRequested = true;

        _currentPlaylistIndex = index;
        var entry = PlaylistEntries[index];

        try
        {
            var loaded = await LoadFileAsync(entry.FilePath);
            if (!loaded)
            {
                return;
            }

            if (CanPlay)
            {
                _engine?.Play();
            }

            SetNowPlaying(index);
        }
        finally
        {
            _userStopRequested = false;
            _isPlaylistTransition = false;
        }
    }

    private void ClearPlaylist()
    {
        foreach (var entry in PlaylistEntries)
        {
            entry.IsNowPlaying = false;
        }

        PlaylistEntries.Clear();
        _currentPlaylistIndex = -1;
        _nowPlayingIndex = -1;
    }

    private void EnsurePlaylistParser()
    {
        _playlistParseCts ??= new CancellationTokenSource();
    }

    private void CancelPlaylistParsing()
    {
        _playlistParseCts?.Cancel();
        _playlistParseCts?.Dispose();
        _playlistParseCts = null;
    }

    private static PlaylistEntry CreatePlaylistEntry(string filePath)
    {
        var title = Path.GetFileNameWithoutExtension(filePath);
        var entry = new PlaylistEntry(filePath, title)
        {
            DurationDisplay = "-",
            TempoDisplay = "-",
            TimeSignatureDisplay = "4/4",
            KeySignatureDisplay = "-",
            SmfTypeDisplay = "-",
            TrackCount = 0,
            SysExPresentDisplay = "No",
            LyricsPresentDisplay = "No",
            MetadataStatus = PlaylistMetadataStatus.Unparsed
        };

        return entry;
    }

    private void StartMetadataParse(PlaylistEntry entry, CancellationToken token)
    {
        entry.MetadataStatus = PlaylistMetadataStatus.Parsing;
        IncrementPlaylistParsing();

        _ = Task.Run(() =>
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }

                if (TryGetCachedMetadata(entry.FilePath, out var cached))
                {
                    if (!token.IsCancellationRequested)
                    {
                        ApplyMetadata(entry, cached);
                    }
                    return;
                }

                var metadata = PlaylistMetadataParser.Parse(entry.FilePath);
                CacheMetadata(entry.FilePath, metadata);
                if (!token.IsCancellationRequested)
                {
                    ApplyMetadata(entry, metadata);
                }
            }
            catch (Exception)
            {
                if (!token.IsCancellationRequested)
                {
                    RemoveInvalidPlaylistEntry(entry);
                }
            }
            finally
            {
                DecrementPlaylistParsing();
            }
        }, CancellationToken.None);
    }

    private void RemoveInvalidPlaylistEntry(PlaylistEntry entry)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            PlaylistEntries.Remove(entry);
        });
    }

    private void IncrementPlaylistParsing()
    {
        if (Interlocked.Increment(ref _playlistParseInFlight) == 1)
        {
            OnPropertyChanged(nameof(IsPlaylistMetadataParsing));
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private void DecrementPlaylistParsing()
    {
        var value = Interlocked.Decrement(ref _playlistParseInFlight);
        if (value <= 0)
        {
            Interlocked.Exchange(ref _playlistParseInFlight, 0);
            OnPropertyChanged(nameof(IsPlaylistMetadataParsing));
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private static void ApplyMetadata(PlaylistEntry entry, PlaylistMetadata metadata)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            entry.DisplayTitle = metadata.Title;
            entry.DurationDisplay = PlaylistMetadataParser.FormatDuration(metadata.Duration);
            entry.TempoDisplay = PlaylistMetadataParser.FormatTempoDisplay(metadata.TempoMin, metadata.TempoMax);
            entry.TimeSignatureDisplay = metadata.TimeSignature;
            entry.KeySignatureDisplay = metadata.KeySignature;
            entry.SmfTypeDisplay = metadata.SmfType;
            entry.TrackCount = metadata.TrackCount;
            entry.SysExPresentDisplay = metadata.HasSysExEvents ? "Yes" : "No";
            entry.LyricsPresentDisplay = metadata.HasLyrics ? "Yes" : "No";
            entry.MetadataStatus = PlaylistMetadataStatus.Parsed;
        });
    }

    private static void ApplyMetadataFailed(PlaylistEntry entry)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            entry.MetadataStatus = PlaylistMetadataStatus.Failed;
        });
    }

    private bool TryGetCachedMetadata(string filePath, out PlaylistMetadata metadata)
    {
        metadata = default!;

        if (!_playlistMetadataCache.TryGetValue(filePath, out var cache))
        {
            return false;
        }

        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            return false;
        }

        if (cache.LastWriteTimeUtc != info.LastWriteTimeUtc || cache.Length != info.Length)
        {
            return false;
        }

        metadata = cache.Metadata;
        return true;
    }

    private void CacheMetadata(string filePath, PlaylistMetadata metadata)
    {
        var info = new FileInfo(filePath);
        if (!info.Exists)
        {
            return;
        }

        _playlistMetadataCache[filePath] = new PlaylistMetadataCache(
            metadata,
            info.LastWriteTimeUtc,
            info.Length);
    }

    private static IEnumerable<string> FilterMidiFiles(IEnumerable<string> filePaths)
    {
        foreach (var file in filePaths)
        {
            var ext = Path.GetExtension(file);
            if (ext.Equals(".mid", StringComparison.OrdinalIgnoreCase) ||
                ext.Equals(".midi", StringComparison.OrdinalIgnoreCase))
            {
                yield return file;
            }
        }
    }

    private void StopAllNotesOnOutput()
    {
        if (_midiOutput == null)
        {
            return;
        }

        try
        {
            _midiOutput.ResetControllers();
            _midiOutput.AllNotesOff();
            _midiOutput.Reset();
        }
        catch (MidiException)
        {
            // Ignore device reset errors during file load.
        }
    }

    private void RaiseAllNotesCleared()
    {
        if (_disposed) return;
        AllNotesCleared?.Invoke(this, EventArgs.Empty);
    }

    private void Panic()
    {
        StopAllNotesOnOutput();
        RaiseAllNotesCleared();
    }

    private async void Play()
    {
        if (_engine == null && PlaylistEntries.Count > 0)
        {
            var index = _currentPlaylistIndex >= 0 ? _currentPlaylistIndex : 0;
            await PlayPlaylistIndexAsync(index);
            return;
        }

        SendDefaultInstrumentToDevice();
        _engine?.Play();
    }

    private void Pause()
    {
        _engine?.Pause();
    }

    private async void PlayPrevious()
    {
        var index = GetPreviousPlaylistIndex();
        if (index < 0)
        {
            return;
        }

        await PlayPlaylistIndexAsync(index);
    }

    private async void PlayNext()
    {
        var index = GetNextPlaylistIndex();
        if (index < 0)
        {
            return;
        }

        await PlayPlaylistIndexAsync(index);
    }

    private async void Stop()
    {
        _userStopRequested = true;
        _forceStopPosition = _engine != null;
        _isSeeking = false;
        _engine?.Stop();
        StopAllNotesOnOutput();
        RaiseAllNotesCleared();

        await Task.Delay(500);

        _forceStopPosition = false;
        CurrentTime = TimeSpan.Zero;
        OnPropertyChanged(nameof(SeekPosition));

        if (_snapshotBuilder != null)
        {
            _channelStates = _snapshotBuilder.RebuildStateAtTick(0);
            ApplyDefaultInstrumentToChannelStates();
            OnPropertyChanged(nameof(ChannelStates));
        }

        UpdateCurrentLyric(TimeSpan.Zero);

        if (_fileData?.TempoMap != null)
        {
            var tempo = _fileData.TempoMap.GetTempoAtTime(new Melanchall.DryWetMidi.Interaction.MidiTimeSpan(0));
            CurrentTempo = tempo.BeatsPerMinute;
        }

        _userStopRequested = false;
    }

    private void SeekTo(TimeSpan time)
    {
        _engine?.SeekTo(time);

        // Update channel states
        if (_snapshotBuilder != null && _engine != null)
        {
            _channelStates = _snapshotBuilder.RebuildStateAtTick(_engine.CurrentTick);
            ApplyDefaultInstrumentToChannelStates();
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

    private void VerticalZoomIn()
    {
        PitchRowHeight = PitchRowHeight + PitchRowHeightStep;
    }

    private void VerticalZoomOut()
    {
        PitchRowHeight = PitchRowHeight - PitchRowHeightStep;
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

    #region Playlist

    private void OnPlaylistChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanPlay));
        OnPropertyChanged(nameof(ShowPlaylistNavigation));
        UpdateEngineLoopMode();

        if (_nowPlayingIndex >= PlaylistEntries.Count)
        {
            _nowPlayingIndex = -1;
        }

        if (_currentPlaylistIndex >= PlaylistEntries.Count)
        {
            _currentPlaylistIndex = PlaylistEntries.Count - 1;
        }
    }

    private int GetPreviousPlaylistIndex()
    {
        if (!CanNavigatePlaylist)
        {
            return -1;
        }

        var currentIndex = GetCurrentPlaylistIndexOrDefault();
        var previousIndex = currentIndex - 1;
        if (previousIndex < 0)
        {
            return LoopPlayback ? PlaylistEntries.Count - 1 : -1;
        }

        return previousIndex;
    }

    private int GetNextPlaylistIndex()
    {
        if (!CanNavigatePlaylist)
        {
            return -1;
        }

        var currentIndex = GetCurrentPlaylistIndexOrDefault();
        var nextIndex = currentIndex + 1;
        if (nextIndex >= PlaylistEntries.Count)
        {
            return LoopPlayback ? 0 : -1;
        }

        return nextIndex;
    }

    private int GetCurrentPlaylistIndexOrDefault()
    {
        if (_currentPlaylistIndex >= 0 && _currentPlaylistIndex < PlaylistEntries.Count)
        {
            return _currentPlaylistIndex;
        }

        if (_nowPlayingIndex >= 0 && _nowPlayingIndex < PlaylistEntries.Count)
        {
            return _nowPlayingIndex;
        }

        return 0;
    }

    private void UpdateEngineLoopMode()
    {
        if (_engine == null)
        {
            return;
        }

        var useEngineLoop = LoopPlayback && PlaylistEntries.Count <= 1;
        _engine.LoopPlayback = useEngineLoop;
    }

    private void OnTrackPlaybackStatesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasTrackPlaybackStates));
        UpdateTrackActivityMask();
    }

    private void RebuildTrackPlaybackStates()
    {
        foreach (var track in TrackPlaybackStates)
        {
            track.PropertyChanged -= OnTrackPlaybackStateChanged;
        }

        TrackPlaybackStates.Clear();

        if (_fileData == null)
        {
            OnPropertyChanged(nameof(HasTrackPlaybackStates));
            return;
        }

        var tracksWithNotes = _fileData.Events
            .OfType<NoteOnEvent>()
            .Where(evt => evt.Velocity > 0)
            .Select(evt => evt.TrackIndex)
            .Distinct()
            .ToHashSet();

        foreach (var track in _fileData.Tracks)
        {
            if (!tracksWithNotes.Contains(track.Index))
            {
                continue;
            }

            var vm = new TrackPlaybackViewModel(track.Index, track.Name);
            vm.PropertyChanged += OnTrackPlaybackStateChanged;
            TrackPlaybackStates.Add(vm);
        }

        OnPropertyChanged(nameof(HasTrackPlaybackStates));
    }

    private void ResetTrackPlaybackStates()
    {
        foreach (var track in TrackPlaybackStates)
        {
            track.PropertyChanged -= OnTrackPlaybackStateChanged;
        }

        TrackPlaybackStates.Clear();
        OnPropertyChanged(nameof(HasTrackPlaybackStates));
    }

    private void OnTrackPlaybackStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrackPlaybackViewModel.IsMuted) ||
            e.PropertyName == nameof(TrackPlaybackViewModel.IsSolo))
        {
            UpdateTrackActivityMask();
        }
    }

    private void UpdateTrackActivityMask()
    {
        if (_engine == null)
        {
            return;
        }

        if (_fileData == null)
        {
            return;
        }

        var anySolo = TrackPlaybackStates.Any(track => track.IsSolo);
        var mask = new bool[_fileData.Tracks.Count];
        Array.Fill(mask, !anySolo);

        for (var i = 0; i < TrackPlaybackStates.Count; i++)
        {
            var track = TrackPlaybackStates[i];
            if (track.TrackIndex >= 0 && track.TrackIndex < mask.Length)
            {
                mask[track.TrackIndex] = anySolo ? track.IsSolo : !track.IsMuted;
            }
        }

        _engine.SetTrackActivityMask(mask);
    }

    private void LoadLyricsFromEvents(IReadOnlyList<MidiEventBase> events)
    {
        Lyrics.Clear();
        ShowLyricsLane = false;

        var lyricEvents = events
            .OfType<MetaEvent>()
            .Where(evt => evt.MetaType == 0x05 && !string.IsNullOrWhiteSpace(evt.TextContent))
            .OrderBy(evt => evt.Time)
            .ToList();

        foreach (var lyric in lyricEvents)
        {
            Lyrics.Add(new LyricLineViewModel(lyric.Time, lyric.TextContent!.Trim()));
        }

        HasLyrics = Lyrics.Count > 0;
        _currentLyricIndex = -1;
    }

    private void ClearLyrics()
    {
        Lyrics.Clear();
        HasLyrics = false;
        ShowLyricsLane = false;
        _currentLyricIndex = -1;
    }

    private void UpdateCurrentLyric(TimeSpan time)
    {
        if (!HasLyrics || Lyrics.Count == 0)
        {
            return;
        }

        var index = FindLyricIndex(time);
        if (index == _currentLyricIndex)
        {
            return;
        }

        if (_currentLyricIndex >= 0 && _currentLyricIndex < Lyrics.Count)
        {
            Lyrics[_currentLyricIndex].IsActive = false;
        }

        _currentLyricIndex = index;
        if (_currentLyricIndex >= 0 && _currentLyricIndex < Lyrics.Count)
        {
            Lyrics[_currentLyricIndex].IsActive = true;
        }
    }

    private int FindLyricIndex(TimeSpan time)
    {
        var low = 0;
        var high = Lyrics.Count - 1;
        var best = -1;

        while (low <= high)
        {
            var mid = (low + high) / 2;
            var midTime = Lyrics[mid].Time;

            if (midTime <= time)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private void SetNowPlaying(int index)
    {
        if (_nowPlayingIndex >= 0 && _nowPlayingIndex < PlaylistEntries.Count)
        {
            PlaylistEntries[_nowPlayingIndex].IsNowPlaying = false;
        }

        _nowPlayingIndex = index;
        if (_nowPlayingIndex >= 0 && _nowPlayingIndex < PlaylistEntries.Count)
        {
            PlaylistEntries[_nowPlayingIndex].IsNowPlaying = true;
        }
    }

    private async Task HandlePlaybackEndedAsync()
    {
        if (_isPlaylistTransition)
        {
            return;
        }

        if (PlaylistEntries.Count == 0 || _currentPlaylistIndex < 0)
        {
            return;
        }

        var nextIndex = _currentPlaylistIndex + 1;
        if (nextIndex >= PlaylistEntries.Count)
        {
            if (LoopPlayback)
            {
                nextIndex = 0;
            }
            else
            {
                return;
            }
        }

        await PlayPlaylistIndexAsync(nextIndex);
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
                SendDefaultInstrumentToDevice();
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
        if (_disposed) return;

        // Update on UI thread using BeginInvoke to prevent deadlock
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed) return;

            if (_forceStopPosition)
            {
                if (e.Time > TimeSpan.Zero)
                {
                    return;
                }

                _forceStopPosition = false;
            }

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
                ApplyDefaultInstrumentToChannelStates();
                OnPropertyChanged(nameof(ChannelStates));
            }

            UpdateCurrentLyric(e.Time);
        });
    }

    private void SendDefaultInstrumentToDevice()
    {
        if (_fileData == null || _fileHasProgramChanges || _midiOutput == null)
        {
            return;
        }

        if (_fileUsedChannels.Length == 0)
        {
            return;
        }

        var program = (byte)_defaultInstrumentProgram;
        foreach (var channel in _fileUsedChannels)
        {
            var status = (byte)(0xC0 | channel);
            _midiOutput.SendShortMessage(status, program, 0);
        }
    }

    private void ApplyDefaultInstrumentToChannelStates()
    {
        if (_fileData == null || _fileHasProgramChanges)
        {
            return;
        }

        if (_fileUsedChannels.Length == 0)
        {
            return;
        }

        if (_channelStates.Length == 0)
        {
            return;
        }

        var program = (byte)_defaultInstrumentProgram;
        foreach (var channel in _fileUsedChannels)
        {
            if (channel < _channelStates.Length)
            {
                _channelStates[channel] = _channelStates[channel] with
                {
                    Program = program,
                    HasProgramChange = true
                };
            }
        }
    }

    private void OnStateChanged(object? sender, PlaybackStateChangedEventArgs e)
    {
        if (_disposed) return;

        // Use BeginInvoke to prevent deadlock when stopping playback
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed) return;

            PlaybackState = e.NewState;

            if (e.NewState == PlaybackState.Stopped)
            {
                if (_userStopRequested)
                {
                    _userStopRequested = false;
                    return;
                }

                _ = HandlePlaybackEndedAsync();
            }
        });
    }

    private void OnNoteActivityChanged(object? sender, NoteActivityChangedEventArgs e)
    {
        if (_disposed) return;

        // Use BeginInvoke to prevent deadlock when stopping playback
        System.Windows.Application.Current?.Dispatcher.BeginInvoke(() =>
        {
            if (_disposed) return;
            LiveNoteChanged?.Invoke(this, new LiveNoteChanged(e.Channel, e.Note, e.IsActive));
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
        // Set disposed flag first to prevent event handlers from running
        _disposed = true;

        SaveSettings();
        if (_engine != null)
        {
            _engine.PositionChanged -= OnPositionChanged;
            _engine.StateChanged -= OnStateChanged;
            _engine.NoteActivityChanged -= OnNoteActivityChanged;
        }
        PlaylistEntries.CollectionChanged -= OnPlaylistChanged;
        TrackPlaybackStates.CollectionChanged -= OnTrackPlaybackStatesChanged;
        foreach (var track in TrackPlaybackStates)
        {
            track.PropertyChanged -= OnTrackPlaybackStateChanged;
        }
        CancelPlaylistParsing();

        // Stop and dispose engine - these should be quick now that handlers are detached
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

public sealed record InstrumentOption(byte Program, string Name);

public sealed record LiveNoteChanged(byte Channel, byte Note, bool IsActive);

public sealed record LoadResult(
    MidiFileData FileData,
    StateSnapshotBuilder SnapshotBuilder,
    bool FileHasProgramChanges,
    byte[] FileUsedChannels,
    ChannelState[] InitialChannelStates);

public sealed record PlaylistMetadataCache(
    PlaylistMetadata Metadata,
    DateTime LastWriteTimeUtc,
    long Length);
