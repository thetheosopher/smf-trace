using SMFTrace.Core.Models;

namespace SMFTrace.Core.Ordering;

/// <summary>
/// Sorts a collection of MIDI events according to the intra-tick ordering rules.
/// </summary>
public static class EventSorter
{
    private readonly record struct IndexedEvent(MidiEventBase Event, int Index);

    /// <summary>
    /// Sorts events in place using the intra-tick ordering rules.
    /// </summary>
    /// <param name="events">The events to sort.</param>
    public static void Sort(List<MidiEventBase> events)
    {
        if (events.Count <= 1)
        {
            return;
        }

        // Use a stable sort to preserve relative input order for fully equal comparisons.
        var indexed = events
            .Select((evt, index) => new IndexedEvent(evt, index))
            .ToList();

        indexed.Sort((left, right) =>
        {
            var comparison = IntraTickComparer.Instance.Compare(left.Event, right.Event);
            return comparison != 0 ? comparison : left.Index.CompareTo(right.Index);
        });

        for (var i = 0; i < indexed.Count; i++)
        {
            events[i] = indexed[i].Event;
        }
    }

    /// <summary>
    /// Returns a new sorted list of events.
    /// </summary>
    /// <param name="events">The source events.</param>
    /// <returns>A new list sorted according to intra-tick ordering rules.</returns>
    public static List<MidiEventBase> Sorted(IEnumerable<MidiEventBase> events)
    {
        var list = events.ToList();
        Sort(list);
        return list;
    }

    /// <summary>
    /// Validates that events are correctly ordered.
    /// </summary>
    /// <param name="events">The events to validate.</param>
    /// <returns>True if all events are in correct order.</returns>
    public static bool IsCorrectlyOrdered(IReadOnlyList<MidiEventBase> events)
    {
        for (var i = 1; i < events.Count; i++)
        {
            if (IntraTickComparer.Instance.Compare(events[i - 1], events[i]) > 0)
            {
                return false;
            }
        }

        return true;
    }
}
