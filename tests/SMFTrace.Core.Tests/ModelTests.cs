using SMFTrace.Core.Models;

namespace SMFTrace.Core.Tests;

public class NoteEventTests
{
    [Fact]
    public void ControlChangeEventIdentifiesBankSelectMsb()
    {
        var cc = new ControlChangeEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = 0,
            ControllerNumber = 0, // CC0 = Bank Select MSB
            Value = 1
        };

        Assert.True(cc.IsBankSelectMsb);
        Assert.False(cc.IsBankSelectLsb);
        Assert.True(cc.IsBankSelect);
    }

    [Fact]
    public void ControlChangeEventIdentifiesBankSelectLsb()
    {
        var cc = new ControlChangeEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = 0,
            ControllerNumber = 32, // CC32 = Bank Select LSB
            Value = 2
        };

        Assert.False(cc.IsBankSelectMsb);
        Assert.True(cc.IsBankSelectLsb);
        Assert.True(cc.IsBankSelect);
    }

    [Fact]
    public void ControlChangeEventIdentifiesNonBankSelect()
    {
        var cc = new ControlChangeEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            Channel = 0,
            ControllerNumber = 7, // Volume
            Value = 100
        };

        Assert.False(cc.IsBankSelectMsb);
        Assert.False(cc.IsBankSelectLsb);
        Assert.False(cc.IsBankSelect);
    }

    [Fact]
    public void MetaEventIdentifiesSetTempo()
    {
        var meta = new MetaEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            MetaType = 0x51, // Set Tempo
            Data = [0x07, 0xA1, 0x20] // 500000 µs = 120 BPM
        };

        Assert.True(meta.IsSetTempo);
        Assert.False(meta.IsTimeSignature);
        Assert.Equal(500000, meta.MicrosecondsPerQuarterNote);
        Assert.Equal(120.0, meta.Bpm, 0.001);
    }

    [Fact]
    public void MetaEventIdentifiesTimeSignature()
    {
        var meta = new MetaEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            MetaType = 0x58, // Time Signature
            Data = [4, 2, 24, 8] // 4/4 time
        };

        Assert.False(meta.IsSetTempo);
        Assert.True(meta.IsTimeSignature);
    }

    [Fact]
    public void MetaEventReturnsTextContent()
    {
        var meta = new MetaEvent
        {
            AbsoluteTick = 0,
            TrackIndex = 0,
            OriginalIndex = 0,
            RawBytes = [],
            MetaType = 0x03, // Track Name
            Data = "Piano"u8.ToArray()
        };

        Assert.True(meta.IsTrackName);
        Assert.Equal("Piano", meta.TextContent);
    }

    [Fact]
    public void ChannelStateDefaultsToNoProgram()
    {
        var state = new ChannelState();

        Assert.False(state.HasProgramChange);
        Assert.Equal("(default)", state.InstrumentDisplayName);
    }

    [Fact]
    public void ChannelStateShowsProgramWhenSet()
    {
        var state = new ChannelState
        {
            HasProgramChange = true,
            Program = 42
        };

        Assert.True(state.HasProgramChange);
        Assert.Equal("Cello", state.InstrumentDisplayName); // Program 42 = Cello in GM
    }

    [Fact]
    public void LaneIdEquality()
    {
        var lane1 = new LaneId(0, 5);
        var lane2 = new LaneId(0, 5);
        var lane3 = new LaneId(1, 5);
        var lane4 = new LaneId(0, 6);

        Assert.Equal(lane1, lane2);
        Assert.NotEqual(lane1, lane3);
        Assert.NotEqual(lane1, lane4);
    }

    [Fact]
    public void TimePositionRecordWorks()
    {
        var pos1 = new TimePosition(100, TimeSpan.FromSeconds(1.5));
        var pos2 = new TimePosition(100, TimeSpan.FromSeconds(1.5));
        var pos3 = new TimePosition(200, TimeSpan.FromSeconds(1.5));

        Assert.Equal(pos1, pos2);
        Assert.NotEqual(pos1, pos3);
        Assert.Equal(100, pos1.Ticks);
        Assert.Equal(TimeSpan.FromSeconds(1.5), pos1.Time);
    }
}
