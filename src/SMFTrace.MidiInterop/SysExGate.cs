namespace SMFTrace.MidiInterop;

/// <summary>
/// Wraps an IMidiOutput and gates SysEx transmission based on a configurable option.
/// </summary>
public sealed class SysExGate : IMidiOutput
{
    private readonly IMidiOutput _inner;
    private bool _disposed;

    /// <summary>
    /// When true, SysEx messages are suppressed (not transmitted).
    /// </summary>
    public bool DisableSysExOutput { get; set; }

    /// <inheritdoc />
    public string DisplayName => _inner.DisplayName;

    /// <summary>
    /// Raised when a SysEx message is suppressed due to DisableSysExOutput being true.
    /// </summary>
    public event EventHandler<SysExSuppressedEventArgs>? SysExSuppressed;

    /// <summary>
    /// Creates a new SysEx gate wrapping the given output.
    /// </summary>
    /// <param name="inner">The underlying MIDI output device.</param>
    /// <param name="disableSysExOutput">Initial value for DisableSysExOutput.</param>
    public SysExGate(IMidiOutput inner, bool disableSysExOutput = false)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        DisableSysExOutput = disableSysExOutput;
    }

    /// <inheritdoc />
    public void SendShortMessage(byte status, byte data1, byte data2)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _inner.SendShortMessage(status, data1, data2);
    }

    /// <inheritdoc />
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public void SendSysEx(ReadOnlySpan<byte> payload)
#pragma warning restore CA1711
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (DisableSysExOutput)
        {
            // Notify that SysEx was suppressed
            SysExSuppressed?.Invoke(this, new SysExSuppressedEventArgs(payload.ToArray()));
            return;
        }

        _inner.SendSysEx(payload);
    }

    /// <inheritdoc />
    public void AllNotesOff()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _inner.AllNotesOff();
    }

    /// <inheritdoc />
    public void ResetControllers()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _inner.ResetControllers();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _inner.Dispose();
    }
}

/// <summary>
/// Event arguments for suppressed SysEx messages.
/// </summary>
public sealed class SysExSuppressedEventArgs : EventArgs
{
    /// <summary>
    /// The SysEx payload that was suppressed.
    /// </summary>
    public byte[] Payload { get; }

    public SysExSuppressedEventArgs(byte[] payload)
    {
        Payload = payload;
    }
}
