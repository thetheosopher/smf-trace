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
}
