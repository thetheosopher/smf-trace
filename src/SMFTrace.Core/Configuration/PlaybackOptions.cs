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
    /// Tempo adjustment in BPM applied on top of the file tempo map.
    /// </summary>
    public double TempoAdjustmentBpm { get; set; }

    /// <summary>
    /// When true, SysEx events are suppressed from output transmission.
    /// </summary>
#pragma warning disable CA1711 // SysEx is an industry-standard term
    public bool DisableSysExOutput { get; set; }
#pragma warning restore CA1711
}
