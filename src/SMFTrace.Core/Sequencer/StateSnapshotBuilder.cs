using SMFTrace.Core.Models;

namespace SMFTrace.Core.Sequencer;

/// <summary>
/// Mutable channel state for building snapshots.
/// </summary>
public sealed class MutableChannelState
{
    /// <summary>Bank Select MSB (CC0).</summary>
    public byte BankMsb { get; set; }

    /// <summary>Bank Select LSB (CC32).</summary>
    public byte BankLsb { get; set; }

    /// <summary>Program number.</summary>
    public byte Program { get; set; }

    /// <summary>Whether a Program Change has been received.</summary>
    public bool HasProgramChange { get; set; }

    /// <summary>Controller values by CC number.</summary>
    public Dictionary<byte, byte> Controllers { get; } = [];

    /// <summary>
    /// Creates an immutable snapshot of the current state.
    /// </summary>
    public ChannelState ToImmutable() => new()
    {
        BankMsb = BankMsb,
        BankLsb = BankLsb,
        Program = Program,
        HasProgramChange = HasProgramChange,
        Controllers = new Dictionary<byte, byte>(Controllers)
    };

    /// <summary>
    /// Resets to default state.
    /// </summary>
    public void Reset()
    {
        BankMsb = 0;
        BankLsb = 0;
        Program = 0;
        HasProgramChange = false;
        Controllers.Clear();
    }

    /// <summary>
    /// Copies values from an immutable state.
    /// </summary>
    public void CopyFrom(ChannelState state)
    {
        BankMsb = state.BankMsb;
        BankLsb = state.BankLsb;
        Program = state.Program;
        HasProgramChange = state.HasProgramChange;
        Controllers.Clear();
        foreach (var kvp in state.Controllers)
        {
            Controllers[kvp.Key] = kvp.Value;
        }
    }
}

/// <summary>
/// A snapshot of all 16 channel states at a specific tick.
/// </summary>
public sealed class StateCheckpoint
{
    /// <summary>The tick at which this checkpoint was created.</summary>
    public long Tick { get; init; }

    /// <summary>The event index at which this checkpoint was created.</summary>
    public int EventIndex { get; init; }

    /// <summary>Channel states (indexed 0-15).</summary>
    public required ChannelState[] Channels { get; init; }
}

/// <summary>
/// Builds and manages state checkpoints for fast seek operations.
/// </summary>
public sealed class StateSnapshotBuilder
{
    private readonly IReadOnlyList<MidiEventBase> _events;
    private readonly List<StateCheckpoint> _checkpoints = [];
    private readonly MutableChannelState[] _currentState;

    /// <summary>Default checkpoint interval in ticks (960 = 1 measure at 4/4, 480 PPQ).</summary>
    public const int DefaultCheckpointInterval = 960;

    /// <summary>
    /// Creates a new snapshot builder for the given events.
    /// </summary>
    /// <param name="events">The sorted event list.</param>
    /// <param name="checkpointInterval">Interval in ticks between checkpoints.</param>
    public StateSnapshotBuilder(IReadOnlyList<MidiEventBase> events, int checkpointInterval = DefaultCheckpointInterval)
    {
        _events = events;
        _currentState = new MutableChannelState[16];
        for (var i = 0; i < 16; i++)
        {
            _currentState[i] = new MutableChannelState();
        }

        BuildCheckpoints(checkpointInterval);
    }

    /// <summary>
    /// Gets all checkpoints.
    /// </summary>
    public IReadOnlyList<StateCheckpoint> Checkpoints => _checkpoints;

    /// <summary>
    /// Rebuilds channel state at a specific tick.
    /// </summary>
    /// <param name="targetTick">The tick to rebuild state for.</param>
    /// <returns>Array of 16 channel states.</returns>
    public ChannelState[] RebuildStateAtTick(long targetTick)
    {
        // Find the nearest checkpoint before the target tick
        var checkpoint = FindNearestCheckpoint(targetTick);

        // Start with checkpoint state or defaults
        for (var i = 0; i < 16; i++)
        {
            if (checkpoint != null)
            {
                _currentState[i].CopyFrom(checkpoint.Channels[i]);
            }
            else
            {
                _currentState[i].Reset();
            }
        }

        // Find the starting event index
        var startIndex = checkpoint?.EventIndex ?? 0;

        // Replay events from checkpoint to target tick
        for (var i = startIndex; i < _events.Count; i++)
        {
            var evt = _events[i];
            if (evt.AbsoluteTick > targetTick)
            {
                break;
            }

            ApplyEventToState(evt);
        }

        // Return immutable snapshots
        var result = new ChannelState[16];
        for (var i = 0; i < 16; i++)
        {
            result[i] = _currentState[i].ToImmutable();
        }

        return result;
    }

