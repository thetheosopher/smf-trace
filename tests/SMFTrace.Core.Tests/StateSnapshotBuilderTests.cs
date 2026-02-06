using SMFTrace.Core.Models;
using SMFTrace.Core.Sequencer;

namespace SMFTrace.Core.Tests;

public class StateSnapshotBuilderTests
{
    private static ControlChangeEvent CreateBankMsb(long tick, byte channel, byte value) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = 0,
            Value = value
        };

    private static ControlChangeEvent CreateBankLsb(long tick, byte channel, byte value) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = 32,
            Value = value
        };

    private static ProgramChangeEvent CreateProgramChange(long tick, byte channel, byte program) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            ProgramNumber = program
        };

    private static ControlChangeEvent CreateCC(long tick, byte channel, byte cc, byte value) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = cc,
            Value = value
        };

    private static NoteOnEvent CreateNoteOn(long tick, byte channel, byte note) =>
        new()
        {
            AbsoluteTick = tick,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = 100
        };

    [Fact]
    public void RebuildStateAtTickReturnsDefaultsWhenNoEvents()
    {
        // Arrange
        var events = new List<MidiEventBase>();
        var builder = new StateSnapshotBuilder(events);

        // Act
        var states = builder.RebuildStateAtTick(1000);

        // Assert
        foreach (var state in states)
        {
            Assert.False(state.HasProgramChange);
            Assert.Equal("(default)", state.InstrumentDisplayName);
        }
    }

    [Fact]
    public void RebuildStateAtTickTracksBankAndProgram()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateBankMsb(100, 0, 5),
            CreateBankLsb(100, 0, 10),
            CreateProgramChange(100, 0, 42),
            CreateNoteOn(200, 0, 60)
        };
        var builder = new StateSnapshotBuilder(events);

        // Act
        var states = builder.RebuildStateAtTick(150);

        // Assert
        var channel0 = states[0];
        Assert.True(channel0.HasProgramChange);
        Assert.Equal(5, channel0.BankMsb);
        Assert.Equal(10, channel0.BankLsb);
        Assert.Equal(42, channel0.Program);
    }

    [Fact]
    public void RebuildStateAtTickTracksControllers()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateCC(100, 0, 7, 100),   // Volume
            CreateCC(200, 0, 10, 64),   // Pan
            CreateCC(300, 0, 7, 80)     // Volume again (updated)
        };
        var builder = new StateSnapshotBuilder(events);

        // Act
        var stateAt250 = builder.RebuildStateAtTick(250);
        var stateAt350 = builder.RebuildStateAtTick(350);

        // Assert
        Assert.Equal(100, stateAt250[0].Controllers[7]);  // Volume before update
        Assert.Equal(64, stateAt250[0].Controllers[10]);  // Pan

        Assert.Equal(80, stateAt350[0].Controllers[7]);   // Volume after update
        Assert.Equal(64, stateAt350[0].Controllers[10]);  // Pan unchanged
    }

    [Fact]
    public void RebuildStateAtTickOnlyIncludesEventsUpToTargetTick()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateProgramChange(100, 0, 10),
            CreateProgramChange(200, 0, 20),
            CreateProgramChange(300, 0, 30)
        };
        var builder = new StateSnapshotBuilder(events);

        // Act
        var stateAt150 = builder.RebuildStateAtTick(150);
        var stateAt250 = builder.RebuildStateAtTick(250);

        // Assert
        Assert.Equal(10, stateAt150[0].Program);  // Only first PC
        Assert.Equal(20, stateAt250[0].Program);  // First and second PC
    }

    [Fact]
    public void RebuildStateAtTickUsesCheckpointsForFastSeek()
    {
        // Arrange - Create many events
        var events = new List<MidiEventBase>();
        for (long tick = 0; tick < 10000; tick += 100)
        {
            events.Add(CreateCC(tick, 0, 7, (byte)(tick / 100 % 128)));
        }

        var builder = new StateSnapshotBuilder(events, checkpointInterval: 1000);

        // Act - Should use checkpoints efficiently
        var state = builder.RebuildStateAtTick(5500);

        // Assert
        Assert.True(builder.Checkpoints.Count > 1);
        Assert.Equal((byte)(55 % 128), state[0].Controllers[7]);
    }

    [Fact]
    public void GetResumeEventIndexReturnsCorrectIndex()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60),
            CreateNoteOn(100, 0, 61),
            CreateNoteOn(200, 0, 62),
            CreateNoteOn(300, 0, 63)
        };
        var builder = new StateSnapshotBuilder(events);

        // Act & Assert
        Assert.Equal(0, builder.GetResumeEventIndex(0));
        Assert.Equal(1, builder.GetResumeEventIndex(100));
        Assert.Equal(2, builder.GetResumeEventIndex(150)); // Between events, returns next
        Assert.Equal(4, builder.GetResumeEventIndex(400)); // Past end
    }

    [Fact]
    public void CheckpointsAreCreatedAtIntervals()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(0, 0, 60),
            CreateNoteOn(500, 0, 61),
            CreateNoteOn(1000, 0, 62),
            CreateNoteOn(1500, 0, 63),
            CreateNoteOn(2000, 0, 64)
        };
        var builder = new StateSnapshotBuilder(events, checkpointInterval: 1000);

        // Assert
        Assert.True(builder.Checkpoints.Count >= 3); // At least 0, 1000, 2000
        Assert.Equal(0, builder.Checkpoints[0].Tick);
    }

    [Fact]
    public void MultipleChannelsTrackedIndependently()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateProgramChange(100, 0, 10),
            CreateProgramChange(100, 1, 20),
            CreateCC(100, 2, 7, 80)
        };
        var builder = new StateSnapshotBuilder(events);

        // Act
        var states = builder.RebuildStateAtTick(150);

        // Assert
        Assert.True(states[0].HasProgramChange);
        Assert.Equal(10, states[0].Program);

        Assert.True(states[1].HasProgramChange);
        Assert.Equal(20, states[1].Program);

        Assert.False(states[2].HasProgramChange);
        Assert.Equal(80, states[2].Controllers[7]);
    }
}
