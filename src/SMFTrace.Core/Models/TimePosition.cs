namespace SMFTrace.Core.Models;

/// <summary>
/// Represents a position in time within a MIDI file.
/// </summary>
/// <param name="Ticks">The absolute tick position.</param>
/// <param name="Time">The corresponding time offset from the start.</param>
public readonly record struct TimePosition(long Ticks, TimeSpan Time);
