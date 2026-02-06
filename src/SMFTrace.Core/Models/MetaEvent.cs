namespace SMFTrace.Core.Models;

/// <summary>
/// Meta event (FF type length data).
/// </summary>
public sealed record MetaEvent : MidiEventBase
{
    /// <summary>Meta event type byte.</summary>
    public required byte MetaType { get; init; }

    /// <summary>Meta event data payload.</summary>
    public required byte[] Data { get; init; }

    /// <summary>Gets a human-readable name for this meta event type.</summary>
    public string TypeName => MetaType switch
    {
        0x00 => "Sequence Number",
        0x01 => "Text",
        0x02 => "Copyright",
        0x03 => "Track Name",
        0x04 => "Instrument Name",
        0x05 => "Lyric",
        0x06 => "Marker",
        0x07 => "Cue Point",
        0x20 => "Channel Prefix",
        0x21 => "Port Prefix",
        0x2F => "End of Track",
        0x51 => "Set Tempo",
        0x54 => "SMPTE Offset",
        0x58 => "Time Signature",
        0x59 => "Key Signature",
        0x7F => "Sequencer Specific",
        _ => $"Unknown (0x{MetaType:X2})"
    };

    /// <summary>True if this is a Set Tempo event.</summary>
    public bool IsSetTempo => MetaType == 0x51;

    /// <summary>True if this is a Time Signature event.</summary>
    public bool IsTimeSignature => MetaType == 0x58;

    /// <summary>True if this is a Track Name event.</summary>
    public bool IsTrackName => MetaType == 0x03;

    /// <summary>True if this is an End of Track event.</summary>
    public bool IsEndOfTrack => MetaType == 0x2F;

    /// <summary>
    /// Gets the tempo in microseconds per quarter note (only valid if IsSetTempo).
    /// </summary>
    public int MicrosecondsPerQuarterNote =>
        IsSetTempo && Data.Length >= 3
            ? (Data[0] << 16) | (Data[1] << 8) | Data[2]
            : 500000; // Default 120 BPM

    /// <summary>
    /// Gets the tempo in BPM (only valid if IsSetTempo).
    /// </summary>
    public double Bpm => 60_000_000.0 / MicrosecondsPerQuarterNote;

    /// <summary>
    /// Gets the text content (for text-based meta events).
    /// </summary>
    public string? TextContent =>
        MetaType is >= 0x01 and <= 0x07
            ? System.Text.Encoding.UTF8.GetString(Data)
            : null;
}
