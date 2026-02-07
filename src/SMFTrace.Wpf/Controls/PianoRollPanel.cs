using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using SMFTrace.Core.Models;

namespace SMFTrace.Wpf.Controls;

/// <summary>
/// High-performance piano roll control using DrawingVisual rendering.
/// Displays a left-to-right scrolling view with a fixed playhead at 33%.
/// </summary>
public class PianoRollPanel : FrameworkElement
{
    #region Dependency Properties

    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(
            nameof(CurrentTime),
            typeof(TimeSpan),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender, OnCurrentTimeChanged));

    public static readonly DependencyProperty IsPlayingProperty =
        DependencyProperty.Register(
            nameof(IsPlaying),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(false, OnIsPlayingChanged));

    public static readonly DependencyProperty TotalDurationProperty =
        DependencyProperty.Register(
            nameof(TotalDuration),
            typeof(TimeSpan),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(TimeSpan.FromSeconds(60), FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty WindowSecondsProperty =
        DependencyProperty.Register(
            nameof(WindowSeconds),
            typeof(double),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(
                PianoRollSettings.DefaultWindowSeconds,
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnWindowSecondsChanged,
                CoerceWindowSeconds));

    public static readonly DependencyProperty ShowTempoProperty =
        DependencyProperty.Register(
            nameof(ShowTempo),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ShowBarsBeatsGridProperty =
        DependencyProperty.Register(
            nameof(ShowBarsBeatsGrid),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty OverlayModeProperty =
        DependencyProperty.Register(
            nameof(OverlayMode),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnOverlayModeChanged));

    public TimeSpan CurrentTime
    {
        get => (TimeSpan)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }

    public bool IsPlaying
    {
        get => (bool)GetValue(IsPlayingProperty);
        set => SetValue(IsPlayingProperty, value);
    }

    public TimeSpan TotalDuration
    {
        get => (TimeSpan)GetValue(TotalDurationProperty);
        set => SetValue(TotalDurationProperty, value);
    }

    public double WindowSeconds
    {
        get => (double)GetValue(WindowSecondsProperty);
        set => SetValue(WindowSecondsProperty, value);
    }

    public bool ShowTempo
    {
        get => (bool)GetValue(ShowTempoProperty);
        set => SetValue(ShowTempoProperty, value);
    }

    public bool ShowBarsBeatsGrid
    {
        get => (bool)GetValue(ShowBarsBeatsGridProperty);
        set => SetValue(ShowBarsBeatsGridProperty, value);
    }

    public bool OverlayMode
    {
        get => (bool)GetValue(OverlayModeProperty);
        set => SetValue(OverlayModeProperty, value);
    }

    private static void OnWindowSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Trigger re-render
    }

    private static void OnCurrentTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            panel.SyncPosition((TimeSpan)e.NewValue);
        }
    }

    private static void OnIsPlayingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            if ((bool)e.NewValue)
            {
                panel.StartSmoothScrolling();
            }
            else
            {
                panel.StopSmoothScrolling();
            }
        }
    }

    private static void OnOverlayModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            panel.RebuildLanes();
        }
    }

#pragma warning disable CA1859 // CoerceValueCallback must return object
    private static object CoerceWindowSeconds(DependencyObject d, object baseValue)
    {
        var value = (double)baseValue;
        if (value < PianoRollSettings.MinWindowSeconds) return PianoRollSettings.MinWindowSeconds;
        if (value > PianoRollSettings.MaxWindowSeconds) return PianoRollSettings.MaxWindowSeconds;
        return value;
    }
