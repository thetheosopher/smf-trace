using System.Diagnostics;
using Melanchall.DryWetMidi.Interaction;
using SMFTrace.Core.Configuration;
using SMFTrace.Core.Models;

namespace SMFTrace.Core.Sequencer;

/// <summary>
/// Interface for MIDI output operations used by the sequencer.
/// </summary>
public interface ISequencerOutput
{
    void SendShortMessage(byte status, byte data1, byte data2);
#pragma warning disable CA1711 // SysEx is an industry-standard term
    void SendSysEx(ReadOnlySpan<byte> payload);
#pragma warning restore CA1711
    void AllNotesOff();
}

/// <summary>
/// High-precision MIDI sequencer engine with transport controls and seek support.
/// </summary>
public sealed class SequencerEngine : IDisposable
{
    private readonly MidiFileData _fileData;
    private readonly StateSnapshotBuilder _snapshotBuilder;
    private readonly PlaybackOptions _options;
    private readonly object _lock = new();
    private double _tempoMultiplier = 1.0;
    private readonly bool[,] _activeNotes = new bool[16, 128];
    private volatile bool _disposing;

    private ISequencerOutput? _output;
    private CancellationTokenSource? _playbackCts;
    private Task? _playbackTask;

    private int _currentEventIndex;
    private long _currentTick;
    private TimeSpan _currentTime;
    private PlaybackState _state = PlaybackState.Stopped;
    private PlaybackState _stateBeforeScrub = PlaybackState.Stopped;

    // Position update interval for smooth UI updates (60 Hz)
    private static readonly TimeSpan PositionUpdateInterval = TimeSpan.FromMilliseconds(1000.0 / 60.0);
    private TimeSpan _lastPositionUpdate;

    /// <summary>
    /// Raised when the playback position changes.
    /// </summary>
    public event EventHandler<PositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// Raised when the playback state changes.
    /// </summary>
    public event EventHandler<PlaybackStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// Raised when an event is dispatched to the output.
    /// </summary>
    public event EventHandler<EventDispatchedEventArgs>? EventDispatched;

    /// <summary>
    /// Raised when a note becomes active/inactive.
    /// </summary>
    public event EventHandler<NoteActivityChangedEventArgs>? NoteActivityChanged;

    /// <summary>
    /// Creates a new sequencer engine for the given file data.
    /// </summary>
    public SequencerEngine(MidiFileData fileData, PlaybackOptions? options = null)
    {
        _fileData = fileData ?? throw new ArgumentNullException(nameof(fileData));
        _options = options ?? new PlaybackOptions();
        _snapshotBuilder = new StateSnapshotBuilder(fileData.Events);
        _tempoMultiplier = Math.Clamp(_options.TempoMultiplier, 0.05, 4.0);
    }

    /// <summary>Current playback state.</summary>
    public PlaybackState State
    {
        get { lock (_lock) return _state; }
    }

    /// <summary>Current position in ticks.</summary>
    public long CurrentTick
    {
        get { lock (_lock) return _currentTick; }
    }

    /// <summary>Current position in time.</summary>
    public TimeSpan CurrentTime
    {
        get { lock (_lock) return _currentTime; }
    }

    /// <summary>
    /// Whether playback should loop at end of file.
    /// </summary>
    public bool LoopPlayback
    {
        get { lock (_lock) return _options.LoopPlayback; }
        set { lock (_lock) _options.LoopPlayback = value; }
    }

    public double TempoMultiplier
    {
        get { lock (_lock) return _tempoMultiplier; }
        set
        {
            var clamped = Math.Clamp(value, 0.05, 4.0);
            lock (_lock)
            {
                _tempoMultiplier = clamped;
            }
        }
    }

    /// <summary>Total duration of the file.</summary>
    public TimeSpan Duration => _fileData.Duration;

    /// <summary>Total ticks in the file.</summary>
    public long TotalTicks => _fileData.TotalTicks;

    /// <summary>
    /// Sets the MIDI output device.
    /// </summary>
    public void SetOutput(ISequencerOutput? output)
    {
        lock (_lock)
        {
            _output = output;
        }
    }

