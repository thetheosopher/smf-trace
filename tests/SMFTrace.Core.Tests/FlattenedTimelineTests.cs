using SMFTrace.Core.Models;
using SMFTrace.Core.Sequencer;

namespace SMFTrace.Core.Tests;

public class FlattenedTimelineTests
{
    private static NoteOnEvent CreateNoteOn(long tick, TimeSpan time, int track, byte channel, byte note) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = 0,
            Time = time,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = 100
        };

    private static NoteOffEvent CreateNoteOff(long tick, TimeSpan time, int track, byte channel, byte note) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = 0,
            Time = time,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = 0
        };

    [Fact]
    public void FindIndexAtOrAfterTickReturnsCorrectIndex()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 0, 61),
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 0, 0, 62),
            CreateNoteOn(300, TimeSpan.FromSeconds(1.5), 0, 0, 63)
        };
        var timeline = new FlattenedTimeline(events);

        // Act & Assert
        Assert.Equal(0, timeline.FindIndexAtOrAfter(0));
        Assert.Equal(1, timeline.FindIndexAtOrAfter(100));
        Assert.Equal(1, timeline.FindIndexAtOrAfter(50));  // Between 0 and 100
        Assert.Equal(2, timeline.FindIndexAtOrAfter(150)); // Between 100 and 200
        Assert.Equal(4, timeline.FindIndexAtOrAfter(400)); // Past end
    }

    [Fact]
    public void FindIndexAtOrAfterTimeReturnsCorrectIndex()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 0, 61),
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 0, 0, 62)
        };
        var timeline = new FlattenedTimeline(events);

        // Act & Assert
        Assert.Equal(0, timeline.FindIndexAtOrAfter(TimeSpan.Zero));
        Assert.Equal(1, timeline.FindIndexAtOrAfter(TimeSpan.FromSeconds(0.5)));
        Assert.Equal(1, timeline.FindIndexAtOrAfter(TimeSpan.FromSeconds(0.25)));
        Assert.Equal(3, timeline.FindIndexAtOrAfter(TimeSpan.FromSeconds(2.0)));
    }

    [Fact]
    public void GetEventsInTickRangeReturnsCorrectEvents()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 0, 61),
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 0, 0, 62),
            CreateNoteOn(300, TimeSpan.FromSeconds(1.5), 0, 0, 63)
        };
        var timeline = new FlattenedTimeline(events);

        // Act
        var result = timeline.GetEventsInRange(100, 250).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(100, result[0].AbsoluteTick);
        Assert.Equal(200, result[1].AbsoluteTick);
    }

    [Fact]
    public void GetEventsInTimeRangeReturnsCorrectEvents()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 0, 61),
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 0, 0, 62)
        };
        var timeline = new FlattenedTimeline(events);

        // Act
        var result = timeline.GetEventsInRange(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.75)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(100, result[0].AbsoluteTick);
    }

    [Fact]
    public void LanesAreCorrectlyIdentified()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),     // Track 0, Channel 0
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 1, 61),  // Track 0, Channel 1
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 1, 0, 62)   // Track 1, Channel 0
        };
        var timeline = new FlattenedTimeline(events);

        // Act
        var lanes = timeline.Lanes;

        // Assert
        Assert.Equal(3, lanes.Count);
        Assert.Contains(new LaneId(0, 0), lanes);
        Assert.Contains(new LaneId(0, 1), lanes);
        Assert.Contains(new LaneId(1, 0), lanes);
    }

    [Fact]
    public void GetEventsForLaneReturnsCorrectEvents()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, TimeSpan.Zero, 0, 0, 60),
            CreateNoteOn(100, TimeSpan.FromSeconds(0.5), 0, 1, 61),
            CreateNoteOn(200, TimeSpan.FromSeconds(1.0), 0, 0, 62),
            CreateNoteOff(300, TimeSpan.FromSeconds(1.5), 0, 0, 60)
        };
        var timeline = new FlattenedTimeline(events);

        // Act
        var lane0Events = timeline.GetEventsForLane(new LaneId(0, 0)).ToList();

        // Assert
        Assert.Equal(3, lane0Events.Count);
    }

    [Fact]
    public void EmptyTimelineHandledCorrectly()
    {
        // Arrange
        var events = new List<MidiEventBase>();
        var timeline = new FlattenedTimeline(events);

        // Act & Assert
        Assert.Equal(0, timeline.Count);
        Assert.Empty(timeline.Lanes);
        Assert.Equal(0, timeline.FindIndexAtOrAfter(100));
        Assert.Empty(timeline.GetEventsInRange(0, 1000));
    }
}
