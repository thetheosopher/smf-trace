using SMFTrace.Core.Models;
using SMFTrace.Core.Configuration;
using SMFTrace.Core.Sequencer;

namespace SMFTrace.Core.Tests;

/// <summary>
/// Mock MIDI output for testing sequencer behavior.
/// </summary>
public class MockSequencerOutput : ISequencerOutput
{
    private readonly List<(byte Status, byte Data1, byte Data2)> _messages = [];
    private readonly List<byte[]> _sysExMessages = [];
    private int _allNotesOffCount;

    public IReadOnlyList<(byte Status, byte Data1, byte Data2)> Messages => _messages;
    public IReadOnlyList<byte[]> SysExMessages => _sysExMessages;
    public int AllNotesOffCount => _allNotesOffCount;

    public void SendShortMessage(byte status, byte data1, byte data2)
    {
        _messages.Add((status, data1, data2));
    }

    public void SendSysEx(ReadOnlySpan<byte> payload)
    {
        _sysExMessages.Add(payload.ToArray());
    }

    public void AllNotesOff()
    {
        Interlocked.Increment(ref _allNotesOffCount);
    }

    public void Clear()
    {
        _messages.Clear();
        _sysExMessages.Clear();
        _allNotesOffCount = 0;
    }
}

public class SequencerEngineTests
{
    private static MidiFileData CreateTestFileData()
    {
        var events = new List<MidiEventBase>
        {
            new NoteOnEvent
            {
                AbsoluteTick = 0,
                Time = TimeSpan.Zero,
                TrackIndex = 0,
                OriginalIndex = 0,
                RawBytes = [],
                Channel = 0,
                NoteNumber = 60,
                Velocity = 100
            },
            new NoteOffEvent
            {
                AbsoluteTick = 480,
                Time = TimeSpan.FromMilliseconds(500),
                TrackIndex = 0,
                OriginalIndex = 1,
                RawBytes = [],
                Channel = 0,
                NoteNumber = 60,
                Velocity = 64
            }
        };

        return new MidiFileData
        {
            FilePath = "test.mid",
            Format = 1,
            Events = events,
            Tracks = [],
            Duration = TimeSpan.FromSeconds(1),
            TotalTicks = 960,
            TicksPerQuarterNote = 480,
            TempoMap = Melanchall.DryWetMidi.Interaction.TempoMap.Default
        };
    }

    private static MidiFileData CreateSysExFileData()
    {
        var events = new List<MidiEventBase>
        {
            new SysExEvent
            {
                AbsoluteTick = 0,
                Time = TimeSpan.Zero,
                TrackIndex = 0,
                OriginalIndex = 0,
                RawBytes = [0xF0, 0x7D, 0x01, 0xF7],
                Data = [0xF0, 0x7D, 0x01, 0xF7]
            }
        };

        return new MidiFileData
        {
            FilePath = "sysex.mid",
            Format = 1,
            Events = events,
            Tracks = [],
            Duration = TimeSpan.FromMilliseconds(50),
            TotalTicks = 1,
            TicksPerQuarterNote = 480,
            TempoMap = Melanchall.DryWetMidi.Interaction.TempoMap.Default
        };
    }

    [Fact]
    public void InitialStateIsStopped()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);

        // Assert
        Assert.Equal(PlaybackState.Stopped, engine.State);
        Assert.Equal(0, engine.CurrentTick);
        Assert.Equal(TimeSpan.Zero, engine.CurrentTime);
    }

    [Fact]
    public void PlayThrowsWhenNoOutputSet()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => engine.Play());
    }

    [Fact]
    public void PlayChangesStateToPlaying()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(50); // Allow state to propagate

        // Assert
        Assert.Equal(PlaybackState.Playing, engine.State);

        // Cleanup
        engine.Stop();
    }

    [Fact]
    public void PauseSendsAllNotesOff()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(50);
        engine.Pause();

        // Assert
        Assert.Equal(PlaybackState.Paused, engine.State);
        Assert.True(output.AllNotesOffCount >= 1, "Pause should send AllNotesOff");
    }

    [Fact]
    public void StopSendsAllNotesOffAndResetsPosition()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(50);
        engine.Stop();

        // Assert
        Assert.Equal(PlaybackState.Stopped, engine.State);
        Assert.True(output.AllNotesOffCount >= 1, "Stop should send AllNotesOff");
        Assert.Equal(0, engine.CurrentTick);
        Assert.Equal(TimeSpan.Zero, engine.CurrentTime);
    }

    [Fact]
    public void BeginScrubChangesStateToScrubbing()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.BeginScrub();

        // Assert
        Assert.Equal(PlaybackState.Scrubbing, engine.State);
    }

    [Fact]
    public void ScrubIsSilentNoOutputDuringDrag()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.BeginScrub();
        output.Clear(); // Clear any previous messages
        engine.Scrub(TimeSpan.FromMilliseconds(250));
        engine.Scrub(TimeSpan.FromMilliseconds(500));

        // Assert - no MIDI output during scrub
        Assert.Empty(output.Messages);
        Assert.Equal(0, output.AllNotesOffCount);
    }

    [Fact]
    public void EndScrubSendsAllNotesOff()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.BeginScrub();
        engine.Scrub(TimeSpan.FromMilliseconds(250));
        output.Clear();
        engine.EndScrub();

        // Assert
        Assert.Equal(1, output.AllNotesOffCount);
    }

    [Fact]
    public void SeekToUpdatePositionCorrectly()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.SeekTo(TimeSpan.FromMilliseconds(500));

        // Assert
        Assert.True(engine.CurrentTime >= TimeSpan.FromMilliseconds(490)); // Allow small tolerance
        Assert.True(engine.CurrentTick > 0);
    }

    [Fact]
    public void SeekToResumesPlaybackIfWasPlaying()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(50);
        engine.SeekTo(TimeSpan.FromMilliseconds(100));
        Thread.Sleep(50);

        // Assert
        Assert.Equal(PlaybackState.Playing, engine.State);

        // Cleanup
        engine.Stop();
    }

    [Fact]
    public void DurationAndTotalTicksReflectFileData()
    {
        // Arrange
        var fileData = CreateTestFileData();
        using var engine = new SequencerEngine(fileData);

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(1), engine.Duration);
        Assert.Equal(960, engine.TotalTicks);
    }

    [Fact]
    public void SysExIsSentByDefault()
    {
        // Arrange
        var fileData = CreateSysExFileData();
        using var engine = new SequencerEngine(fileData);
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(75);

        // Assert
        Assert.NotEmpty(output.SysExMessages);
    }

    [Fact]
    public void DisableSysExOutputSuppressesTransmission()
    {
        // Arrange
        var fileData = CreateSysExFileData();
        using var engine = new SequencerEngine(fileData, new PlaybackOptions { DisableSysExOutput = true });
        var output = new MockSequencerOutput();
        engine.SetOutput(output);

        // Act
        engine.Play();
        Thread.Sleep(75);

        // Assert
        Assert.Empty(output.SysExMessages);
    }
}
