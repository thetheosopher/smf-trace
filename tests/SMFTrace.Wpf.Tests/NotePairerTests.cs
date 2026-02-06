using SMFTrace.Core.Models;
using SMFTrace.Wpf.Controls;
using Xunit;

namespace SMFTrace.Wpf.Tests;

public class NotePairerTests
{
    private static NoteOnEvent CreateNoteOn(long tick, byte channel, byte note, byte velocity = 100, int track = 0) =>
        new()
        {
            AbsoluteTick = tick,
            Time = TimeSpan.FromMilliseconds(tick),
            TrackIndex = track,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = velocity
        };

    private static NoteOffEvent CreateNoteOff(long tick, byte channel, byte note, int track = 0) =>
        new()
        {
            AbsoluteTick = tick,
            Time = TimeSpan.FromMilliseconds(tick),
            TrackIndex = track,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = 64
        };

    [Fact]
    public void PairNotesCreatesCorrectPairs()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60),
            CreateNoteOff(480, 0, 60)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Single(pairs);
        Assert.Equal(0L, pairs[0].StartTick);
        Assert.Equal(480L, pairs[0].EndTick);
        Assert.Equal(60, pairs[0].NoteNumber);
    }

    [Fact]
    public void NoteOnVelocityZeroTreatedAsNoteOff()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60),
            CreateNoteOn(480, 0, 60, velocity: 0)  // NoteOn with velocity 0
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Single(pairs);
        Assert.Equal(480L, pairs[0].EndTick);
    }

    [Fact]
    public void OverlappingNoteOnClosesPreviousNote()
    {
        // Arrange - Same note on same channel, overlapping
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60, velocity: 80),
            CreateNoteOn(240, 0, 60, velocity: 100),  // Overlapping NoteOn
            CreateNoteOff(480, 0, 60)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Equal(2, pairs.Count);

        // First note: 0 -> 240 (closed by overlapping NoteOn)
        Assert.Equal(0L, pairs[0].StartTick);
        Assert.Equal(240L, pairs[0].EndTick);
        Assert.Equal(80, pairs[0].Velocity);

        // Second note: 240 -> 480
        Assert.Equal(240L, pairs[1].StartTick);
        Assert.Equal(480L, pairs[1].EndTick);
        Assert.Equal(100, pairs[1].Velocity);
    }

    [Fact]
    public void MultipleChannelsHandledSeparately()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60),
            CreateNoteOn(100, 1, 60),
            CreateNoteOff(480, 0, 60),
            CreateNoteOff(480, 1, 60)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Equal(2, pairs.Count);
        Assert.Equal(0, pairs[0].Channel);
        Assert.Equal(1, pairs[1].Channel);
    }

    [Fact]
    public void MultipleTracksHandledSeparately()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60, track: 0),
            CreateNoteOn(0, 0, 60, track: 1),
            CreateNoteOff(480, 0, 60, track: 0),
            CreateNoteOff(480, 0, 60, track: 1)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Equal(2, pairs.Count);
        Assert.Equal(0, pairs[0].TrackIndex);
        Assert.Equal(1, pairs[1].TrackIndex);
    }

    [Fact]
    public void OrphanNoteOffIsIgnored()
    {
        // Arrange - NoteOff without matching NoteOn
        var events = new List<MidiEventBase>
        {
            CreateNoteOff(480, 0, 60)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Empty(pairs);
    }

    [Fact]
    public void UnterminatedNotesGetDefaultDuration()
    {
        // Arrange - NoteOn without matching NoteOff
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Single(pairs);
        Assert.Equal(0L, pairs[0].StartTick);
        Assert.True(pairs[0].EndTick > 0);  // Has some default duration
    }

    [Fact]
    public void GroupByLaneGroupsCorrectly()
    {
        // Arrange
        var notes = new List<PairedNote>
        {
            new()
            {
                StartTick = 0, EndTick = 480,
                StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromMilliseconds(480),
                Channel = 0, NoteNumber = 60, Velocity = 100, TrackIndex = 0,
                Lane = new LaneId(0, 0)
            },
            new()
            {
                StartTick = 0, EndTick = 480,
                StartTime = TimeSpan.Zero, EndTime = TimeSpan.FromMilliseconds(480),
                Channel = 1, NoteNumber = 60, Velocity = 100, TrackIndex = 0,
                Lane = new LaneId(0, 1)
            },
            new()
            {
                StartTick = 100, EndTick = 480,
                StartTime = TimeSpan.FromMilliseconds(100), EndTime = TimeSpan.FromMilliseconds(480),
                Channel = 0, NoteNumber = 62, Velocity = 100, TrackIndex = 0,
                Lane = new LaneId(0, 0)
            }
        };

        // Act
        var grouped = NotePairer.GroupByLane(notes);

        // Assert
        Assert.Equal(2, grouped.Count);
        Assert.Equal(2, grouped[new LaneId(0, 0)].Count);
        Assert.Single(grouped[new LaneId(0, 1)]);
    }

    [Fact]
    public void LaneIdAssignedCorrectly()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 5, 60, track: 2),
            CreateNoteOff(480, 5, 60, track: 2)
        };

        // Act
        var pairs = NotePairer.PairNotes(events);

        // Assert
        Assert.Single(pairs);
        Assert.Equal(new LaneId(2, 5), pairs[0].Lane);
    }
}
