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
}