    /// <summary>
    /// Starts or resumes playback.
    /// </summary>
    public void Play()
    {
        lock (_lock)
        {
            if (_state == PlaybackState.Playing)
            {
                return;
            }

            if (_output == null)
            {
                throw new InvalidOperationException("No output device set");
            }

            SetState(PlaybackState.Playing);
            StartPlaybackLoop();
        }
    }

    /// <summary>
    /// Pauses playback at the current position.
    /// Sends All Notes Off to prevent stuck notes.
    /// </summary>
    public void Pause()
    {
        // Check state before doing work
        bool wasPlaying;
        ISequencerOutput? output;
        lock (_lock)
        {
            wasPlaying = _state == PlaybackState.Playing;
            if (!wasPlaying) return;
            output = _output;
        }

        // Cancel and wait outside the lock to avoid deadlock
        _playbackCts?.Cancel();
        try
        {
            _playbackTask?.Wait(100);
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        // Send AllNotesOff outside the lock to prevent blocking
        output?.AllNotesOff();

        lock (_lock)
        {
            _playbackCts?.Dispose();
            _playbackCts = null;
            _playbackTask = null;

            ClearActiveNotes();
            SetState(PlaybackState.Paused);
        }
    }

    /// <summary>
    /// Stops playback, sends All Notes Off, and resets position to start.
    /// </summary>
    public void Stop()
    {
        // Capture output before doing work
        ISequencerOutput? output;
        lock (_lock)
        {
            output = _output;
        }

        // First cancel and wait outside the lock to avoid deadlock
        _playbackCts?.Cancel();
        try
        {
            _playbackTask?.Wait(100);
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        // Send AllNotesOff outside the lock to prevent blocking
        output?.AllNotesOff();

        lock (_lock)
        {
            _playbackCts?.Dispose();
            _playbackCts = null;
            _playbackTask = null;

            ClearActiveNotes();
            _currentEventIndex = 0;
            _currentTick = 0;
            _currentTime = TimeSpan.Zero;
            SetState(PlaybackState.Stopped);
            RaisePositionChanged();
        }
    }

    /// <summary>
    /// Begins silent scrubbing (no MIDI output during drag).
    /// </summary>
    public void BeginScrub()
    {
        bool wasPlaying;
        lock (_lock)
        {
            _stateBeforeScrub = _state;
            wasPlaying = _state == PlaybackState.Playing;
        }

        if (wasPlaying)
        {
            // Cancel and wait outside the lock to avoid deadlock
            _playbackCts?.Cancel();
            try
            {
                _playbackTask?.Wait(100);
            }
            catch (AggregateException)
            {
                // Expected on cancellation
            }
        }

        lock (_lock)
        {
            if (wasPlaying)
            {
                _playbackCts?.Dispose();
                _playbackCts = null;
                _playbackTask = null;
            }

            SetState(PlaybackState.Scrubbing);
        }
    }

    /// <summary>
    /// Updates the scrub position (silent, no output).
    /// </summary>
    /// <param name="time">The time to scrub to.</param>
    public void Scrub(TimeSpan time)
    {
        lock (_lock)
        {
            if (_state != PlaybackState.Scrubbing)
            {
                return;
            }

            // Clamp to valid range
            time = time < TimeSpan.Zero ? TimeSpan.Zero : time;
            time = time > _fileData.Duration ? _fileData.Duration : time;

            // Convert time to tick
            var metric = new MetricTimeSpan(time);
            var tick = TimeConverter.ConvertFrom(metric, _fileData.TempoMap);

            _currentTime = time;
            _currentTick = tick;
            _currentEventIndex = _snapshotBuilder.GetResumeEventIndex(tick);

            RaisePositionChanged();
        }
    }

    /// <summary>
    /// Ends scrubbing: sends All Notes Off, rebuilds state, emits to device, and resumes if was playing.
    /// </summary>
    public void EndScrub()
    {
        lock (_lock)
        {
            if (_state != PlaybackState.Scrubbing)
            {
                return;
            }

            // 1. All Notes Off
            _output?.AllNotesOff();

            // 2. Rebuild channel state at current tick
            var channelStates = _snapshotBuilder.RebuildStateAtTick(_currentTick);

            // 3. Emit bank/program/controller state to device
            EmitStateToDevice(channelStates);

            // 4. Update event index for resumption
            _currentEventIndex = _snapshotBuilder.GetResumeEventIndex(_currentTick);

            // 5. Resume playback if was playing before scrub
            if (_stateBeforeScrub == PlaybackState.Playing)
            {
                SetState(PlaybackState.Playing);
                StartPlaybackLoop();
            }
            else
            {
                SetState(_stateBeforeScrub == PlaybackState.Stopped ? PlaybackState.Stopped : PlaybackState.Paused);
            }
        }
    }

    /// <summary>
    /// Seeks to a specific time position.
    /// If playing, playback will continue from the new position.
    /// </summary>
    public void SeekTo(TimeSpan time)
    {
        BeginScrub();
        Scrub(time);
        EndScrub();
    }

    private void StartPlaybackLoop()
    {
        _playbackCts = new CancellationTokenSource();
        var token = _playbackCts.Token;

        _playbackTask = Task.Run(() => PlaybackLoop(token), token);
    }

    private void StopPlaybackLoop()
    {
        _playbackCts?.Cancel();
        try
        {
            _playbackTask?.Wait(100);
        }
        catch (AggregateException)
        {
            // Expected on cancellation
        }

        _playbackCts?.Dispose();
        _playbackCts = null;
        _playbackTask = null;
    }

    private void PlaybackLoop(CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = _currentTime;
        var lastSpeed = 1.0;
        lock (_lock)
        {
            lastSpeed = _tempoMultiplier;
        }
        _lastPositionUpdate = TimeSpan.Zero;

        while (!token.IsCancellationRequested)
        {
            MidiEventBase? nextEvent;
            TimeSpan eventTime;
            int eventIndex;
            bool shouldStop = false;
            bool shouldLoop = false;

            lock (_lock)
            {
                if (_currentEventIndex >= _fileData.Events.Count)
                {
                    // End of file
                    if (_options.LoopPlayback)
                    {
                        shouldLoop = true;
                    }
                    else
                    {
                        shouldStop = true;
                    }
                }
            }

            if (shouldStop)
            {
                Stop();
                return;
            }

            if (shouldLoop)
            {
                ResetForLoop(stopwatch);
                startTime = TimeSpan.Zero;
                continue;
            }

            lock (_lock)
            {
                nextEvent = _fileData.Events[_currentEventIndex];
                eventTime = nextEvent.Time;
                eventIndex = _currentEventIndex;

                var elapsed = stopwatch.Elapsed;
                _currentTime = GetScaledTime(ref startTime, elapsed, ref lastSpeed, _tempoMultiplier);
                _currentTick = TimeToTick(_currentTime);
            }

            // Wait until it's time to dispatch this event
            var timeUntilEvent = eventTime - _currentTime;
            if (timeUntilEvent > TimeSpan.Zero)
            {
                // Use spin-wait for sub-millisecond precision, but also update position periodically
                while (timeUntilEvent > TimeSpan.Zero && !token.IsCancellationRequested)
                {
                    var speed = lastSpeed;
                    var realTimeUntilEvent = speed > 0.0001
                        ? TimeSpan.FromTicks((long)(timeUntilEvent.Ticks / speed))
                        : timeUntilEvent;

                    // Sleep for a short time, but wake up for position updates
                    var sleepTime = Math.Min(realTimeUntilEvent.TotalMilliseconds, PositionUpdateInterval.TotalMilliseconds);
                    if (sleepTime > 1)
                    {
                        Thread.Sleep((int)sleepTime);
                    }
                    else
                    {
                        Thread.SpinWait(10);
                    }

                    // Update current time
                    lock (_lock)
                    {
                        var elapsed = stopwatch.Elapsed;
                        _currentTime = GetScaledTime(ref startTime, elapsed, ref lastSpeed, _tempoMultiplier);
                        _currentTick = TimeToTick(_currentTime);
                    }

                    // Raise periodic position update for smooth UI
                    var elapsedForUpdate = stopwatch.Elapsed;
                    if (elapsedForUpdate - _lastPositionUpdate >= PositionUpdateInterval)
                    {
                        _lastPositionUpdate = elapsedForUpdate;
                        RaisePositionChanged();
                    }

                    timeUntilEvent = eventTime - _currentTime;
                }

                if (token.IsCancellationRequested)
                {
                    return;
                }
            }

            // Dispatch the event
            lock (_lock)
            {
                if (_currentEventIndex == eventIndex)
                {
                    DispatchEvent(nextEvent);
                    _currentEventIndex++;
                    _currentTick = nextEvent.AbsoluteTick;
                    _currentTime = nextEvent.Time;
                }
            }

            // Always raise position update after dispatching event
            _lastPositionUpdate = stopwatch.Elapsed;
            RaisePositionChanged();
        }
    }

    private static TimeSpan GetScaledTime(ref TimeSpan startTime, TimeSpan elapsed, ref double lastSpeed, double speed)
    {
        if (Math.Abs(speed - lastSpeed) > 0.0001)
        {
            var currentTime = startTime + ScaleElapsed(elapsed, lastSpeed);
            startTime = currentTime - ScaleElapsed(elapsed, speed);
            lastSpeed = speed;
        }

        return startTime + ScaleElapsed(elapsed, speed);
    }

    private static TimeSpan ScaleElapsed(TimeSpan elapsed, double speed)
    {
        var ticks = (long)(elapsed.Ticks * speed);
        return ticks <= 0 ? TimeSpan.Zero : TimeSpan.FromTicks(ticks);
    }

    private void ResetForLoop(Stopwatch stopwatch)
    {
        lock (_lock)
        {
            _output?.AllNotesOff();
            ClearActiveNotes();
            _currentEventIndex = 0;
            _currentTick = 0;
            _currentTime = TimeSpan.Zero;

            var channelStates = _snapshotBuilder.RebuildStateAtTick(0);
            EmitStateToDevice(channelStates);

            _lastPositionUpdate = TimeSpan.Zero;
            RaisePositionChanged();
        }

        stopwatch.Restart();
    }

    private void DispatchEvent(MidiEventBase evt)
    {
        if (_output == null)
        {
            return;
        }

        switch (evt)
        {
            case NoteOnEvent noteOn:
                UpdateActiveNote(noteOn.Channel, noteOn.NoteNumber, noteOn.Velocity > 0);
                _output.SendShortMessage(
                    (byte)(0x90 | noteOn.Channel),
                    noteOn.NoteNumber,
                    noteOn.Velocity);
                break;

            case NoteOffEvent noteOff:
                UpdateActiveNote(noteOff.Channel, noteOff.NoteNumber, false);
                _output.SendShortMessage(
                    (byte)(0x80 | noteOff.Channel),
                    noteOff.NoteNumber,
                    noteOff.Velocity);
                break;

            case ControlChangeEvent cc:
                _output.SendShortMessage(
                    (byte)(0xB0 | cc.Channel),
                    cc.ControllerNumber,
                    cc.Value);
                break;

            case ProgramChangeEvent pc:
                _output.SendShortMessage(
                    (byte)(0xC0 | pc.Channel),
                    pc.ProgramNumber,
                    0);
                break;

            case PitchBendEvent pb:
                _output.SendShortMessage(
                    (byte)(0xE0 | pb.Channel),
                    (byte)(pb.Value & 0x7F),
                    (byte)((pb.Value >> 7) & 0x7F));
                break;

            case ChannelPressureEvent cp:
                _output.SendShortMessage(
                    (byte)(0xD0 | cp.Channel),
                    cp.Pressure,
                    0);
                break;

            case PolyPressureEvent pp:
                _output.SendShortMessage(
                    (byte)(0xA0 | pp.Channel),
                    pp.NoteNumber,
                    pp.Pressure);
                break;

            case SysExEvent sysex:
                _output.SendSysEx(sysex.Data);
                break;
        }

        EventDispatched?.Invoke(this, new EventDispatchedEventArgs(evt));
    }

    private void UpdateActiveNote(byte channel, byte note, bool isActive)
    {
        if (channel > 15 || note > 127)
        {
            return;
        }

        var current = _activeNotes[channel, note];
        if (current == isActive)
        {
            return;
        }

        _activeNotes[channel, note] = isActive;
        if (!_disposing)
        {
            NoteActivityChanged?.Invoke(this, new NoteActivityChangedEventArgs(channel, note, isActive));
        }
    }

    private void ClearActiveNotes()
    {
        for (byte channel = 0; channel < 16; channel++)
        {
            for (byte note = 0; note < 128; note++)
            {
                if (_activeNotes[channel, note])
                {
                    _activeNotes[channel, note] = false;
                    if (!_disposing)
                    {
                        NoteActivityChanged?.Invoke(this, new NoteActivityChangedEventArgs(channel, note, false));
                    }
                }
            }
        }
    }

    private void EmitStateToDevice(ChannelState[] states)
    {
        if (_output == null)
        {
            return;
        }

        for (byte channel = 0; channel < 16; channel++)
        {
            var state = states[channel];

            if (state.HasProgramChange)
            {
                // Send Bank Select MSB (CC0)
                _output.SendShortMessage((byte)(0xB0 | channel), 0, state.BankMsb);

                // Send Bank Select LSB (CC32)
                _output.SendShortMessage((byte)(0xB0 | channel), 32, state.BankLsb);

                // Send Program Change
                _output.SendShortMessage((byte)(0xC0 | channel), state.Program, 0);
            }

            // Send all controller values
            foreach (var (cc, value) in state.Controllers)
            {
                _output.SendShortMessage((byte)(0xB0 | channel), cc, value);
            }
        }
    }

    private long TimeToTick(TimeSpan time)
    {
        var metric = new MetricTimeSpan(time);
        return TimeConverter.ConvertFrom(metric, _fileData.TempoMap);
    }

    private void SetState(PlaybackState newState)
    {
        var oldState = _state;
        _state = newState;
        if (oldState != newState && !_disposing)
        {
            StateChanged?.Invoke(this, new PlaybackStateChangedEventArgs(oldState, newState));
        }
    }

    private void RaisePositionChanged()
    {
        if (_disposing) return;
        PositionChanged?.Invoke(this, new PositionChangedEventArgs(_currentTick, _currentTime));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposing = true;
        StopPlaybackLoop();
    }
}

/// <summary>
/// Event arguments for position changes.
/// </summary>
public sealed class PositionChangedEventArgs : EventArgs
{
    public long Tick { get; }
    public TimeSpan Time { get; }

    public PositionChangedEventArgs(long tick, TimeSpan time)
    {
        Tick = tick;
        Time = time;
    }
}

/// <summary>
/// Event arguments for state changes.
/// </summary>
public sealed class PlaybackStateChangedEventArgs : EventArgs
{
    public PlaybackState OldState { get; }
    public PlaybackState NewState { get; }

    public PlaybackStateChangedEventArgs(PlaybackState oldState, PlaybackState newState)
    {
        OldState = oldState;
        NewState = newState;
    }
}

/// <summary>
/// Event arguments for event dispatch.
/// </summary>
public sealed class EventDispatchedEventArgs : EventArgs
{
    public MidiEventBase Event { get; }

    public EventDispatchedEventArgs(MidiEventBase evt)
    {
        Event = evt;
    }
}

/// <summary>
/// Event arguments for note activity changes.
/// </summary>
public sealed class NoteActivityChangedEventArgs : EventArgs
{
    public byte Channel { get; }
    public byte Note { get; }
    public bool IsActive { get; }

    public NoteActivityChangedEventArgs(byte channel, byte note, bool isActive)
    {
        Channel = channel;
        Note = note;
        IsActive = isActive;
    }
}
