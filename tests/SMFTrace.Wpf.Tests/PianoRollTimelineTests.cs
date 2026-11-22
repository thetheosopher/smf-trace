using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Melanchall.DryWetMidi.Interaction;
using SMFTrace.Core.Models;
using SMFTrace.Wpf.Controls;
using Xunit;

namespace SMFTrace.Wpf.Tests;

public sealed class PianoRollTimelineTests
{
    [Fact]
    public async Task NoteContextMenuUsesTheRenderedNoteWithoutSeeking()
    {
        var result = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var note = new NoteOnEvent { AbsoluteTick = 0, Time = TimeSpan.Zero, TrackIndex = 0,
                    OriginalIndex = 0, RawBytes = [], NoteNumber = 60, Velocity = 100, Channel = 0 };
                var panel = new TestPanel { WindowSeconds = 4, OverlayMode = true, CompactPitchRange = true };
                panel.SetTimeline(TempoMap.Default, [note]);
                panel.LoadNotes([note], []);
                panel.Measure(new Size(800, 400));
                panel.Arrange(new Rect(0, 0, 800, 400));
                panel.RenderTimelineVisuals();

                NoteOnEvent? inspected = null;
                panel.InspectNoteRequested += (_, value) => inspected = value;
                panel.SeekDragStarted += (_, _) => throw new InvalidOperationException("Inspecting must not start a seek.");
                Assert.Null(panel.CreateNoteContextMenu(new Point(420, 20)));
                var menu = Assert.IsType<ContextMenu>(panel.CreateNoteContextMenu(new Point(420, 54)));
                var item = Assert.IsType<MenuItem>(Assert.Single(menu.Items));
                item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
                Assert.Same(note, inspected);
                Assert.Equal(TimeSpan.Zero, panel.CurrentTime);
                result.SetResult();
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        await result.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task VisibleMarkerActivatesItsExactTick()
    {
        var result = new TaskCompletionSource<(bool IsInputTarget, long? Tick)>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                var panel = new TestPanel { WindowSeconds = 4, TotalDuration = TimeSpan.FromSeconds(3) };
                panel.SetTimeline(TempoMap.Default, [
                    new MetaEvent { AbsoluteTick = 960, Time = TimeSpan.FromSeconds(1), TrackIndex = 0,
                        OriginalIndex = 0, RawBytes = [], MetaType = 0x06, Data = "Verse"u8.ToArray() }
                ]);
                panel.Measure(new Size(800, 400));
                panel.Arrange(new Rect(0, 0, 800, 400));
                panel.RenderTimelineVisuals();
                long? tick = null;
                panel.MarkerClicked += (_, value) => tick = value;
                var markerPoint = new Point(552, 27);
                var hit = VisualTreeHelper.HitTest(panel, markerPoint)?.VisualHit;
                var isInputTarget = ReferenceEquals(hit, panel) ||
                    (hit != null && ReferenceEquals(VisualTreeHelper.GetParent(hit), panel));
                panel.TryActivateMarker(markerPoint);
                result.SetResult((isInputTarget, tick));
            }
            catch (Exception ex)
            {
                result.SetException(ex);
            }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        var marker = await result.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.True(marker.IsInputTarget);
        Assert.Equal(960, marker.Tick);
    }

    private sealed class TestPanel : PianoRollPanel
    {
        public void RenderTimelineVisuals()
        {
            using var context = new DrawingVisual().RenderOpen();
            OnRender(context);
        }
    }
}
