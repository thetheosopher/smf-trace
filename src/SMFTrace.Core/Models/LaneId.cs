namespace SMFTrace.Core.Models;

/// <summary>
/// Identifies a lane in the piano roll (unique combination of track and channel).
/// </summary>
/// <param name="TrackIndex">Zero-based track index.</param>
/// <param name="Channel">MIDI channel (0-15).</param>
public readonly record struct LaneId(int TrackIndex, byte Channel);
