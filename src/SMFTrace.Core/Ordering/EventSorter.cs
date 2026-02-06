using SMFTrace.Core.Models;

namespace SMFTrace.Core.Ordering;

/// <summary>
/// Sorts a collection of MIDI events according to the intra-tick ordering rules.
/// </summary>
public static class EventSorter
{
    /// <summary>
    /// Sorts events in place using the intra-tick ordering rules.
    /// </summary>
    /// <param name="events">The events to sort.</param>
    public static void Sort(List<MidiEventBase> events)
    {
        // Use stable sort to preserve relative order for equal priorities
        events.Sort(IntraTickComparer.Instance);
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
