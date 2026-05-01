using System.IO;
using SMFTrace.Wpf;
using Xunit;

namespace SMFTrace.Wpf.Tests;

public sealed class CommandLineMidiFileTests : IDisposable
{
    private readonly string _tempDir;

    public CommandLineMidiFileTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SMFTraceCommandLineTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void GetCommandLineMidiPathsReturnsAllExistingMidiFilesInOrder()
    {
        // Arrange
        var first = CreateFile("first.mid");
        var second = CreateFile("second.midi");
        var ignoredText = CreateFile("notes.txt");
        var missing = Path.Combine(_tempDir, "missing.mid");

        // Act
        var paths = App.GetCommandLineMidiPaths([
            "",
            "  " + first + "  ",
            ignoredText,
            missing,
            second
        ]);

        // Assert
        Assert.Equal([first, second], paths);
    }

    private string CreateFile(string fileName)
    {
        var path = Path.Combine(_tempDir, fileName);
        File.WriteAllBytes(path, []);
        return path;
    }
}
