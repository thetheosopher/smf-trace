using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.ViewModels;

/// <summary>
/// Wrapped MIDI event for display in the diagnostics list.
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed class DiagnosticEventViewModel : INotifyPropertyChanged
#pragma warning restore CA1711
{
    private bool _isCurrent;
    private readonly MidiEventBase _event;
    private readonly string _eventType;
    private readonly string _channel;
    private readonly string _summary;
    private readonly string _rawBytesHex;
    private readonly EventCategory _category;
    private readonly bool _isMeta;
    private readonly bool _isSysExEvent;
    private readonly bool _isChannel;

    public DiagnosticEventViewModel(MidiEventBase evt, int index)
    {
        _event = evt;
        Index = index;
        _eventType = BuildEventType(evt);
        _channel = BuildChannel(evt);
        _summary = BuildSummary(evt);
        _rawBytesHex = BuildRawBytesHex(evt.RawBytes);
        _category = BuildCategory(evt);
        _isMeta = evt is MetaEvent;
        _isSysExEvent = evt is SysExEvent;
        _isChannel = evt is ChannelEventBase;
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
    public string EventType => _eventType;

    /// <summary>Channel number (1-16) or empty for non-channel events.</summary>
    public string Channel => _channel;

    /// <summary>Summary of event data.</summary>
    public string Summary => _summary;

    /// <summary>Underlying event for detail view.</summary>
    public MidiEventBase Event => _event;

    /// <summary>Raw bytes as hex string.</summary>
    public string RawBytesHex => _rawBytesHex;

    /// <summary>Whether this is a meta event.</summary>
    public bool IsMeta => _isMeta;

    /// <summary>Whether this is a SysEx event.</summary>
    public bool IsSysExEvent => _isSysExEvent;

    /// <summary>Whether this is a channel event.</summary>
    public bool IsChannel => _isChannel;

    /// <summary>Category for filtering.</summary>
    public EventCategory Category => _category;

    /// <summary>Whether this event is at the current playback position.</summary>
    public bool IsCurrent
    {
        get => _isCurrent;
        set
        {
            if (_isCurrent != value)
            {
                _isCurrent = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string NoteNumberToName(byte noteNumber)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (noteNumber / 12) - 1;
        var note = noteNames[noteNumber % 12];
        return $"{note}{octave}";
    }

    private static string BuildEventType(MidiEventBase evt) => evt switch
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

    private static string BuildChannel(MidiEventBase evt) => evt switch
    {
        ChannelEventBase ch => (ch.Channel + 1).ToString(CultureInfo.InvariantCulture),
        _ => ""
    };

    private static string BuildSummary(MidiEventBase evt) => evt switch
    {
        NoteOnEvent n => $"Note {NoteNumberToName(n.NoteNumber)} Vel {n.Velocity}",
        NoteOffEvent n => $"Note {NoteNumberToName(n.NoteNumber)} Vel {n.Velocity}",
        ControlChangeEvent cc => $"CC{cc.ControllerNumber} = {cc.Value}",
        ProgramChangeEvent pc => $"Program {pc.ProgramNumber}",
        PitchBendEvent pb => $"Bend {pb.Value}",
        ChannelPressureEvent cp => $"Pressure {cp.Pressure}",
        PolyPressureEvent pp => $"Key {pp.NoteNumber} Pressure {pp.Pressure}",
        MetaEvent meta => GetMetaSummary(meta),
        SysExEvent sysex => $"[{sysex.Data.Length} bytes] Mfr: {FormatHexBytes(sysex.ManufacturerId)}",
        _ => ""
    };

    private static EventCategory BuildCategory(MidiEventBase evt) => evt switch
    {
        NoteOnEvent or NoteOffEvent => EventCategory.Note,
        ControlChangeEvent => EventCategory.ControlChange,
        ProgramChangeEvent => EventCategory.ProgramChange,
        PitchBendEvent or ChannelPressureEvent or PolyPressureEvent => EventCategory.Other,
        MetaEvent => EventCategory.Meta,
        SysExEvent => EventCategory.SysExCategory,
        _ => EventCategory.Other
    };

    private static string BuildRawBytesHex(IReadOnlyList<byte> rawBytes)
    {
        if (rawBytes.Count == 0)
        {
            return string.Empty;
        }

        var chars = new char[(rawBytes.Count * 3) - 1];
        var position = 0;

        for (var i = 0; i < rawBytes.Count; i++)
        {
            var hex = rawBytes[i].ToString("X2", CultureInfo.InvariantCulture);
            chars[position++] = hex[0];
            chars[position++] = hex[1];

            if (i < rawBytes.Count - 1)
            {
                chars[position++] = ' ';
            }
        }

        return new string(chars);
    }

    private static string FormatHexBytes(IReadOnlyList<byte> bytes) => BuildRawBytesHex(bytes);

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
