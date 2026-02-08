namespace SMFTrace.Core.Configuration;

/// <summary>
/// Options controlling playback behavior.
/// </summary>
public sealed class PlaybackOptions
{
    /// <summary>
    /// Whether to display tempo (BPM) overlay on the piano roll.
    /// </summary>
    public bool ShowTempo { get; set; } = true;

    /// <summary>
    /// Whether to display bars/beats grid lines on the piano roll.
    /// </summary>
    public bool ShowBarsBeatsGrid { get; set; } = true;

    /// <summary>
    /// Whether playback should loop when it reaches the end of the file.
    /// </summary>
    public bool LoopPlayback { get; set; }

    /// <summary>
    /// Playback speed multiplier (0.05x - 4x).
    /// </summary>
    public double TempoMultiplier { get; set; } = 1.0;
}