#pragma warning restore CA1859

    #endregion

    #region Fields

    private readonly VisualCollection _visuals;
    private readonly DrawingVisual _backgroundVisual;
    private readonly DrawingVisual _gridVisual;
    private readonly DrawingVisual _notesVisual;
    private readonly DrawingVisual _playheadVisual;
    private readonly DrawingVisual _laneHeadersVisual;
    private readonly DrawingVisual _tempoVisual;

    private readonly PianoRollSettings _settings = new();
    private List<LaneLayout> _lanes = [];
    private List<PairedNote> _allNotes = [];
    private IReadOnlyList<TrackInfo> _tracks = [];
    private double _currentTempo = 120.0; // BPM
    private ChannelState[] _channelStates = new ChannelState[16]; // Cached for lane rebuilds

    // Smooth scrolling fields
    private readonly Stopwatch _smoothScrollStopwatch = new();
    private TimeSpan _syncPosition; // Last known position from sequencer
    private bool _isSmoothScrolling;
    private TimeSpan _interpolatedTime; // Current interpolated position for rendering

    // Cached resources
    private static readonly Pen PlayheadPen;
    private static readonly Pen GridPen;
    private static readonly Pen OctaveGridPen;
    private static readonly Brush BackgroundBrush;
    private static readonly Brush LaneBackgroundBrush;
    private static readonly Brush LaneAlternateBrush;
    private static readonly Brush LaneHeaderBrush;
    private static readonly Typeface LabelTypeface;

    #endregion

    #region Static Constructor

    static PianoRollPanel()
    {
        PlayheadPen = new Pen(Brushes.Red, 2);
        PlayheadPen.Freeze();

        GridPen = new Pen(new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)), 1);
        GridPen.Freeze();

        OctaveGridPen = new Pen(new SolidColorBrush(Color.FromArgb(80, 128, 128, 128)), 1);
        OctaveGridPen.Freeze();

        BackgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 35));
        BackgroundBrush.Freeze();

        LaneBackgroundBrush = new SolidColorBrush(Color.FromRgb(40, 40, 45));
        LaneBackgroundBrush.Freeze();

        LaneAlternateBrush = new SolidColorBrush(Color.FromRgb(35, 35, 40));
        LaneAlternateBrush.Freeze();

        LaneHeaderBrush = new SolidColorBrush(Color.FromRgb(50, 50, 55));
        LaneHeaderBrush.Freeze();

        LabelTypeface = new Typeface("Segoe UI");
    }

    #endregion

    #region Constructor

    public PianoRollPanel()
    {
        _visuals = new VisualCollection(this);

        _backgroundVisual = new DrawingVisual();
        _gridVisual = new DrawingVisual();
        _notesVisual = new DrawingVisual();
        _playheadVisual = new DrawingVisual();
        _laneHeadersVisual = new DrawingVisual();
        _tempoVisual = new DrawingVisual();

        _visuals.Add(_backgroundVisual);
        _visuals.Add(_gridVisual);
        _visuals.Add(_notesVisual);
        _visuals.Add(_playheadVisual);
        _visuals.Add(_laneHeadersVisual);
        _visuals.Add(_tempoVisual);

        // Initialize channel states with defaults
        for (var i = 0; i < 16; i++)
        {
            _channelStates[i] = new ChannelState();
        }

        ClipToBounds = true;

        // Ensure cleanup when unloaded
        Unloaded += (_, _) => StopSmoothScrolling();
    }

    #endregion

    #region Smooth Scrolling

    /// <summary>
    /// Gets the effective time to use for rendering (interpolated when playing, actual when stopped).
    /// </summary>
    private TimeSpan RenderTime => _isSmoothScrolling ? _interpolatedTime : CurrentTime;

    private void SyncPosition(TimeSpan newPosition)
    {
        _syncPosition = newPosition;
        _smoothScrollStopwatch.Restart();

        if (!_isSmoothScrolling)
        {
            _interpolatedTime = newPosition;
        }
    }

    private void StartSmoothScrolling()
    {
        if (_isSmoothScrolling) return;

        _isSmoothScrolling = true;
        _syncPosition = CurrentTime;
        _interpolatedTime = CurrentTime;
        _smoothScrollStopwatch.Restart();
        CompositionTarget.Rendering += OnCompositionTargetRendering;
    }

    private void StopSmoothScrolling()
    {
        if (!_isSmoothScrolling) return;

        _isSmoothScrolling = false;
        CompositionTarget.Rendering -= OnCompositionTargetRendering;
        _smoothScrollStopwatch.Stop();
        InvalidateVisual();
    }

    private void OnCompositionTargetRendering(object? sender, EventArgs e)
    {
        // Interpolate position based on elapsed time since last sync
        var elapsed = _smoothScrollStopwatch.Elapsed;
        _interpolatedTime = _syncPosition + elapsed;

        // Clamp to duration
        if (_interpolatedTime > TotalDuration)
        {
            _interpolatedTime = TotalDuration;
        }

        // Trigger a render
        InvalidateVisual();
    }

    #endregion

    #region Visual Tree

    protected override int VisualChildrenCount => _visuals.Count;

    protected override Visual GetVisualChild(int index) => _visuals[index];

    #endregion

    #region Layout

    protected override Size MeasureOverride(Size availableSize)
    {
        // In overlay mode, use available height
        if (OverlayMode)
        {
            return new Size(
                double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? 600 : availableSize.Height);
        }

        // Calculate total height needed for all lanes
        var totalHeight = CalculateTotalLanesHeight();

        // Use at least the available height, or more if content requires
        var height = double.IsInfinity(availableSize.Height)
            ? Math.Max(totalHeight, 600)
            : Math.Max(totalHeight, availableSize.Height);

        return new Size(
            double.IsInfinity(availableSize.Width) ? 800 : availableSize.Width,
            height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Update overlay lane height when size changes
        if (OverlayMode && _lanes.Count > 0)
        {
            _lanes[0].Height = finalSize.Height;
        }

        return finalSize;
    }

    private double CalculateTotalLanesHeight()
    {
        if (_lanes.Count == 0)
        {
            return 0;
        }

        var laneHeight = _settings.CalculateLaneHeight();
        return _lanes.Count * laneHeight + (_lanes.Count - 1) * PianoRollSettings.LaneGap;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads note data for rendering.
    /// </summary>
    public void LoadNotes(IReadOnlyList<MidiEventBase> events, IReadOnlyList<TrackInfo> tracks)
    {
        _allNotes = NotePairer.PairNotes(events);
        _tracks = tracks;
        RebuildLanes();
    }

    /// <summary>
    /// Rebuilds lane layouts based on current mode (overlay vs lane).
    /// </summary>
    private void RebuildLanes()
    {
        _lanes.Clear();

        if (OverlayMode)
        {
            // Single lane with all notes overlaid
            var layout = new LaneLayout
            {
                Id = new LaneId(0, 0),
                TrackName = "All Tracks (Overlay)",
                YOffset = 0,
                Height = ActualHeight > 0 ? ActualHeight : 600
            };

            layout.Notes.AddRange(_allNotes);
            _lanes.Add(layout);
        }
        else
        {
            // Separate lanes by (track, channel)
            var notesByLane = NotePairer.GroupByLane(_allNotes);
            var sortedLanes = notesByLane.Keys.OrderBy(l => l.TrackIndex).ThenBy(l => l.Channel).ToList();

            var yOffset = 0.0;
            var laneHeight = _settings.CalculateLaneHeight();

            foreach (var laneId in sortedLanes)
            {
                var trackName = laneId.TrackIndex < _tracks.Count ? _tracks[laneId.TrackIndex].Name : null;

                var layout = new LaneLayout
                {
                    Id = laneId,
                    TrackName = trackName,
                    YOffset = yOffset,
                    Height = laneHeight
                };

                layout.Notes.AddRange(notesByLane[laneId]);
                _lanes.Add(layout);

                yOffset += laneHeight + PianoRollSettings.LaneGap;
            }
        }

        // Reapply instrument names from cached channel states
        ApplyInstrumentNames();

        InvalidateMeasure();
        InvalidateVisual();
    }

    /// <summary>
    /// Updates instrument names based on current channel states.
    /// </summary>
    public void UpdateInstrumentNames(ChannelState[] channelStates)
    {
        _channelStates = channelStates;
        ApplyInstrumentNames();
    }

    private void ApplyInstrumentNames()
    {
        foreach (var lane in _lanes)
        {
            if (lane.Id.Channel < _channelStates.Length)
            {
                lane.InstrumentName = _channelStates[lane.Id.Channel].InstrumentDisplayName;
            }
        }

        InvalidateLaneHeaders();
    }

    /// <summary>
    /// Updates the current tempo display.
    /// </summary>
    /// <param name="bpm">Tempo in beats per minute.</param>
    public void UpdateTempo(double bpm)
    {
        _currentTempo = bpm;
        InvalidateVisual();
    }

    /// <summary>
    /// Zooms in (decreases visible time window).
    /// </summary>
    public void ZoomIn()
    {
        WindowSeconds = Math.Max(WindowSeconds * 0.8, PianoRollSettings.MinWindowSeconds);
    }

    /// <summary>
    /// Zooms out (increases visible time window).
    /// </summary>
    public void ZoomOut()
    {
        WindowSeconds = Math.Min(WindowSeconds * 1.25, PianoRollSettings.MaxWindowSeconds);
    }

    #endregion

    #region Rendering

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        RenderBackground();
        RenderGrid();
        RenderNotes();
        RenderPlayhead();
        RenderLaneHeaders();
        RenderTempoDisplay();
    }

    private void RenderBackground()
    {
        using var dc = _backgroundVisual.RenderOpen();
        dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

        // Draw lane backgrounds
        for (var i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            var brush = i % 2 == 0 ? LaneBackgroundBrush : LaneAlternateBrush;
            dc.DrawRectangle(
                brush,
                null,
                new Rect(PianoRollSettings.LaneHeaderWidth, lane.YOffset, ActualWidth - PianoRollSettings.LaneHeaderWidth, lane.Height));
        }
    }

    private void RenderGrid()
    {
        using var dc = _gridVisual.RenderOpen();

        if (!ShowBarsBeatsGrid)
        {
            // Clear the visual by just opening and closing the context
            return;
        }

        var viewWidth = ActualWidth - PianoRollSettings.LaneHeaderWidth;
        var playheadX = PianoRollSettings.LaneHeaderWidth + viewWidth * PianoRollSettings.PlayheadPosition;
        var pixelsPerSecond = viewWidth / WindowSeconds;

        // Time at left edge of view
        var leftTime = RenderTime.TotalSeconds - WindowSeconds * PianoRollSettings.PlayheadPosition;
        var rightTime = leftTime + WindowSeconds;

        // Draw vertical time grid lines (every second for now, adjust based on zoom)
        var gridInterval = CalculateGridInterval(pixelsPerSecond);
        var startSecond = Math.Floor(leftTime / gridInterval) * gridInterval;

        for (var t = startSecond; t <= rightTime; t += gridInterval)
        {
            if (t < 0) continue;

            var x = PianoRollSettings.LaneHeaderWidth + (t - leftTime) * pixelsPerSecond;
            if (x < PianoRollSettings.LaneHeaderWidth || x > ActualWidth) continue;

            dc.DrawLine(GridPen, new Point(x, 0), new Point(x, ActualHeight));
        }

        // Draw horizontal pitch grid lines for each lane
        foreach (var lane in _lanes)
        {
            RenderPitchGrid(dc, lane);
        }
    }

    private void RenderPitchGrid(DrawingContext dc, LaneLayout lane)
    {
        var pitchCount = _settings.PitchCount;
        var rowHeight = lane.Height / pitchCount;

        for (var pitch = _settings.PitchLow; pitch <= _settings.PitchHigh; pitch++)
        {
            var relPitch = pitch - _settings.PitchLow;
            var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;

            // Emphasize octave lines (C notes)
            var isOctave = pitch % 12 == 0;
            var pen = isOctave ? OctaveGridPen : GridPen;

            dc.DrawLine(pen,
                new Point(PianoRollSettings.LaneHeaderWidth, y),
                new Point(ActualWidth, y));
        }
    }

    private void RenderNotes()
    {
        using var dc = _notesVisual.RenderOpen();

        var viewWidth = ActualWidth - PianoRollSettings.LaneHeaderWidth;
        var pixelsPerSecond = viewWidth / WindowSeconds;

        // Time at left edge of view
        var leftTime = RenderTime.TotalSeconds - WindowSeconds * PianoRollSettings.PlayheadPosition;
        var rightTime = leftTime + WindowSeconds;

        foreach (var lane in _lanes)
        {
            RenderLaneNotes(dc, lane, leftTime, rightTime, pixelsPerSecond);
        }
    }

    private void RenderLaneNotes(
        DrawingContext dc,
        LaneLayout lane,
        double leftTime,
        double rightTime,
        double pixelsPerSecond)
    {
        var pitchCount = _settings.PitchCount;
        var rowHeight = lane.Height / pitchCount;
        var useTrackColors = OverlayMode;

        foreach (var note in lane.Notes)
        {
            var noteStartSec = note.StartTime.TotalSeconds;
            var noteEndSec = note.EndTime.TotalSeconds;

            // Skip notes outside visible range
            if (noteEndSec < leftTime || noteStartSec > rightTime) continue;

            // Skip notes outside pitch range
            if (note.NoteNumber < _settings.PitchLow || note.NoteNumber > _settings.PitchHigh) continue;

            // Calculate X coordinates
            var x1 = PianoRollSettings.LaneHeaderWidth + (noteStartSec - leftTime) * pixelsPerSecond;
            var x2 = PianoRollSettings.LaneHeaderWidth + (noteEndSec - leftTime) * pixelsPerSecond;

            // Clamp to visible area
            x1 = Math.Max(x1, PianoRollSettings.LaneHeaderWidth);
            x2 = Math.Min(x2, ActualWidth);

            if (x2 <= x1) continue;

            // Calculate Y coordinate (inverted: low pitches at bottom)
            var relPitch = note.NoteNumber - _settings.PitchLow;
            var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;

            var rect = new Rect(x1, y + 1, x2 - x1, rowHeight - 2);

            // Use track colors in overlay mode, velocity colors otherwise
            var brush = useTrackColors
                ? TrackColorMapper.GetBrush(note.TrackIndex, note.Velocity)
                : VelocityColorMapper.GetBrush(note.Velocity);

            dc.DrawRectangle(brush, null, rect);
        }
    }

    private void RenderPlayhead()
    {
        using var dc = _playheadVisual.RenderOpen();

        var viewWidth = ActualWidth - PianoRollSettings.LaneHeaderWidth;
        var playheadX = PianoRollSettings.LaneHeaderWidth + viewWidth * PianoRollSettings.PlayheadPosition;

        dc.DrawLine(PlayheadPen, new Point(playheadX, 0), new Point(playheadX, ActualHeight));
    }

    private void RenderLaneHeaders()
    {
        using var dc = _laneHeadersVisual.RenderOpen();

        // Background
        dc.DrawRectangle(LaneHeaderBrush, null, new Rect(0, 0, PianoRollSettings.LaneHeaderWidth, ActualHeight));

        if (OverlayMode)
        {
            RenderOverlayTrackList(dc);
            return;
        }

        foreach (var lane in _lanes)
        {
            var y = lane.YOffset + 4;
            var x = 4.0;

            // Track name
            if (!string.IsNullOrEmpty(lane.TrackName))
            {
                var trackText = new FormattedText(
                    lane.TrackName,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    11,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(trackText, new Point(x, y));
                y += 14;
            }

            // Track/Channel info
            var channelText = new FormattedText(
                $"T{lane.Id.TrackIndex} Ch{lane.Id.Channel + 1}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                Brushes.LightGray,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(channelText, new Point(x, y));
            y += 12;

            // Instrument name
            var instrumentText = new FormattedText(
                lane.InstrumentName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                Brushes.CornflowerBlue,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(instrumentText, new Point(x, y));
        }
    }

    private void RenderOverlayTrackList(DrawingContext dc)
    {
        // Get unique tracks from all notes
        var trackInfos = _allNotes
            .Select(n => n.TrackIndex)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var y = 8.0;
        var x = 4.0;
        const double lineHeight = 16.0;
        const double colorBoxSize = 10.0;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Title
        var titleText = new FormattedText(
            "Tracks",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            11,
            Brushes.White,
            dpi);

        dc.DrawText(titleText, new Point(x, y));
        y += lineHeight + 4;

        // Draw each track with its color
        foreach (var trackIndex in trackInfos)
        {
            if (y + lineHeight > ActualHeight - 4)
            {
                // Show "..." if we run out of space
                var moreText = new FormattedText(
                    $"... +{trackInfos.Count - trackInfos.IndexOf(trackIndex)} more",
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    LabelTypeface,
                    9,
                    Brushes.Gray,
                    dpi);
                dc.DrawText(moreText, new Point(x, y));
                break;
            }

            // Color indicator box
            var trackBrush = TrackColorMapper.GetBrush(trackIndex);
            var colorRect = new Rect(x, y + 2, colorBoxSize, colorBoxSize);
            dc.DrawRectangle(trackBrush, null, colorRect);

            // Track name or index
            var trackName = trackIndex < _tracks.Count && !string.IsNullOrEmpty(_tracks[trackIndex].Name)
                ? _tracks[trackIndex].Name
                : $"Track {trackIndex}";

            var trackText = new FormattedText(
                trackName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                Brushes.White,
                dpi);

            // Clip text if too long
            trackText.MaxTextWidth = PianoRollSettings.LaneHeaderWidth - x - colorBoxSize - 8;
            trackText.Trimming = TextTrimming.CharacterEllipsis;

            dc.DrawText(trackText, new Point(x + colorBoxSize + 4, y));
            y += lineHeight;
        }
    }

    private void InvalidateLaneHeaders()
    {
        RenderLaneHeaders();
    }

    private void RenderTempoDisplay()
    {
        using var dc = _tempoVisual.RenderOpen();

        if (!ShowTempo)
        {
            // Clear the visual
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Draw tempo badge in top-right corner
        var tempoText = $"{_currentTempo:F1} BPM";
        var formattedText = new FormattedText(
            tempoText,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            14,
            Brushes.White,
            dpi);

        var padding = 8.0;
        var badgeWidth = formattedText.Width + padding * 2;
        var badgeHeight = formattedText.Height + padding;
        var x = ActualWidth - badgeWidth - 10;
        var y = 10.0;

        // Background with slight transparency
        var badgeBrush = new SolidColorBrush(Color.FromArgb(200, 60, 60, 65));
        badgeBrush.Freeze();
        dc.DrawRoundedRectangle(
            badgeBrush,
            null,
            new Rect(x, y, badgeWidth, badgeHeight),
            4, 4);

        // Text
        dc.DrawText(formattedText, new Point(x + padding, y + padding / 2));
    }

    private static double CalculateGridInterval(double pixelsPerSecond)
    {
        // Aim for ~50-100 pixels between grid lines
        var targetPixels = 75.0;
        var seconds = targetPixels / pixelsPerSecond;

        // Snap to nice values
        if (seconds <= 0.5) return 0.5;
        if (seconds <= 1) return 1;
        if (seconds <= 2) return 2;
        if (seconds <= 5) return 5;
        if (seconds <= 10) return 10;
        if (seconds <= 30) return 30;
        return 60;
    }

    #endregion

    #region Mouse Handling

    protected override void OnMouseWheel(System.Windows.Input.MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Delta > 0)
            ZoomIn();
        else
            ZoomOut();

        e.Handled = true;
    }

    #endregion
}
