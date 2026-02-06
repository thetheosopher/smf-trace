using System.IO;
using System.Text.Json;
using SMFTrace.Wpf.Settings;
using Xunit;

namespace SMFTrace.Wpf.Tests;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsPath;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SMFTraceTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _settingsPath = Path.Combine(_tempDir, "settings.json");
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
    public void LoadWithNoFileCreatesDefaultSettings()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);

        // Act
        service.Load();

        // Assert
        Assert.Equal(1280, service.Current.WindowWidth);
        Assert.Equal(800, service.Current.WindowHeight);
        Assert.Equal(60, service.Current.RenderFps);
        Assert.True(service.Current.ShowTempo);
    }

    [Fact]
    public void SaveAndLoadRoundTripsSettings()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);
        service.Current.WindowWidth = 1920;
        service.Current.WindowHeight = 1080;
        service.Current.LastDeviceName = "Test Device";
        service.Current.DisableSysExOutput = true;
        service.Current.RenderFps = 30;

        // Act
        service.Save();

        var service2 = new SettingsService(_settingsPath);
        service2.Load();

        // Assert
        Assert.Equal(1920, service2.Current.WindowWidth);
        Assert.Equal(1080, service2.Current.WindowHeight);
        Assert.Equal("Test Device", service2.Current.LastDeviceName);
        Assert.True(service2.Current.DisableSysExOutput);
        Assert.Equal(30, service2.Current.RenderFps);
    }

    [Fact]
    public void LoadCorruptedFileUsesDefaults()
    {
        // Arrange
        File.WriteAllText(_settingsPath, "{ invalid json }}}");
        var service = new SettingsService(_settingsPath);

        // Act
        service.Load();

        // Assert - should use defaults, not throw
        Assert.Equal(1280, service.Current.WindowWidth);
    }

    [Fact]
    public void ResetResetsToDefaults()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);
        service.Current.WindowWidth = 9999;
        service.Current.RenderFps = 30;

        // Act
        service.Reset();

        // Assert
        Assert.Equal(1280, service.Current.WindowWidth);
        Assert.Equal(60, service.Current.RenderFps);
    }

    [Fact]
    public void SaveCreatesMissingDirectory()
    {
        // Arrange
        var nestedPath = Path.Combine(_tempDir, "nested", "deep", "settings.json");
        var service = new SettingsService(nestedPath);
        service.Current.WindowWidth = 500;

        // Act
        service.Save();

        // Assert
        Assert.True(File.Exists(nestedPath));
    }

    [Fact]
    public void DiagnosticsFiltersArePersisted()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);
        service.Current.DiagShowNotes = false;
        service.Current.DiagShowControlChanges = false;
        service.Current.DiagMetaOnlyMode = true;
        service.Current.DiagAutoScrollEnabled = false;

        // Act
        service.Save();

        var service2 = new SettingsService(_settingsPath);
        service2.Load();

        // Assert
        Assert.False(service2.Current.DiagShowNotes);
        Assert.False(service2.Current.DiagShowControlChanges);
        Assert.True(service2.Current.DiagMetaOnlyMode);
        Assert.False(service2.Current.DiagAutoScrollEnabled);
    }

    [Fact]
    public void WindowPositionIsPersisted()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);
        service.Current.WindowLeft = 200;
        service.Current.WindowTop = 150;
        service.Current.IsMaximized = true;

        // Act
        service.Save();

        var service2 = new SettingsService(_settingsPath);
        service2.Load();

        // Assert
        Assert.Equal(200, service2.Current.WindowLeft);
        Assert.Equal(150, service2.Current.WindowTop);
        Assert.True(service2.Current.IsMaximized);
    }

    [Fact]
    public void JsonIsHumanReadableFormat()
    {
        // Arrange
        var service = new SettingsService(_settingsPath);
        service.Current.LastDeviceName = "My Device";

        // Act
        service.Save();
        var json = File.ReadAllText(_settingsPath);

        // Assert - should be indented (contains newlines) and use camelCase
        Assert.Contains("\n", json);
        Assert.Contains("lastDeviceName", json);
        Assert.Contains("My Device", json);
    }
}
