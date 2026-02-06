namespace SMFTrace.MidiInterop;

/// <summary>
/// Exception thrown when a MIDI operation fails.
/// </summary>
public sealed class MidiException : Exception
{
    /// <summary>
    /// The Windows Multimedia error code.
    /// </summary>
    public uint ErrorCode { get; }

    public MidiException(string message, uint errorCode)
        : base($"{message} (error code: {errorCode})")
    {
        ErrorCode = errorCode;
    }

    public MidiException(string message) : base(message)
    {
    }

    public MidiException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public MidiException()
    {
    }
}
