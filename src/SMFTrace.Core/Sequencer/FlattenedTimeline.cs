using SMFTrace.Core.Models;

namespace SMFTrace.Core.Sequencer;

/// <summary>
/// Provides indexed access to a flattened and sorted MIDI event timeline.
/// </summary>
public sealed class FlattenedTimeline
{
    private readonly IReadOnlyList<MidiEventBase> _events;
    private readonly Dictionary<LaneId, List<int>> _eventsByLane;

    /// <summary>
    /// Creates a new timeline from the given events.
    /// Events must already be sorted.
    /// </summary>
    public FlattenedTimeline(IReadOnlyList<MidiEventBase> events)
    {
        _events = events;
        _eventsByLane = BuildLaneIndex(events);
    }

    /// <summary>All events in order.</summary>
    public IReadOnlyList<MidiEventBase> Events => _events;

    /// <summary>Number of events.</summary>
    public int Count => _events.Count;

    /// <summary>Gets the event at the specified index.</summary>
    public MidiEventBase this[int index] => _events[index];

    /// <summary>All unique lanes (track, channel combinations).</summary>
    public IReadOnlyCollection<LaneId> Lanes => _eventsByLane.Keys;

    /// <summary>
    /// Finds the index of the first event at or after the specified tick.
    /// Returns Count if no such event exists.
    /// </summary>
    public int FindIndexAtOrAfter(long tick)
    {
        if (_events.Count == 0)
        {
            return 0;
        }

        var low = 0;
        var high = _events.Count;

        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (_events[mid].AbsoluteTick < tick)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>
    /// Finds the index of the first event at or after the specified time.
    /// Returns Count if no such event exists.
    /// </summary>
    public int FindIndexAtOrAfter(TimeSpan time)
    {
        if (_events.Count == 0)
        {
            return 0;
        }

        var low = 0;
        var high = _events.Count;

        while (low < high)
        {
            var mid = low + ((high - low) / 2);
            if (_events[mid].Time < time)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>
    /// Gets events in the specified tick range [startTick, endTick).
    /// </summary>
    public IEnumerable<MidiEventBase> GetEventsInRange(long startTick, long endTick)
    {
        var startIndex = FindIndexAtOrAfter(startTick);
        for (var i = startIndex; i < _events.Count && _events[i].AbsoluteTick < endTick; i++)
        {
            yield return _events[i];
        }
    }

    /// <summary>
    /// Gets events in the specified time range [start, end).
    /// </summary>
    public IEnumerable<MidiEventBase> GetEventsInRange(TimeSpan start, TimeSpan end)
    {
        var startIndex = FindIndexAtOrAfter(start);
        for (var i = startIndex; i < _events.Count && _events[i].Time < end; i++)
        {
            yield return _events[i];
        }
    }

    /// <summary>
    /// Gets all channel events for a specific lane.
    /// </summary>
    public IEnumerable<ChannelEventBase> GetEventsForLane(LaneId lane)
    {
        if (!_eventsByLane.TryGetValue(lane, out var indices))
        {
            yield break;
        }

        foreach (var index in indices)
        {
            if (_events[index] is ChannelEventBase channelEvent)
            {
                yield return channelEvent;
            }
        }
    }

    /// <summary>
    /// Gets all note events (NoteOn/NoteOff) for a specific lane.
    /// </summary>
    public IEnumerable<MidiEventBase> GetNoteEventsForLane(LaneId lane)
    {
        return GetEventsForLane(lane).Where(e => e is NoteOnEvent or NoteOffEvent);
    }

    private static Dictionary<LaneId, List<int>> BuildLaneIndex(IReadOnlyList<MidiEventBase> events)
    {
        var index = new Dictionary<LaneId, List<int>>();

        for (var i = 0; i < events.Count; i++)
        {
            if (events[i] is ChannelEventBase channelEvent)
            {
                var lane = new LaneId(channelEvent.TrackIndex, channelEvent.Channel);
                if (!index.TryGetValue(lane, out var list))
                {
                    list = [];
                    index[lane] = list;
                }

                list.Add(i);
            }
        }

        return index;
    }
}