    /// <summary>
    /// Rebuilds channel state at a specific tick using a track activity mask.
    /// </summary>
    /// <param name="targetTick">The tick to rebuild state for.</param>
    /// <param name="activeTracks">Track activity mask (true = include).</param>
    /// <returns>Array of 16 channel states.</returns>
    public ChannelState[] RebuildStateAtTick(long targetTick, bool[]? activeTracks)
    {
        if (activeTracks == null || activeTracks.Length == 0 || AreAllTracksActive(activeTracks))
        {
            return RebuildStateAtTick(targetTick);
        }

        // Reset to defaults for a full rebuild with filtering.
        for (var i = 0; i < 16; i++)
        {
            _currentState[i].Reset();
        }

        for (var i = 0; i < _events.Count; i++)
        {
            var evt = _events[i];
            if (evt.AbsoluteTick > targetTick)
            {
                break;
            }

            if (!IsTrackActive(evt.TrackIndex, activeTracks))
            {
                continue;
            }

            ApplyEventToState(evt);
        }

        var result = new ChannelState[16];
        for (var i = 0; i < 16; i++)
        {
            result[i] = _currentState[i].ToImmutable();
        }

        return result;
    }

    /// <summary>
    /// Gets the event index to resume playback from after seeking to a tick.
    /// </summary>
    /// <param name="targetTick">The target tick.</param>
    /// <returns>The event index where playback should resume.</returns>
    public int GetResumeEventIndex(long targetTick)
    {
        for (var i = 0; i < _events.Count; i++)
        {
            if (_events[i].AbsoluteTick >= targetTick)
            {
                return i;
            }
        }

        return _events.Count;
    }

    private void BuildCheckpoints(int interval)
    {
        // Reset all state
        for (var i = 0; i < 16; i++)
        {
            _currentState[i].Reset();
        }

        // Create initial checkpoint at tick 0
        _checkpoints.Add(CreateCheckpoint(0, 0));

        long nextCheckpointTick = interval;

        for (var eventIndex = 0; eventIndex < _events.Count; eventIndex++)
        {
            var evt = _events[eventIndex];

            // Create checkpoint if we've passed the threshold
            while (evt.AbsoluteTick >= nextCheckpointTick)
            {
                _checkpoints.Add(CreateCheckpoint(nextCheckpointTick, eventIndex));
                nextCheckpointTick += interval;
            }

            ApplyEventToState(evt);
        }
    }

    private StateCheckpoint CreateCheckpoint(long tick, int eventIndex)
    {
        var channels = new ChannelState[16];
        for (var i = 0; i < 16; i++)
        {
            channels[i] = _currentState[i].ToImmutable();
        }

        return new StateCheckpoint
        {
            Tick = tick,
            EventIndex = eventIndex,
            Channels = channels
        };
    }

    private StateCheckpoint? FindNearestCheckpoint(long tick)
    {
        StateCheckpoint? best = null;

        foreach (var checkpoint in _checkpoints)
        {
            if (checkpoint.Tick <= tick)
            {
                best = checkpoint;
            }
            else
            {
                break; // Checkpoints are ordered by tick
            }
        }

        return best;
    }

    private void ApplyEventToState(MidiEventBase evt)
    {
        switch (evt)
        {
            case ControlChangeEvent cc:
                var state = _currentState[cc.Channel];
                if (cc.IsBankSelectMsb)
                {
                    state.BankMsb = cc.Value;
                }
                else if (cc.IsBankSelectLsb)
                {
                    state.BankLsb = cc.Value;
                }
                else
                {
                    state.Controllers[cc.ControllerNumber] = cc.Value;
                }
                break;

            case ProgramChangeEvent pc:
                var pcState = _currentState[pc.Channel];
                pcState.Program = pc.ProgramNumber;
                pcState.HasProgramChange = true;
                break;
        }
    }

    private static bool IsTrackActive(int trackIndex, bool[] activeTracks)
    {
        return trackIndex >= 0 && trackIndex < activeTracks.Length ? activeTracks[trackIndex] : true;
    }

    private static bool AreAllTracksActive(bool[] activeTracks)
    {
        for (var i = 0; i < activeTracks.Length; i++)
        {
            if (!activeTracks[i])
            {
                return false;
            }
        }

        return true;
    }
}
