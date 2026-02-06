namespace SMFTrace.Core.Models;

/// <summary>
/// Information about a track in the MIDI file.
/// </summary>
/// <param name="Index">Zero-based track index.</param>
/// <param name="Name">Track name from meta event, or null if not specified.</param>
/// <param name="EventCount">Number of events in this track.</param>
public readonly record struct TrackInfo(int Index, string? Name, int EventCount);
