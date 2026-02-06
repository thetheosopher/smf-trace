namespace SMFTrace.Core.Models;

/// <summary>
/// Base class for all MIDI events in the flattened timeline.
/// </summary>
public abstract record MidiEventBase
{
    /// <summary>Absolute tick position in the file.</summary>
    public required long AbsoluteTick { get; init; }

    /// <summary>Zero-based track index from which this event originated.</summary>
    public required int TrackIndex { get; init; }

    /// <summary>Original index within the track (for stable sorting).</summary>
    public required int OriginalIndex { get; init; }

    /// <summary>Time offset from the start of the file.</summary>
    public TimeSpan Time { get; init; }

    /// <summary>Raw bytes of the MIDI message.</summary>
    public required byte[] RawBytes { get; init; }
}

/// <summary>
/// Base class for channel voice messages.
/// </summary>
public abstract record ChannelEventBase : MidiEventBase
{
    /// <summary>MIDI channel (0-15).</summary>
    public required byte Channel { get; init; }
}

/// <summary>
/// Note On event (status 0x9n).
/// </summary>
public sealed record NoteOnEvent : ChannelEventBase
{
    /// <summary>MIDI note number (0-127).</summary>
    public required byte NoteNumber { get; init; }

    /// <summary>Velocity (1-127; velocity 0 is treated as NoteOff).</summary>
    public required byte Velocity { get; init; }
}

/// <summary>
/// Note Off event (status 0x8n, or NoteOn with velocity 0).
/// </summary>
public sealed record NoteOffEvent : ChannelEventBase
{
    /// <summary>MIDI note number (0-127).</summary>
    public required byte NoteNumber { get; init; }

    /// <summary>Release velocity (0-127).</summary>
    public required byte Velocity { get; init; }
}

/// <summary>
/// Control Change event (status 0xBn).
/// </summary>
public sealed record ControlChangeEvent : ChannelEventBase
{
    /// <summary>Controller number (0-127).</summary>
    public required byte ControllerNumber { get; init; }

    /// <summary>Controller value (0-127).</summary>
    public required byte Value { get; init; }

    /// <summary>True if this is Bank Select MSB (CC0).</summary>
    public bool IsBankSelectMsb => ControllerNumber == 0;

    /// <summary>True if this is Bank Select LSB (CC32).</summary>
    public bool IsBankSelectLsb => ControllerNumber == 32;

    /// <summary>True if this is any Bank Select message.</summary>
    public bool IsBankSelect => IsBankSelectMsb || IsBankSelectLsb;
}

/// <summary>
/// Program Change event (status 0xCn).
/// </summary>
public sealed record ProgramChangeEvent : ChannelEventBase
{
    /// <summary>Program number (0-127).</summary>
    public required byte ProgramNumber { get; init; }
}

/// <summary>
/// Pitch Bend event (status 0xEn).
/// </summary>
public sealed record PitchBendEvent : ChannelEventBase
{
    /// <summary>14-bit pitch bend value (0-16383, center = 8192).</summary>
    public required ushort Value { get; init; }
}

/// <summary>
/// Channel Pressure / Aftertouch event (status 0xDn).
/// </summary>
public sealed record ChannelPressureEvent : ChannelEventBase
{
    /// <summary>Pressure value (0-127).</summary>
    public required byte Pressure { get; init; }
}

/// <summary>
/// Polyphonic Key Pressure event (status 0xAn).
/// </summary>
public sealed record PolyPressureEvent : ChannelEventBase
{
    /// <summary>MIDI note number (0-127).</summary>
    public required byte NoteNumber { get; init; }

    /// <summary>Pressure value (0-127).</summary>
    public required byte Pressure { get; init; }
}
