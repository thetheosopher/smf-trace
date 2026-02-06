namespace SMFTrace.Wpf.Settings;

/// <summary>
/// Application settings persisted to JSON.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Name of the last used MIDI output device.
    /// </summary>
    public string? LastDeviceName { get; set; }

    /// <summary>
    /// Main window width.
    /// </summary>
    public double WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Main window height.
    /// </summary>
    public double WindowHeight { get; set; } = 800;

    /// <summary>
    /// Main window left position.
    /// </summary>
    public double WindowLeft { get; set; } = 100;

    /// <summary>
    /// Main window top position.
    /// </summary>
    public double WindowTop { get; set; } = 100;

    /// <summary>
    /// Whether the window was maximized.
    /// </summary>
    public bool IsMaximized { get; set; }

    /// <summary>
    /// Render FPS for piano roll (30 or 60).
    /// </summary>
    public int RenderFps { get; set; } = 60;

    /// <summary>
    /// Whether tempo display is enabled.
    /// </summary>
    public bool ShowTempo { get; set; } = true;

    /// <summary>
    /// Whether bars/beats grid is enabled.
    /// </summary>
    public bool ShowBarsBeatsGrid { get; set; } = true;

    /// <summary>
    /// Whether overlay mode is enabled (all tracks on single lane with track colors).
    /// </summary>
    public bool OverlayMode { get; set; }

    /// <summary>
    /// Whether SysEx output is disabled.
    /// </summary>
    public bool DisableSysExOutput { get; set; }

    /// <summary>
    /// Piano roll zoom level (seconds visible in window).
    /// </summary>
    public double PianoRollWindowSeconds { get; set; } = 30.0;

    // Diagnostics filter states

    /// <summary>
    /// Show Note On/Off events in diagnostics.
    /// </summary>
    public bool DiagShowNotes { get; set; } = true;

    /// <summary>
    /// Show Control Change events in diagnostics.
    /// </summary>
    public bool DiagShowControlChanges { get; set; } = true;

    /// <summary>
    /// Show Program Change events in diagnostics.
    /// </summary>
    public bool DiagShowProgramChanges { get; set; } = true;

    /// <summary>
    /// Show Pitch Bend events in diagnostics.
    /// </summary>
    public bool DiagShowPitchBend { get; set; } = true;

    /// <summary>
    /// Show SysEx events in diagnostics.
    /// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public bool DiagShowSysEx { get; set; } = true;
#pragma warning restore CA1711

    /// <summary>
    /// Meta-only mode in diagnostics.
    /// </summary>
    public bool DiagMetaOnlyMode { get; set; }

    /// <summary>
    /// Auto-scroll enabled in diagnostics.
    /// </summary>
    public bool DiagAutoScrollEnabled { get; set; } = true;
}
