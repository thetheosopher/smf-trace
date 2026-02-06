namespace SMFTrace.Core;

/// <summary>
/// Exception thrown when a MIDI file cannot be loaded or parsed.
/// </summary>
public class MidiFileException : Exception
{
    /// <summary>
    /// Path to the file that caused the exception.
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// The type of error that occurred.
    /// </summary>
    public MidiFileErrorType ErrorType { get; }

    public MidiFileException(string message)
        : base(message)
    {
        ErrorType = MidiFileErrorType.Unknown;
    }

    public MidiFileException(string message, string? filePath, MidiFileErrorType errorType)
        : base(message)
    {
        FilePath = filePath;
        ErrorType = errorType;
    }

    public MidiFileException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorType = MidiFileErrorType.Unknown;
    }

    public MidiFileException(string message, string? filePath, MidiFileErrorType errorType, Exception innerException)
        : base(message, innerException)
    {
        FilePath = filePath;
        ErrorType = errorType;
    }
}

/// <summary>
/// Types of MIDI file errors.
/// </summary>
public enum MidiFileErrorType
{
    /// <summary>Unknown error.</summary>
    Unknown,

    /// <summary>File was not found.</summary>
    FileNotFound,

    /// <summary>File is not a valid MIDI file.</summary>
    InvalidFormat,

    /// <summary>File format is not supported (e.g., Type 2).</summary>
    UnsupportedFormat,

    /// <summary>File is empty or has no tracks.</summary>
    EmptyFile,

    /// <summary>File is corrupted or contains invalid data.</summary>
    CorruptedData,

    /// <summary>File is too large to process.</summary>
    FileTooLarge
}
