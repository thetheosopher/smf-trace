using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.Controls;

/// <summary>
/// Render data for a single note in the piano roll.
/// </summary>
public readonly struct NoteVisual
{
    /// <summary>Rectangle bounds in lane-local coordinates.</summary>
    public Rect Bounds { get; init; }

    /// <summary>Fill brush based on velocity.</summary>
    public Brush Fill { get; init; }

    /// <summary>MIDI note number for tooltip/label.</summary>
    public byte NoteNumber { get; init; }

    /// <summary>Velocity value.</summary>
    public byte Velocity { get; init; }

    /// <summary>Start tick.</summary>
    public long StartTick { get; init; }

    /// <summary>End tick.</summary>
    public long EndTick { get; init; }
}

/// <summary>
/// Paired note with start and end times for rendering.
/// </summary>
public sealed class PairedNote
{
    public required long StartTick { get; init; }
    public required long EndTick { get; init; }
    public required TimeSpan StartTime { get; init; }
    public required TimeSpan EndTime { get; init; }
    public required byte Channel { get; init; }
    public required byte NoteNumber { get; init; }
    public required byte Velocity { get; init; }
    public required int TrackIndex { get; init; }
    public required LaneId Lane { get; init; }
}

/// <summary>
/// Layout information for a single lane.
/// </summary>
public sealed class LaneLayout
{
    /// <summary>Lane identifier.</summary>
    public required LaneId Id { get; init; }

    /// <summary>Track name for display.</summary>
    public string? TrackName { get; init; }

    /// <summary>Current instrument display name.</summary>
    public string InstrumentName { get; set; } = "(default)";

    /// <summary>Cached formatted track name.</summary>
    public FormattedText? TrackText { get; set; }

    /// <summary>Cached formatted channel label.</summary>
    public FormattedText? ChannelText { get; set; }

    /// <summary>Cached formatted instrument name.</summary>
    public FormattedText? InstrumentText { get; set; }

    /// <summary>Cached header text DPI.</summary>
    public double CachedHeaderDpi { get; set; }

    /// <summary>Cached header text values.</summary>
    public string? CachedTrackName { get; set; }
    public string? CachedChannelLabel { get; set; }
    public string? CachedInstrumentName { get; set; }

    /// <summary>Y offset from top of scroll area.</summary>
    public double YOffset { get; set; }

    /// <summary>Height of this lane in pixels.</summary>
    public double Height { get; set; }

    /// <summary>Low pitch of visible range for this lane.</summary>
    public int PitchLow { get; set; }

    /// <summary>High pitch of visible range for this lane.</summary>
    public int PitchHigh { get; set; }

    /// <summary>Number of pitches in the visible range.</summary>
    public int PitchCount => PitchHigh - PitchLow + 1;

    /// <summary>Notes in this lane (paired for rendering).</summary>
    public List<PairedNote> Notes { get; } = [];

    /// <summary>Active note timeline for keyboard highlighting.</summary>
    public LaneEventTimeline ActiveTimeline { get; set; } = new LaneEventTimeline([]);

    /// <summary>Live active notes from the sequencer (if available).</summary>
    public bool[]? LiveActiveNotes { get; set; }

    /// <summary>Cached keyboard drawing for this lane.</summary>
    public DrawingGroup? KeyboardDrawing { get; set; }

    /// <summary>Keyboard cache parameters.</summary>
    public double KeyboardKeyLeft { get; set; }
    public double KeyboardRowHeight { get; set; }
    public double KeyboardWidth { get; set; }
    public double KeyboardHeight { get; set; }
    public int KeyboardPitchLow { get; set; }
    public int KeyboardPitchHigh { get; set; }

    public FormattedText[]? NoteNameTexts { get; set; }
    public double NoteNameFontSize { get; set; }
    public double NoteNameDpi { get; set; }
    public int NoteNamePitchLow { get; set; }
    public int NoteNamePitchHigh { get; set; }

    public void InvalidateHeaderCache()
    {
        TrackText = null;
        ChannelText = null;
        InstrumentText = null;
        CachedHeaderDpi = 0;
        CachedTrackName = null;
        CachedChannelLabel = null;
        CachedInstrumentName = null;
    }

    public void InvalidateNoteNameCache()
    {
        NoteNameTexts = null;
        NoteNameFontSize = 0;
        NoteNameDpi = 0;
        NoteNamePitchLow = 0;
        NoteNamePitchHigh = 0;
    }
}

/// <summary>
/// Incremental active-note tracker for efficient keyboard highlighting.
/// </summary>
public sealed class LaneEventTimeline
{
    private readonly List<(TimeSpan Time, byte Note)> _noteOns;
    private readonly List<(TimeSpan Time, byte Note)> _noteOffs;
    private readonly int[] _activeCounts = new int[128];
    private readonly bool[] _activeNotes = new bool[128];
    private int _onIndex;
    private int _offIndex;
    private TimeSpan _lastTime;
    private int _activeTotal;

