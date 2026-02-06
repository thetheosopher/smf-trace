namespace SMFTrace.Core.Models;

/// <summary>
/// Per-channel state for bank select, program change, and controllers.
/// Immutable; create updated copies via with-expressions.
/// </summary>
public sealed record ChannelState
{
    /// <summary>Bank Select MSB (CC0).</summary>
    public byte BankMsb { get; init; }

    /// <summary>Bank Select LSB (CC32).</summary>
    public byte BankLsb { get; init; }

    /// <summary>Currently active program number (0-127).</summary>
    public byte Program { get; init; }

    /// <summary>Indicates whether a Program Change has been received.</summary>
    public bool HasProgramChange { get; init; }

    /// <summary>Controller values indexed by CC number.</summary>
    public IReadOnlyDictionary<byte, byte> Controllers { get; init; } = new Dictionary<byte, byte>();

    /// <summary>
    /// Gets the display name for the current instrument.
    /// Returns "(default)" if no program change has been received.
    /// </summary>
    public string InstrumentDisplayName => HasProgramChange ? $"Program {Program}" : "(default)";
}
