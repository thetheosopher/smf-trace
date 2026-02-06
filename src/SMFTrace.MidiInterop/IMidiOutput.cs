namespace SMFTrace.MidiInterop;

/// <summary>
/// Abstraction for MIDI output device operations.
/// </summary>
public interface IMidiOutput : IDisposable
{
    /// <summary>Display name of the device.</summary>
    string DisplayName { get; }

    /// <summary>Sends a short (non-SysEx) MIDI message.</summary>
    void SendShortMessage(byte status, byte data1, byte data2);

    /// <summary>Sends a System Exclusive message.</summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    void SendSysEx(ReadOnlySpan<byte> payload);
#pragma warning restore CA1711

    /// <summary>Sends All Notes Off (CC 123) on all channels.</summary>
    void AllNotesOff();

    /// <summary>Sends Reset All Controllers (CC 121) on all channels.</summary>
    void ResetControllers();
}