    public LaneEventTimeline(IEnumerable<PairedNote> notes)
    {
        _noteOns = new List<(TimeSpan, byte)>();
        _noteOffs = new List<(TimeSpan, byte)>();

        foreach (var note in notes)
        {
            _noteOns.Add((note.StartTime, note.NoteNumber));
            _noteOffs.Add((note.EndTime, note.NoteNumber));
        }

        _noteOns.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        _noteOffs.Sort(static (a, b) => a.Time.CompareTo(b.Time));
    }

    public bool[] ActiveNotes => _activeNotes;

    public bool HasAnyActive => _activeTotal > 0;

    public void Reset()
    {
        Array.Clear(_activeCounts, 0, _activeCounts.Length);
        Array.Clear(_activeNotes, 0, _activeNotes.Length);
        _onIndex = 0;
        _offIndex = 0;
        _activeTotal = 0;
        _lastTime = TimeSpan.Zero;
    }

    public void AdvanceTo(TimeSpan time)
    {
        if (time < _lastTime)
        {
            Reset();
        }

        while (_onIndex < _noteOns.Count && _noteOns[_onIndex].Time <= time)
        {
            var note = _noteOns[_onIndex].Note;
            if (_activeCounts[note] == 0)
            {
                _activeNotes[note] = true;
                _activeTotal++;
            }
            _activeCounts[note]++;
            _onIndex++;
        }

        while (_offIndex < _noteOffs.Count && _noteOffs[_offIndex].Time < time)
        {
            var note = _noteOffs[_offIndex].Note;
            if (_activeCounts[note] > 0)
            {
                _activeCounts[note]--;
                if (_activeCounts[note] == 0)
                {
                    _activeNotes[note] = false;
                    _activeTotal--;
                }
            }
            _offIndex++;
        }

        _lastTime = time;
    }
}

/// <summary>
/// Generates velocity-based color gradients for notes.
/// </summary>
public static class VelocityColorMapper
{
    private static readonly SolidColorBrush[] VelocityBrushes;

    static VelocityColorMapper()
    {
        // Pre-create brushes for each velocity value (0-127)
        VelocityBrushes = new SolidColorBrush[128];

        for (var i = 0; i < 128; i++)
        {
            var ratio = i / 127.0;
            // Gradient from light blue (low velocity) to deep blue (high velocity)
            var r = (byte)(180 - ratio * 100);
            var g = (byte)(200 - ratio * 80);
            var b = (byte)(255);
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            VelocityBrushes[i] = brush;
        }
    }

    /// <summary>
    /// Gets a frozen brush for the given velocity.
    /// </summary>
    public static SolidColorBrush GetBrush(byte velocity)
    {
        return VelocityBrushes[velocity < 128 ? velocity : 127];
    }

    /// <summary>
    /// Gets opacity based on velocity (0.4 - 1.0 range).
    /// </summary>
    public static double GetOpacity(byte velocity)
    {
        return 0.4 + 0.6 * (velocity / 127.0);
    }
}

/// <summary>
/// Generates distinct colors for different tracks in overlay mode.
/// </summary>
public static class TrackColorMapper
{
    // 16 distinct hue values for tracks
    private static readonly SolidColorBrush[] TrackBrushes;

    static TrackColorMapper()
    {
        // Create 16 distinct colors using HSL color wheel
        TrackBrushes = new SolidColorBrush[16];

        var hues = new[]
        {
            0,    // Red
            210,  // Blue
            120,  // Green
            45,   // Orange
            280,  // Purple
            180,  // Cyan
            330,  // Pink
            60,   // Yellow
            240,  // Deep Blue
            150,  // Teal
            30,   // Red-Orange
            195,  // Sky Blue
            90,   // Yellow-Green
            300,  // Magenta
            165,  // Sea Green
            350   // Rose
        };

        for (var i = 0; i < 16; i++)
        {
            var color = HslToRgb(hues[i], 0.75, 0.55);
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            TrackBrushes[i] = brush;
        }
    }

    /// <summary>
    /// Gets a frozen brush for the given track index.
    /// </summary>
    public static SolidColorBrush GetBrush(int trackIndex)
    {
        return TrackBrushes[trackIndex % 16];
    }

    /// <summary>
    /// Gets a brush with velocity-modulated brightness for overlay mode.
    /// </summary>
    public static SolidColorBrush GetBrush(int trackIndex, byte velocity)
    {
        var baseBrush = TrackBrushes[trackIndex % 16];
        var baseColor = baseBrush.Color;

        // Modulate brightness based on velocity (0.5 to 1.0)
        var factor = 0.5 + 0.5 * (velocity / 127.0);
        var r = (byte)(baseColor.R * factor);
        var g = (byte)(baseColor.G * factor);
        var b = (byte)(baseColor.B * factor);

        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }

    private static Color HslToRgb(double hue, double saturation, double lightness)
    {
        var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var x = c * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = lightness - c / 2;

        double r, g, b;
        if (hue < 60) { r = c; g = x; b = 0; }
        else if (hue < 120) { r = x; g = c; b = 0; }
        else if (hue < 180) { r = 0; g = c; b = x; }
        else if (hue < 240) { r = 0; g = x; b = c; }
        else if (hue < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb(
            (byte)((r + m) * 255),
            (byte)((g + m) * 255),
            (byte)((b + m) * 255));
    }
}
