namespace SMFTrace.Wpf.Controls;

/// <summary>
/// Configuration settings for the piano roll panel.
/// </summary>
public sealed class PianoRollSettings
{
    /// <summary>Default visible time window in seconds.</summary>
    public const double DefaultWindowSeconds = 30.0;

    /// <summary>Minimum visible time window in seconds.</summary>
    public const double MinWindowSeconds = 1.0;

    /// <summary>Maximum visible time window in seconds.</summary>
    public const double MaxWindowSeconds = 300.0;

    /// <summary>Playhead position as fraction of view width (0.33 = 33%).</summary>
    public const double PlayheadPosition = 0.33;

    /// <summary>Default pitch range low (MIDI note number).</summary>
    public const int DefaultPitchLow = 21;  // A0

    /// <summary>Default pitch range high (MIDI note number).</summary>
    public const int DefaultPitchHigh = 108;  // C8

    /// <summary>Height per pitch row in pixels.</summary>
    public const double PitchRowHeight = 8.0;

    /// <summary>Lane header width in pixels.</summary>
    public const double LaneHeaderWidth = 150.0;

    /// <summary>Minimum lane height in pixels.</summary>
    public const double MinLaneHeight = 50.0;

    /// <summary>Gap between lanes in pixels.</summary>
    public const double LaneGap = 2.0;

    /// <summary>Visible time window in seconds.</summary>
    public double WindowSeconds { get; set; } = DefaultWindowSeconds;

    /// <summary>Low pitch of visible range (MIDI note).</summary>
    public int PitchLow { get; set; } = DefaultPitchLow;

    /// <summary>High pitch of visible range (MIDI note).</summary>
    public int PitchHigh { get; set; } = DefaultPitchHigh;

    /// <summary>Whether to show tempo display.</summary>
    public bool ShowTempo { get; set; } = true;

    /// <summary>Whether to show bars/beats grid.</summary>
    public bool ShowBarsBeatsGrid { get; set; } = true;

    /// <summary>Number of pitches in the visible range.</summary>
    public int PitchCount => PitchHigh - PitchLow + 1;

    /// <summary>Calculates lane height based on pitch count.</summary>
    public double CalculateLaneHeight()
    {
        var height = PitchCount * PitchRowHeight;
        return height < MinLaneHeight ? MinLaneHeight : height;
    }
}
