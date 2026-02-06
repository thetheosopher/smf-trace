using System.Globalization;
using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// Wrapped MIDI event for display in the diagnostics list.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class DiagnosticEventViewModel
#pragma warning restore CA1711
{
    private readonly MidiEventBase _event;

    public DiagnosticEventViewModel(MidiEventBase evt, int index)
    {
        _event = evt;
        Index = index;
    }

    /// <summary>List index for display.</summary>
    public int Index { get; }

    /// <summary>Absolute tick position.</summary>
    public long Tick => _event.AbsoluteTick;

    /// <summary>Time offset from start.</summary>
    public TimeSpan Time => _event.Time;

    /// <summary>Time formatted as mm:ss.fff.</summary>
    public string TimeDisplay => Time.ToString(@"mm\:ss\.fff", CultureInfo.InvariantCulture);

    /// <summary>Track index.</summary>
    public int Track => _event.TrackIndex;

    /// <summary>Event type name for display.</summary>
    public string EventType => _event switch
    {
        NoteOnEvent => "Note On",
        NoteOffEvent => "Note Off",
        ControlChangeEvent => "Control Change",
        ProgramChangeEvent => "Program Change",
        PitchBendEvent => "Pitch Bend",
        ChannelPressureEvent => "Channel Pressure",
        PolyPressureEvent => "Poly Pressure",
        MetaEvent meta => $"Meta: {meta.TypeName}",
        SysExEvent => "SysEx",
        _ => "Unknown"
    };

    /// <summary>Channel number (1-16) or empty for non-channel events.</summary>
    public string Channel => _event switch
    {
        ChannelEventBase ch => (ch.Channel + 1).ToString(CultureInfo.InvariantCulture),
        _ => ""
    };

    /// <summary>Summary of event data.</summary>
    public string Summary => _event switch
    {
        NoteOnEvent n => $"Note {NoteNumberToName(n.NoteNumber)} Vel {n.Velocity}",
        NoteOffEvent n => $"Note {NoteNumberToName(n.NoteNumber)} Vel {n.Velocity}",
        ControlChangeEvent cc => $"CC{cc.ControllerNumber} = {cc.Value}",
        ProgramChangeEvent pc => $"Program {pc.ProgramNumber}",
        PitchBendEvent pb => $"Bend {pb.Value}",
        ChannelPressureEvent cp => $"Pressure {cp.Pressure}",
        PolyPressureEvent pp => $"Key {pp.NoteNumber} Pressure {pp.Pressure}",
        MetaEvent meta => GetMetaSummary(meta),
        SysExEvent sysex => $"[{sysex.Data.Length} bytes] Mfr: {sysex.ManufacturerId:X2}",
        _ => ""
    };

    /// <summary>Underlying event for detail view.</summary>
    public MidiEventBase Event => _event;

    /// <summary>Raw bytes as hex string.</summary>
    public string RawBytesHex => _event.RawBytes.Length > 0
        ? string.Join(" ", _event.RawBytes.Select(b => b.ToString("X2", CultureInfo.InvariantCulture)))
        : "";

    /// <summary>Whether this is a meta event.</summary>
    public bool IsMeta => _event is MetaEvent;

    /// <summary>Whether this is a SysEx event.</summary>
    public bool IsSysExEvent => _event is SysExEvent;

    /// <summary>Whether this is a channel event.</summary>
    public bool IsChannel => _event is ChannelEventBase;

    /// <summary>Category for filtering.</summary>
    public EventCategory Category => _event switch
    {
        NoteOnEvent or NoteOffEvent => EventCategory.Note,
        ControlChangeEvent => EventCategory.ControlChange,
        ProgramChangeEvent => EventCategory.ProgramChange,
        PitchBendEvent or ChannelPressureEvent or PolyPressureEvent => EventCategory.Other,
        MetaEvent => EventCategory.Meta,
        SysExEvent => EventCategory.SysExCategory,
        _ => EventCategory.Other
    };

    private static string NoteNumberToName(byte noteNumber)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (noteNumber / 12) - 1;
        var note = noteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    private static string GetMetaSummary(MetaEvent meta)
    {
        return meta.MetaType switch
        {
            0x00 => $"Seq#", // Sequence Number
            0x01 or 0x02 or 0x03 or 0x04 or 0x05 or 0x06 or 0x07 => TruncateText(meta.TextContent), // Text events
            0x20 => $"Channel", // Channel Prefix
            0x2F => "", // End of Track
            0x51 => $"{meta.Bpm:F1} BPM", // Set Tempo
            0x54 => "SMPTE", // SMPTE Offset
            0x58 => GetTimeSignature(meta), // Time Signature
            0x59 => GetKeySignature(meta), // Key Signature
            0x7F => $"[{meta.Data.Length} bytes]", // Sequencer Specific
            _ => ""
        };
    }

    private static string GetTimeSignature(MetaEvent meta)
    {
        if (meta.Data.Length >= 2)
        {
            var numerator = meta.Data[0];
            var denominator = (int)Math.Pow(2, meta.Data[1]);
            return $"{numerator}/{denominator}";
        }
        return "";
    }

    private static string GetKeySignature(MetaEvent meta)
    {
        if (meta.Data.Length >= 2)
        {
            var sf = (sbyte)meta.Data[0]; // sharps/flats
            var minor = meta.Data[1] != 0;
            return $"{sf} {(minor ? "Minor" : "Major")}";
        }
        return "";
    }

    private static string TruncateText(string? text, int maxLength = 30)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Length <= maxLength ? $"\"{text}\"" : $"\"{text[..maxLength]}...\"";
    }
}

/// <summary>
/// Event categories for filtering.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public enum EventCategory
{
    Note,
    ControlChange,
    ProgramChange,
    Meta,
    SysExCategory,
    Other
}
#pragma warning restore CA1711
