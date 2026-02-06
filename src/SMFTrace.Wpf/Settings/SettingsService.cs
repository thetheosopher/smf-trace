using System.IO;
using System.Text.Json;

namespace SMFTrace.Wpf.Settings;

/// <summary>
/// Service for loading and saving application settings to JSON.
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _settingsPath;
    private AppSettings _current;

    /// <summary>
    /// Creates a new settings service using the default AppData path.
    /// </summary>
    public SettingsService()
        : this(GetDefaultSettingsPath())
    {
    }

    /// <summary>
    /// Creates a new settings service with a custom path (for testing).
    /// </summary>
    public SettingsService(string settingsPath)
    {
        _settingsPath = settingsPath;
        _current = new AppSettings();
    }

    /// <summary>
    /// Gets the current settings.
    /// </summary>
    public AppSettings Current => _current;

    /// <summary>
    /// Loads settings from disk. If the file doesn't exist, uses defaults.
    /// </summary>
    public void Load()
    {
        if (!File.Exists(_settingsPath))
        {
            _current = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_settingsPath);
            _current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupted file, use defaults
            _current = new AppSettings();
        }
    }

    /// <summary>
    /// Saves current settings to disk.
    /// </summary>
    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_current, JsonOptions);
            File.WriteAllText(_settingsPath, json);
        }
        catch (IOException)
        {
            // Ignore save failures (e.g., read-only location)
        }
    }

    /// <summary>
    /// Resets settings to defaults.
    /// </summary>
    public void Reset()
    {
        _current = new AppSettings();
    }

    private static string GetDefaultSettingsPath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "SMFTrace", "settings.json");
    }
}
