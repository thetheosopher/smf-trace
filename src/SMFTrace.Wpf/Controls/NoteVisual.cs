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

    /// <summary>Y offset from top of scroll area.</summary>
    public double YOffset { get; set; }

    /// <summary>Height of this lane in pixels.</summary>
    public double Height { get; set; }

    /// <summary>Notes in this lane (paired for rendering).</summary>
    public List<PairedNote> Notes { get; } = [];
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
