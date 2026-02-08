using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
            new FrameworkPropertyMetadata(TimeSpan.Zero, OnCurrentTimeChanged));

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

    public static readonly DependencyProperty ShowNoteNamesProperty =
        DependencyProperty.Register(
            nameof(ShowNoteNames),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnShowNoteNamesChanged));

    public static readonly DependencyProperty ShowPianoKeysProperty =
        DependencyProperty.Register(
            nameof(ShowPianoKeys),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnShowPianoKeysChanged));

    public static readonly DependencyProperty CompactPitchRangeProperty =
        DependencyProperty.Register(
            nameof(CompactPitchRange),
            typeof(bool),
            typeof(PianoRollPanel),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender, OnCompactPitchRangeChanged));

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

    public bool ShowNoteNames
    {
        get => (bool)GetValue(ShowNoteNamesProperty);
        set => SetValue(ShowNoteNamesProperty, value);
    }

    public bool ShowPianoKeys
    {
        get => (bool)GetValue(ShowPianoKeysProperty);
        set => SetValue(ShowPianoKeysProperty, value);
    }

    public bool CompactPitchRange
    {
        get => (bool)GetValue(CompactPitchRangeProperty);
        set => SetValue(CompactPitchRangeProperty, value);
    }

    private static void OnWindowSecondsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // Trigger re-render
    }

    private static void OnCurrentTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            // During smooth scrolling, ignore external position updates -
            // the interpolation handles rendering smoothly based on elapsed time.
            // Only process updates when stopped/paused.
            if (!panel._isSmoothScrolling)
            {
                panel._interpolatedTime = (TimeSpan)e.NewValue;
                panel.InvalidateVisual();
                return;
            }

            var newTime = (TimeSpan)e.NewValue;
            if (newTime + TimeSpan.FromMilliseconds(10) < panel._interpolatedTime)
            {
                // Loop or seek while playing: resync interpolation to the new time.
                panel._syncPosition = newTime;
                panel._interpolatedTime = newTime;
                panel._smoothScrollStopwatch.Restart();
                panel._lastRenderTime = TimeSpan.Zero;
                panel.InvalidateVisual();
            }
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

    private static void OnShowNoteNamesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            panel.InvalidateLaneHeaders();
        }
    }

    private static void OnShowPianoKeysChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoRollPanel panel)
        {
            panel.InvalidateLaneHeaders();
        }
    }

    private static void OnCompactPitchRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

    private ScrollViewer? _scrollViewer;

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
    private TimeSpan _lastRenderTime; // For frame rate throttling in non-overlay mode
    private TimeSpan _lastActiveTimelineUpdate;

    private List<int> _overlayTrackIndices = [];
    private List<string> _overlayTrackNames = [];
    private List<FormattedText>? _overlayTrackTexts;
    private FormattedText? _overlayTrackTitleText;
    private double _overlayTrackDpi;
    private double _overlayTrackMaxWidth;

    // Cached resources
    private static readonly Pen PlayheadPen;
    private static readonly Pen GridPen;
    private static readonly Pen OctaveGridPen;
    private static readonly Brush BackgroundBrush;
    private static readonly Brush LaneBackgroundBrush;
    private static readonly Brush LaneAlternateBrush;
    private static readonly Brush LaneHeaderBrush;
    private static readonly Brush PianoWhiteBrush;
    private static readonly Brush PianoBlackBrush;
    private static readonly Brush PianoActiveFillBrush;
    private static readonly Brush PianoActiveGlowBrush;
    private static readonly Pen PianoActiveOuterPen;
    private static readonly Pen PianoActiveInnerPen;
    private static readonly Pen PianoKeyOutlinePen;
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

        PianoWhiteBrush = new SolidColorBrush(Color.FromRgb(235, 235, 235));
        PianoWhiteBrush.Freeze();

        PianoBlackBrush = new SolidColorBrush(Color.FromRgb(35, 35, 35));
        PianoBlackBrush.Freeze();

        PianoActiveFillBrush = new SolidColorBrush(Color.FromArgb(255, 213, 94, 0));
        PianoActiveFillBrush.Freeze();

        PianoActiveGlowBrush = new SolidColorBrush(Color.FromArgb(255, 213, 94, 0));
        PianoActiveGlowBrush.Freeze();

        PianoActiveOuterPen = new Pen(new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)), 2);
        PianoActiveOuterPen.Freeze();

        PianoActiveInnerPen = new Pen(new SolidColorBrush(Color.FromArgb(166, 0, 0, 0)), 2);
        PianoActiveInnerPen.Freeze();

        PianoKeyOutlinePen = new Pen(new SolidColorBrush(Color.FromArgb(60, 200, 200, 200)), 1);
        PianoKeyOutlinePen.Freeze();

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

    private void EnsureScrollViewer()
    {
        if (_scrollViewer != null)
        {
            return;
        }

        _scrollViewer = FindAncestor<ScrollViewer>(this);
    }

    private void GetVisibleVerticalRange(out double top, out double bottom)
    {
        EnsureScrollViewer();
        if (_scrollViewer == null || _scrollViewer.ViewportHeight <= 0)
        {
            top = 0;
            bottom = ActualHeight;
            return;
        }

        top = _scrollViewer.VerticalOffset;
        bottom = top + _scrollViewer.ViewportHeight;
    }

    private bool TryGetContentClipRect(out Rect rect)
    {
        GetVisibleVerticalRange(out var top, out var bottom);

        var width = ActualWidth - PianoRollSettings.LaneHeaderWidth;
        var height = bottom - top;

        if (width <= 0 || height <= 0)
        {
            rect = Rect.Empty;
            return false;
        }

        rect = new Rect(PianoRollSettings.LaneHeaderWidth, top, width, height);
        return true;
    }

    private static T? FindAncestor<T>(DependencyObject? start) where T : DependencyObject
    {
        var current = start;
        while (current != null)
        {
            current = VisualTreeHelper.GetParent(current);
            if (current is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    private static double GetPianoKeyLeft()
    {
        return PianoRollSettings.LaneHeaderWidth - PianoRollSettings.PianoKeyWidth;
    }

    #endregion

    #region Smooth Scrolling

    /// <summary>
    /// Gets the effective time to use for rendering (interpolated when playing, actual when stopped).
    /// </summary>
    private TimeSpan RenderTime => _isSmoothScrolling ? _interpolatedTime : CurrentTime;

    private void StartSmoothScrolling()
    {
        if (_isSmoothScrolling) return;

        _isSmoothScrolling = true;
        _syncPosition = CurrentTime;
        _interpolatedTime = CurrentTime;
        _lastRenderTime = TimeSpan.Zero;
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

        // Throttle to 30 FPS in non-overlay mode to reduce CPU usage with complex files
        if (!OverlayMode)
        {
            var timeSinceLastRender = elapsed - _lastRenderTime;
            if (timeSinceLastRender.TotalMilliseconds < 33.3) // ~30 FPS
            {
                // Keep keyboard highlights responsive even when throttling the main render.
                RenderLaneHeaders();
                RenderPlayhead();
                return;
            }
            _lastRenderTime = elapsed;
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
            var lane = _lanes[0];
            var targetHeight = finalSize.Height;
            if (CompactPitchRange && lane.PitchCount > 0)
            {
                targetHeight = Math.Min(targetHeight, lane.PitchCount * 16.0);
            }

            if (Math.Abs(lane.Height - targetHeight) > 0.1)
            {
                lane.Height = targetHeight;
                lane.KeyboardDrawing = null;
            }
        }

        return finalSize;
    }

    private double CalculateTotalLanesHeight()
    {
        if (_lanes.Count == 0)
        {
            return 0;
        }

        var total = 0.0;
        for (var i = 0; i < _lanes.Count; i++)
        {
            total += _lanes[i].Height;
        }

        total += (_lanes.Count - 1) * PianoRollSettings.LaneGap;
        return total;
    }

    private (int Low, int High) GetLanePitchRange(List<PairedNote> notes)
    {
        if (!CompactPitchRange || notes.Count == 0)
        {
            return (_settings.PitchLow, _settings.PitchHigh);
        }

        var min = notes.Min(n => (int)n.NoteNumber) - 1;
        var max = notes.Max(n => (int)n.NoteNumber) + 1;

        min = Math.Max(0, min);
        max = Math.Min(127, max);

        if (max < min)
        {
            return (_settings.PitchLow, _settings.PitchHigh);
        }

        return (min, max);
    }

    private static double CalculateLaneHeight(int pitchCount)
    {
        var height = pitchCount * PianoRollSettings.PitchRowHeight;
        return height < PianoRollSettings.MinLaneHeight ? PianoRollSettings.MinLaneHeight : height;
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
        BuildOverlayTrackIndexCache();
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
            var (pitchLow, pitchHigh) = GetLanePitchRange(_allNotes);
            var pitchCount = pitchHigh - pitchLow + 1;
            var overlayHeight = ActualHeight > 0 ? ActualHeight : 600;
            if (CompactPitchRange && pitchCount > 0)
            {
                overlayHeight = Math.Min(overlayHeight, pitchCount * 16.0);
            }
            var layout = new LaneLayout
            {
                Id = new LaneId(0, 0),
                TrackName = "All Tracks (Overlay)",
                YOffset = 0,
                Height = overlayHeight,
                PitchLow = pitchLow,
                PitchHigh = pitchHigh,
                ActiveTimeline = new LaneEventTimeline(_allNotes)
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

            foreach (var laneId in sortedLanes)
            {
                var trackName = laneId.TrackIndex < _tracks.Count ? _tracks[laneId.TrackIndex].Name : null;
                var laneNotes = notesByLane[laneId];
                var (pitchLow, pitchHigh) = GetLanePitchRange(laneNotes);
                var pitchCount = pitchHigh - pitchLow + 1;
                var laneHeight = CompactPitchRange
                    ? CalculateLaneHeight(pitchCount)
                    : _settings.CalculateLaneHeight();

                var layout = new LaneLayout
                {
                    Id = laneId,
                    TrackName = trackName,
                    YOffset = yOffset,
                    Height = laneHeight,
                    PitchLow = pitchLow,
                    PitchHigh = pitchHigh,
                    ActiveTimeline = new LaneEventTimeline(laneNotes)
                };

                layout.Notes.AddRange(laneNotes);
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

        // Check if any instrument names actually changed
        var anyChanged = false;
        foreach (var lane in _lanes)
        {
            if (lane.Id.Channel < _channelStates.Length)
            {
                var newName = _channelStates[lane.Id.Channel].InstrumentDisplayName;
                if (lane.InstrumentName != newName)
                {
                    lane.InstrumentName = newName;
                    lane.InvalidateHeaderCache();
                    anyChanged = true;
                }
            }
        }

        // Only re-render lane headers if something changed
        if (anyChanged && !_isSmoothScrolling)
        {
            InvalidateLaneHeaders();
        }
    }

    private void ApplyInstrumentNames()
    {
        foreach (var lane in _lanes)
        {
            if (lane.Id.Channel < _channelStates.Length)
            {
                lane.InstrumentName = _channelStates[lane.Id.Channel].InstrumentDisplayName;
                lane.InvalidateHeaderCache();
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
        // Only update if tempo changed significantly
        if (Math.Abs(_currentTempo - bpm) < 0.01) return;

        _currentTempo = bpm;

        // During smooth scrolling, OnRender will pick up the tempo change.
        // When stopped, we need to trigger a render.
        if (!_isSmoothScrolling)
        {
            InvalidateVisual();
        }
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

        GetVisibleVerticalRange(out var visibleTop, out var visibleBottom);

        var clipped = TryGetContentClipRect(out var clipRect);
        if (clipped)
        {
            dc.PushClip(new RectangleGeometry(clipRect));
        }

        // Draw lane backgrounds
        for (var i = 0; i < _lanes.Count; i++)
        {
            var lane = _lanes[i];
            if (lane.YOffset + lane.Height < visibleTop || lane.YOffset > visibleBottom)
            {
                continue;
            }
            var brush = i % 2 == 0 ? LaneBackgroundBrush : LaneAlternateBrush;
            dc.DrawRectangle(
                brush,
                null,
                new Rect(PianoRollSettings.LaneHeaderWidth, lane.YOffset, ActualWidth - PianoRollSettings.LaneHeaderWidth, lane.Height));
        }

        if (clipped)
        {
            dc.Pop();
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

        GetVisibleVerticalRange(out var visibleTop, out var visibleBottom);

        // Draw vertical time grid lines (every second for now, adjust based on zoom)
        var gridInterval = CalculateGridInterval(pixelsPerSecond);
        var startSecond = Math.Floor(leftTime / gridInterval) * gridInterval;

        for (var t = startSecond; t <= rightTime; t += gridInterval)
        {
            if (t < 0) continue;

            var x = PianoRollSettings.LaneHeaderWidth + (t - leftTime) * pixelsPerSecond;
            if (x < PianoRollSettings.LaneHeaderWidth || x > ActualWidth) continue;

            dc.DrawLine(GridPen, new Point(x, visibleTop), new Point(x, visibleBottom));
        }

        var clipped = TryGetContentClipRect(out var clipRect);
        if (clipped)
        {
            dc.PushClip(new RectangleGeometry(clipRect));
        }

        // Draw horizontal pitch grid lines for each lane
        foreach (var lane in _lanes)
        {
            if (lane.YOffset + lane.Height < visibleTop || lane.YOffset > visibleBottom)
            {
                continue;
            }
            RenderPitchGrid(dc, lane);
        }

        if (clipped)
        {
            dc.Pop();
        }
    }

    private void RenderPitchGrid(DrawingContext dc, LaneLayout lane)
    {
        var pitchCount = lane.PitchCount;
        var rowHeight = lane.Height / pitchCount;

        for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
        {
            var relPitch = pitch - lane.PitchLow;
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

        GetVisibleVerticalRange(out var visibleTop, out var visibleBottom);

        var clipped = TryGetContentClipRect(out var clipRect);
        if (clipped)
        {
            dc.PushClip(new RectangleGeometry(clipRect));
        }

        foreach (var lane in _lanes)
        {
            if (lane.YOffset + lane.Height < visibleTop || lane.YOffset > visibleBottom)
            {
                continue;
            }
            RenderLaneNotes(dc, lane, leftTime, rightTime, pixelsPerSecond);
        }

        if (clipped)
        {
            dc.Pop();
        }
    }

    private void RenderLaneNotes(
        DrawingContext dc,
        LaneLayout lane,
        double leftTime,
        double rightTime,
        double pixelsPerSecond)
    {
        var pitchCount = lane.PitchCount;
        var rowHeight = lane.Height / pitchCount;
        var useTrackColors = OverlayMode;

        foreach (var note in lane.Notes)
        {
            var noteStartSec = note.StartTime.TotalSeconds;
            var noteEndSec = note.EndTime.TotalSeconds;

            // Skip notes outside visible range
            if (noteEndSec < leftTime || noteStartSec > rightTime) continue;

            // Skip notes outside pitch range
            if (note.NoteNumber < lane.PitchLow || note.NoteNumber > lane.PitchHigh) continue;

            // Calculate X coordinates
            var x1 = PianoRollSettings.LaneHeaderWidth + (noteStartSec - leftTime) * pixelsPerSecond;
            var x2 = PianoRollSettings.LaneHeaderWidth + (noteEndSec - leftTime) * pixelsPerSecond;

            // Clamp to visible area
            x1 = Math.Max(x1, PianoRollSettings.LaneHeaderWidth);
            x2 = Math.Min(x2, ActualWidth);

            if (x2 <= x1) continue;

            // Calculate Y coordinate (inverted: low pitches at bottom)
            var relPitch = note.NoteNumber - lane.PitchLow;
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
            var renderTime = RenderTime;
            var keyLeft = GetPianoKeyLeft();
            var headerRight = ShowPianoKeys ? keyLeft - 4 : PianoRollSettings.LaneHeaderWidth - 4;

            if (ShowPianoKeys && _lanes.Count > 0)
            {
                UpdateActiveTimelines(renderTime);
                RenderPianoKeysForLane(dc, _lanes[0], renderTime, keyLeft);
            }

            RenderOverlayTrackList(dc, headerRight);

            // Also render note names in overlay mode (using first lane or full height)
            if (ShowNoteNames && _lanes.Count > 0)
            {
                RenderNoteNamesForLane(dc, _lanes[0]);
            }
            return;
        }

        GetVisibleVerticalRange(out var visibleTop, out var visibleBottom);
        var renderTimeLocal = RenderTime;
        var keyLeftLocal = GetPianoKeyLeft();

        if (ShowPianoKeys)
        {
            UpdateActiveTimelines(renderTimeLocal);
        }

        foreach (var lane in _lanes)
        {
            if (lane.YOffset + lane.Height < visibleTop || lane.YOffset > visibleBottom)
            {
                continue;
            }
            var y = lane.YOffset + 4;
            var x = 4.0;
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            EnsureLaneHeaderText(lane, dpi);

            if (ShowPianoKeys)
            {
                RenderPianoKeysForLane(dc, lane, renderTimeLocal, keyLeftLocal);
            }

            // Track name
            if (lane.TrackText != null)
            {
                dc.DrawText(lane.TrackText, new Point(x, y));
                y += 14;
            }

            // Track/Channel info
            if (lane.ChannelText != null)
            {
                dc.DrawText(lane.ChannelText, new Point(x, y));
                y += 12;
            }

            // Instrument name
            if (lane.InstrumentText != null)
            {
                dc.DrawText(lane.InstrumentText, new Point(x, y));
            }

            // Note names on the right side (if enabled)
            if (ShowNoteNames)
            {
                RenderNoteNamesForLane(dc, lane);
            }
        }
    }

    private static void EnsureLaneHeaderText(LaneLayout lane, double dpi)
    {
        var trackName = lane.TrackName ?? string.Empty;
        var channelLabel = $"T{lane.Id.TrackIndex} Ch{lane.Id.Channel + 1}";
        var instrumentName = lane.InstrumentName;

        if (Math.Abs(lane.CachedHeaderDpi - dpi) < 0.1
            && lane.CachedTrackName == trackName
            && lane.CachedChannelLabel == channelLabel
            && lane.CachedInstrumentName == instrumentName)
        {
            return;
        }

        lane.TrackText = string.IsNullOrEmpty(trackName)
            ? null
            : new FormattedText(
                trackName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                11,
                Brushes.White,
                dpi);

        lane.ChannelText = new FormattedText(
            channelLabel,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            10,
            Brushes.LightGray,
            dpi);

        lane.InstrumentText = new FormattedText(
            instrumentName,
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            10,
            Brushes.CornflowerBlue,
            dpi);

        lane.CachedHeaderDpi = dpi;
        lane.CachedTrackName = trackName;
        lane.CachedChannelLabel = channelLabel;
        lane.CachedInstrumentName = instrumentName;
    }

    private static void RenderPianoKeysForLane(DrawingContext dc, LaneLayout lane, TimeSpan renderTime, double keyLeft)
    {
        var pitchCount = lane.PitchCount;
        if (pitchCount <= 0)
        {
            return;
        }

        var rowHeight = lane.Height / pitchCount;
        var keyWidth = PianoRollSettings.PianoKeyWidth;
        var blackKeyWidth = keyWidth * 0.6;
        var blackKeyHeight = rowHeight * 0.6;
        var blackKeyX = keyLeft;

        EnsureKeyboardDrawing(lane, keyLeft, keyWidth, rowHeight);
        if (lane.KeyboardDrawing != null)
        {
            dc.DrawDrawing(lane.KeyboardDrawing);
        }
        if (!lane.ActiveTimeline.HasAnyActive)
        {
            return;
        }

        var activeNotes = lane.ActiveTimeline.ActiveNotes;

        // Draw white key highlights
        for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
        {
            if (IsBlackKey(pitch) || !activeNotes[pitch])
            {
                continue;
            }

            var relPitch = pitch - lane.PitchLow;
            var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;
            var rect = new Rect(keyLeft, y + 1, keyWidth, rowHeight - 2);
            var inset = Math.Max(1, rowHeight * 0.12);
            var glowRect = new Rect(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            var innerRect = new Rect(rect.X + inset, rect.Y + inset, rect.Width - inset * 2, rect.Height - inset * 2);
            dc.DrawRectangle(PianoActiveGlowBrush, null, glowRect);
            dc.DrawRectangle(PianoActiveFillBrush, null, rect);
            dc.DrawRectangle(null, PianoActiveOuterPen, rect);
            dc.DrawRectangle(null, PianoActiveInnerPen, innerRect);
        }

        // Draw black key highlights
        for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
        {
            if (!IsBlackKey(pitch) || !activeNotes[pitch])
            {
                continue;
            }

            var relPitch = pitch - lane.PitchLow;
            var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;
            var rect = new Rect(
                blackKeyX,
                y + (rowHeight - blackKeyHeight) / 2,
                blackKeyWidth,
                blackKeyHeight);
            var glowRect = new Rect(rect.X - 5, rect.Y - 5, rect.Width + 10, rect.Height + 10);
            var inset = Math.Max(1, rowHeight * 0.12);
            var innerRect = new Rect(rect.X + inset, rect.Y + inset, rect.Width - inset * 2, rect.Height - inset * 2);
            dc.DrawRectangle(PianoActiveGlowBrush, null, glowRect);
            dc.DrawRectangle(PianoActiveFillBrush, null, rect);
            dc.DrawRectangle(null, PianoActiveOuterPen, rect);
            dc.DrawRectangle(null, PianoActiveInnerPen, innerRect);
        }
    }

    private static void EnsureKeyboardDrawing(LaneLayout lane, double keyLeft, double keyWidth, double rowHeight)
    {
        if (lane.KeyboardDrawing != null
            && Math.Abs(lane.KeyboardKeyLeft - keyLeft) < 0.1
            && Math.Abs(lane.KeyboardRowHeight - rowHeight) < 0.1
            && Math.Abs(lane.KeyboardWidth - keyWidth) < 0.1
            && Math.Abs(lane.KeyboardHeight - lane.Height) < 0.1
            && lane.KeyboardPitchLow == lane.PitchLow
            && lane.KeyboardPitchHigh == lane.PitchHigh)
        {
            return;
        }

        var drawing = new DrawingGroup();
        using (var dc = drawing.Open())
        {
            var blackKeyWidth = keyWidth * 0.6;
            var blackKeyHeight = rowHeight * 0.6;
            var blackKeyX = keyLeft;

            // White keybed behind the keys for contrast
            var keybedRect = new Rect(keyLeft, lane.YOffset, keyWidth, lane.Height);
            dc.DrawRectangle(PianoWhiteBrush, null, keybedRect);

            // Draw white keys
            for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
            {
                if (IsBlackKey(pitch))
                {
                    continue;
                }

                var relPitch = pitch - lane.PitchLow;
                var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;
                var rect = new Rect(keyLeft, y + 1, keyWidth, rowHeight - 2);
                dc.DrawRectangle(PianoWhiteBrush, PianoKeyOutlinePen, rect);
            }

            // Draw black keys on top
            for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
            {
                if (!IsBlackKey(pitch))
                {
                    continue;
                }

                var relPitch = pitch - lane.PitchLow;
                var y = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;
                var rect = new Rect(
                    blackKeyX,
                    y + (rowHeight - blackKeyHeight) / 2,
                    blackKeyWidth,
                    blackKeyHeight);
                dc.DrawRectangle(PianoBlackBrush, null, rect);
            }
        }

        drawing.Freeze();

        lane.KeyboardDrawing = drawing;
        lane.KeyboardKeyLeft = keyLeft;
        lane.KeyboardRowHeight = rowHeight;
        lane.KeyboardWidth = keyWidth;
        lane.KeyboardHeight = lane.Height;
        lane.KeyboardPitchLow = lane.PitchLow;
        lane.KeyboardPitchHigh = lane.PitchHigh;
    }

    private void UpdateActiveTimelines(TimeSpan renderTime)
    {
        if (renderTime == _lastActiveTimelineUpdate)
        {
            return;
        }

        if (renderTime < _lastActiveTimelineUpdate)
        {
            foreach (var lane in _lanes)
            {
                lane.ActiveTimeline.Reset();
            }
        }

        foreach (var lane in _lanes)
        {
            lane.ActiveTimeline.AdvanceTo(renderTime);
        }

        _lastActiveTimelineUpdate = renderTime;
    }

    private static bool IsBlackKey(int pitch)
    {
        return pitch % 12 is 1 or 3 or 6 or 8 or 10;
    }

    private void RenderNoteNamesForLane(DrawingContext dc, LaneLayout lane)
    {
        var pitchCount = lane.PitchCount;
        if (pitchCount <= 0)
        {
            return;
        }
        var rowHeight = lane.Height / pitchCount;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var rightEdge = ShowPianoKeys
            ? GetPianoKeyLeft() - 2
            : PianoRollSettings.LaneHeaderWidth - 2;

        // Calculate font size to fit within row height (leave some padding)
        var fontSize = Math.Max(6, Math.Min(rowHeight * 0.9, 10));
        EnsureNoteNameCache(lane, fontSize, dpi);

        for (var pitch = lane.PitchLow; pitch <= lane.PitchHigh; pitch++)
        {
            var relPitch = pitch - lane.PitchLow;
            var noteY = lane.YOffset + lane.Height - (relPitch + 1) * rowHeight;

            var noteText = lane.NoteNameTexts?[relPitch];
            if (noteText is null)
            {
                continue;
            }

            // Right-align the text, center vertically in row
            var textX = rightEdge - noteText.Width;
            var textY = noteY + (rowHeight - noteText.Height) / 2;

            dc.DrawText(noteText, new Point(textX, textY));
        }
    }

    private static void EnsureNoteNameCache(LaneLayout lane, double fontSize, double dpi)
    {
        var pitchLow = lane.PitchLow;
        var pitchHigh = lane.PitchHigh;

        if (lane.NoteNameTexts is not null
            && Math.Abs(lane.NoteNameFontSize - fontSize) < 0.1
            && Math.Abs(lane.NoteNameDpi - dpi) < 0.1
            && lane.NoteNamePitchLow == pitchLow
            && lane.NoteNamePitchHigh == pitchHigh)
        {
            return;
        }

        if (pitchHigh < pitchLow)
        {
            lane.NoteNameTexts = null;
            lane.NoteNameFontSize = fontSize;
            lane.NoteNameDpi = dpi;
            lane.NoteNamePitchLow = pitchLow;
            lane.NoteNamePitchHigh = pitchHigh;
            return;
        }

        var count = pitchHigh - pitchLow + 1;
        var texts = new FormattedText[count];

        for (var pitch = pitchLow; pitch <= pitchHigh; pitch++)
        {
            var noteName = GetNoteNameShort(pitch);
            var isOctave = pitch % 12 == 0;

            texts[pitch - pitchLow] = new FormattedText(
                noteName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                fontSize,
                isOctave ? Brushes.White : Brushes.Gray,
                dpi);
        }

        lane.NoteNameTexts = texts;
        lane.NoteNameFontSize = fontSize;
        lane.NoteNameDpi = dpi;
        lane.NoteNamePitchLow = pitchLow;
        lane.NoteNamePitchHigh = pitchHigh;
    }

    private static string GetNoteNameShort(int midiNote)
    {
        var noteNames = new[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        var octave = (midiNote / 12) - 1;
        var note = noteNames[midiNote % 12];
        return $"{note}{octave}";
    }

    private void RenderOverlayTrackList(DrawingContext dc, double headerRight)
    {
        var y = 8.0;
        var x = 4.0;
        const double lineHeight = 16.0;
        const double colorBoxSize = 10.0;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        EnsureOverlayTrackTextCache(headerRight, dpi);

        // Title
        if (_overlayTrackTitleText != null)
        {
            dc.DrawText(_overlayTrackTitleText, new Point(x, y));
        }
        y += lineHeight + 4;

        // Draw each track with its color
        for (var i = 0; i < _overlayTrackIndices.Count; i++)
        {
            if (y + lineHeight > ActualHeight - 4)
            {
                // Show "..." if we run out of space
                var moreText = new FormattedText(
                    $"... +{_overlayTrackIndices.Count - i} more",
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
            var trackIndex = _overlayTrackIndices[i];
            var trackBrush = TrackColorMapper.GetBrush(trackIndex);
            var colorRect = new Rect(x, y + 2, colorBoxSize, colorBoxSize);
            dc.DrawRectangle(trackBrush, null, colorRect);

            var trackText = _overlayTrackTexts != null && i < _overlayTrackTexts.Count
                ? _overlayTrackTexts[i]
                : null;
            if (trackText != null)
            {
                dc.DrawText(trackText, new Point(x + colorBoxSize + 4, y));
            }
            y += lineHeight;
        }
    }

    private void BuildOverlayTrackIndexCache()
    {
        _overlayTrackIndices = _allNotes
            .Select(n => n.TrackIndex)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        _overlayTrackNames = new List<string>(_overlayTrackIndices.Count);
        foreach (var trackIndex in _overlayTrackIndices)
        {
            var trackName = trackIndex < _tracks.Count && !string.IsNullOrEmpty(_tracks[trackIndex].Name)
                ? _tracks[trackIndex].Name
                : $"Track {trackIndex}";
            _overlayTrackNames.Add(trackName ?? string.Empty);
        }

        _overlayTrackTexts = null;
        _overlayTrackTitleText = null;
        _overlayTrackDpi = 0;
        _overlayTrackMaxWidth = 0;
    }

    private void EnsureOverlayTrackTextCache(double headerRight, double dpi)
    {
        var maxWidth = headerRight - 4 - 10 - 8;
        if (_overlayTrackTexts != null
            && Math.Abs(_overlayTrackDpi - dpi) < 0.1
            && Math.Abs(_overlayTrackMaxWidth - maxWidth) < 0.1
            && _overlayTrackTexts.Count == _overlayTrackNames.Count)
        {
            return;
        }

        _overlayTrackTitleText = new FormattedText(
            "Tracks",
            System.Globalization.CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            LabelTypeface,
            11,
            Brushes.White,
            dpi);

        var texts = new List<FormattedText>(_overlayTrackNames.Count);
        foreach (var trackName in _overlayTrackNames)
        {
            var text = new FormattedText(
                trackName,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                LabelTypeface,
                10,
                Brushes.White,
                dpi)
            {
                MaxTextWidth = maxWidth,
                Trimming = TextTrimming.CharacterEllipsis
            };

            texts.Add(text);
        }

        _overlayTrackTexts = texts;
        _overlayTrackDpi = dpi;
        _overlayTrackMaxWidth = maxWidth;
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
