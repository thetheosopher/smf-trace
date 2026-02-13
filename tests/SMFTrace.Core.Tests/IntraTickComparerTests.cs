using SMFTrace.Core.Models;
using SMFTrace.Core.Ordering;

namespace SMFTrace.Core.Tests;

#pragma warning disable CA1859 // Use concrete types when possible for improved performance
public class IntraTickComparerTests
{
    private static MidiEventBase CreateNoteOn(long tick, int track, int originalIndex, byte channel, byte note, byte velocity) =>
        new NoteOnEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = velocity
        };

    private static MidiEventBase CreateNoteOff(long tick, int track, int originalIndex, byte channel, byte note) =>
        new NoteOffEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            NoteNumber = note,
            Velocity = 0
        };

    private static MidiEventBase CreateProgramChange(long tick, int track, int originalIndex, byte channel, byte program) =>
        new ProgramChangeEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            ProgramNumber = program
        };

    private static MidiEventBase CreateBankSelectMsb(long tick, int track, int originalIndex, byte channel, byte value) =>
        new ControlChangeEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = 0, // CC0 = Bank Select MSB
            Value = value
        };

    private static MidiEventBase CreateBankSelectLsb(long tick, int track, int originalIndex, byte channel, byte value) =>
        new ControlChangeEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = 32, // CC32 = Bank Select LSB
            Value = value
        };

    private static MidiEventBase CreateControlChange(long tick, int track, int originalIndex, byte channel, byte cc, byte value) =>
        new ControlChangeEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            Channel = channel,
            ControllerNumber = cc,
            Value = value
        };

    private static MidiEventBase CreateMetaEvent(long tick, int track, int originalIndex, byte type) =>
        new MetaEvent
        {
            AbsoluteTick = tick,
            TrackIndex = track,
            OriginalIndex = originalIndex,
            RawBytes = [],
            MetaType = type,
            Data = []
        };

    [Fact]
    public void EventsAtDifferentTicksOrderedByTick()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 0, 0, 60, 100),
            CreateNoteOn(50, 0, 1, 0, 60, 100),
            CreateNoteOn(200, 0, 2, 0, 60, 100)
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.Equal(50, events[0].AbsoluteTick);
        Assert.Equal(100, events[1].AbsoluteTick);
        Assert.Equal(200, events[2].AbsoluteTick);
    }

    [Fact]
    public void BankSelectComesBeforeProgramChange()
    {
        // Arrange - At same tick, Bank Select should come before Program Change
        var events = new List<MidiEventBase>
        {
            CreateProgramChange(100, 0, 1, 0, 5),
            CreateBankSelectMsb(100, 0, 0, 0, 1)
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.IsType<ControlChangeEvent>(events[0]); // Bank Select MSB
        Assert.IsType<ProgramChangeEvent>(events[1]);
    }

    [Fact]
    public void BankSelectMsbAndLsbComeTogether()
    {
        // Arrange - Both Bank Select MSB and LSB should come before Program Change
        var events = new List<MidiEventBase>
        {
            CreateProgramChange(100, 0, 2, 0, 5),
            CreateBankSelectLsb(100, 0, 1, 0, 2),
            CreateBankSelectMsb(100, 0, 0, 0, 1)
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        var cc0 = Assert.IsType<ControlChangeEvent>(events[0]);
        Assert.True(cc0.IsBankSelect);
        var cc1 = Assert.IsType<ControlChangeEvent>(events[1]);
        Assert.True(cc1.IsBankSelect);
        Assert.IsType<ProgramChangeEvent>(events[2]);
    }

    [Fact]
    public void ProgramChangeComesBeforeNoteOn()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 1, 0, 60, 100),
            CreateProgramChange(100, 0, 0, 0, 5)
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.IsType<ProgramChangeEvent>(events[0]);
        Assert.IsType<NoteOnEvent>(events[1]);
    }

    [Fact]
    public void OtherCcComesBeforeNoteOn()
    {
        // Arrange - Non-bank-select CC should come before NoteOn
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 1, 0, 60, 100),
            CreateControlChange(100, 0, 0, 0, 7, 100) // CC7 = Volume
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.IsType<ControlChangeEvent>(events[0]);
        Assert.IsType<NoteOnEvent>(events[1]);
    }

    [Fact]
    public void NoteOnComesBeforeNoteOff()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateNoteOff(100, 0, 1, 0, 60),
            CreateNoteOn(100, 0, 0, 0, 60, 100)
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.IsType<NoteOnEvent>(events[0]);
        Assert.IsType<NoteOffEvent>(events[1]);
    }

    [Fact]
    public void MetaEventsComesFirst()
    {
        // Arrange - Meta events (like tempo) should come before all channel events
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 1, 0, 60, 100),
            CreateMetaEvent(100, 0, 0, 0x51) // Set Tempo
        };

        // Act
        EventSorter.Sort(events);

        // Assert
        Assert.IsType<MetaEvent>(events[0]);
        Assert.IsType<NoteOnEvent>(events[1]);
    }

    [Fact]
    public void FullOrderingSequence()
    {
        // Arrange - Full sequence: Meta -> BankSelect -> ProgramChange -> CC -> NoteOn -> NoteOff
        var events = new List<MidiEventBase>
        {
            CreateNoteOff(100, 0, 5, 0, 60),
            CreateNoteOn(100, 0, 4, 0, 60, 100),
            CreateControlChange(100, 0, 3, 0, 7, 100), // Volume
            CreateProgramChange(100, 0, 2, 0, 5),
            CreateBankSelectMsb(100, 0, 1, 0, 1),
            CreateMetaEvent(100, 0, 0, 0x51) // Set Tempo
        };

        // Act
        EventSorter.Sort(events);

        // Assert - Verify order
        Assert.IsType<MetaEvent>(events[0]);
        var cc = Assert.IsType<ControlChangeEvent>(events[1]);
        Assert.True(cc.IsBankSelectMsb);
        Assert.IsType<ProgramChangeEvent>(events[2]);
        var volume = Assert.IsType<ControlChangeEvent>(events[3]);
        Assert.Equal(7, volume.ControllerNumber);
        Assert.IsType<NoteOnEvent>(events[4]);
        Assert.IsType<NoteOffEvent>(events[5]);
    }

    [Fact]
    public void PreservesOriginalOrderForSamePriority()
    {
        // Arrange - Multiple NoteOns at same tick should preserve relative order
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 2, 0, 62, 100), // Third
            CreateNoteOn(100, 0, 0, 0, 60, 100), // First
            CreateNoteOn(100, 0, 1, 0, 61, 100)  // Second
        };

        // Act
        EventSorter.Sort(events);

        // Assert - Should be ordered by OriginalIndex
        Assert.Equal(60, ((NoteOnEvent)events[0]).NoteNumber);
        Assert.Equal(61, ((NoteOnEvent)events[1]).NoteNumber);
        Assert.Equal(62, ((NoteOnEvent)events[2]).NoteNumber);
    }

    [Fact]
    public void IsCorrectlyOrderedReturnsTrueForSortedEvents()
    {
        // Arrange
        var events = new List<MidiEventBase>
        {
            CreateBankSelectMsb(100, 0, 0, 0, 1),
            CreateProgramChange(100, 0, 1, 0, 5),
            CreateNoteOn(100, 0, 2, 0, 60, 100)
        };

        // Act & Assert
        Assert.True(EventSorter.IsCorrectlyOrdered(events));
    }

    [Fact]
    public void IsCorrectlyOrderedReturnsFalseForUnsortedEvents()
    {
        // Arrange - NoteOn before ProgramChange violates ordering
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 0, 0, 0, 60, 100),
            CreateProgramChange(100, 0, 1, 0, 5)
        };

        // Act & Assert
        Assert.False(EventSorter.IsCorrectlyOrdered(events));
    }

    [Fact]
    public void MultiTrackEventsAtSameTick()
    {
        // Arrange - Events from different tracks at same tick
        var events = new List<MidiEventBase>
        {
            CreateNoteOn(100, 1, 0, 0, 60, 100),
            CreateProgramChange(100, 0, 0, 0, 5)
        };

        // Act
        EventSorter.Sort(events);

        // Assert - Program Change should come before NoteOn regardless of track
        Assert.IsType<ProgramChangeEvent>(events[0]);
        Assert.IsType<NoteOnEvent>(events[1]);
    }

    [Fact]
    public void StableSortPreservesInputOrderWhenComparerReturnsEqual()
    {
        // Arrange - same tick, same priority, same track and original index => comparer returns 0
        var first = CreateMetaEvent(100, 0, 0, 0x01);
        var second = CreateMetaEvent(100, 0, 0, 0x02);
        var third = CreateMetaEvent(100, 0, 0, 0x03);
        var events = new List<MidiEventBase> { second, third, first };

        // Act
        EventSorter.Sort(events);

        // Assert - stable sort preserves original input order for equal comparisons
        Assert.Same(second, events[0]);
        Assert.Same(third, events[1]);
        Assert.Same(first, events[2]);
    }
}
