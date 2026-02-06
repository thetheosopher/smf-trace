using SMFTrace.Core.Models;

namespace SMFTrace.Core.Ordering;

/// <summary>
/// Compares MIDI events for ordering within the same tick.
/// Enforces the required order: Bank Select → Program Change → other CC → NoteOn → NoteOff.
/// </summary>
public sealed class IntraTickComparer : IComparer<MidiEventBase>
{
    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static IntraTickComparer Instance { get; } = new();

    private IntraTickComparer() { }

    /// <summary>
    /// Compares two events for ordering.
    /// </summary>
    public int Compare(MidiEventBase? x, MidiEventBase? y)
    {
        if (ReferenceEquals(x, y))
        {
            return 0;
        }

        if (x is null)
        {
            return -1;
        }

        if (y is null)
        {
            return 1;
        }

        // First compare by absolute tick
        var tickComparison = x.AbsoluteTick.CompareTo(y.AbsoluteTick);
        if (tickComparison != 0)
        {
            return tickComparison;
        }

        // Same tick: compare by priority
        var priorityX = GetPriority(x);
        var priorityY = GetPriority(y);
        var priorityComparison = priorityX.CompareTo(priorityY);
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        // Same priority: preserve original order (stable sort) by track, then original index
        var trackComparison = x.TrackIndex.CompareTo(y.TrackIndex);
        if (trackComparison != 0)
        {
            return trackComparison;
        }

        return x.OriginalIndex.CompareTo(y.OriginalIndex);
    }

    /// <summary>
    /// Gets the sorting priority for an event.
    /// Lower values sort first.
    /// </summary>
    /// <remarks>
    /// Priority order:
    /// 0 - Meta events (tempo, time sig, etc.)
    /// 1 - Bank Select (CC0 MSB, CC32 LSB)
    /// 2 - Program Change
    /// 3 - Other CC (controllers affecting note onset)
    /// 4 - NoteOn
    /// 5 - NoteOff
    /// 6 - Other channel messages (pitch bend, aftertouch)
    /// 7 - SysEx
    /// </remarks>
    public static int GetPriority(MidiEventBase evt) => evt switch
    {
        MetaEvent => 0,
        ControlChangeEvent cc when cc.IsBankSelect => 1,
        ProgramChangeEvent => 2,
        ControlChangeEvent => 3,
        NoteOnEvent => 4,
        NoteOffEvent => 5,
        ChannelPressureEvent => 6,
        PolyPressureEvent => 6,
        PitchBendEvent => 6,
        SysExEvent => 7,
        _ => 8
    };
}
