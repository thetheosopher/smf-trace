using SMFTrace.Core;
using SMFTrace.Core.Sequencer;
using Xunit;

namespace SMFTrace.Core.Tests;

public class MidiFileLoaderEdgeCaseTests
{
    [Fact]
    public void LoadNullPathThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MidiFileLoader.Load((string)null!));
    }

    [Fact]
    public void LoadNonExistentFileThrowsMidiFileException()
    {
        // Arrange
        var fakePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.mid");

        // Act & Assert
        var ex = Assert.Throws<MidiFileException>(() => MidiFileLoader.Load(fakePath));
        Assert.Equal(MidiFileErrorType.FileNotFound, ex.ErrorType);
        Assert.Equal(fakePath, ex.FilePath);
    }

    [Fact]
    public void LoadEmptyFileThrowsMidiFileException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, []);

            // Act & Assert
            var ex = Assert.Throws<MidiFileException>(() => MidiFileLoader.Load(tempFile));
            Assert.Equal(MidiFileErrorType.EmptyFile, ex.ErrorType);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadInvalidDataThrowsMidiFileException()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllBytes(tempFile, "Not a MIDI file"u8.ToArray());

            // Act & Assert
            var ex = Assert.Throws<MidiFileException>(() => MidiFileLoader.Load(tempFile));
            Assert.Equal(MidiFileErrorType.InvalidFormat, ex.ErrorType);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void LoadNullStreamThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => MidiFileLoader.Load((Stream)null!));
    }

    [Fact]
    public void LoadInvalidStreamThrowsMidiFileException()
    {
        // Arrange
        using var stream = new MemoryStream("Not a MIDI file"u8.ToArray());

        // Act & Assert
        var ex = Assert.Throws<MidiFileException>(() => MidiFileLoader.Load(stream, "test.mid"));
        Assert.Equal(MidiFileErrorType.InvalidFormat, ex.ErrorType);
    }

    [Fact]
    public void LoadPreservesUnknownMetaEvents()
    {
        // Arrange
        using var stream = new MemoryStream(
        [
            0x4D, 0x54, 0x68, 0x64,
            0x00, 0x00, 0x00, 0x06,
            0x00, 0x00,
            0x00, 0x01,
            0x01, 0xE0,
            0x4D, 0x54, 0x72, 0x6B,
            0x00, 0x00, 0x00, 0x0B,
            0x00, 0xFF, 0x7E, 0x03, 0x01, 0x02, 0x03,
            0x00, 0xFF, 0x2F, 0x00
        ]);

        // Act
        var data = MidiFileLoader.Load(stream, "unknown-meta.mid");

        // Assert
        var meta = Assert.Single(data.Events.OfType<SMFTrace.Core.Models.MetaEvent>(), evt => evt.MetaType == 0x7E);
        Assert.Equal([0x01, 0x02, 0x03], meta.Data);
        Assert.NotEmpty(meta.RawBytes);
    }

    [Fact]
    public void LoadPreservesEscapeSysExEvents()
    {
        // Arrange
        using var stream = new MemoryStream(
        [
            0x4D, 0x54, 0x68, 0x64,
            0x00, 0x00, 0x00, 0x06,
            0x00, 0x00,
            0x00, 0x01,
            0x01, 0xE0,
            0x4D, 0x54, 0x72, 0x6B,
            0x00, 0x00, 0x00, 0x0A,
            0x00, 0xF7, 0x03, 0x7D, 0x10, 0xF7,
            0x00, 0xFF, 0x2F, 0x00
        ]);

        // Act
        var data = MidiFileLoader.Load(stream, "escape-sysex.mid");

        // Assert
        var sysex = Assert.Single(data.Events.OfType<SMFTrace.Core.Models.SysExEvent>());
        Assert.NotEmpty(sysex.Data);
        Assert.NotEmpty(sysex.RawBytes);
    }
}
