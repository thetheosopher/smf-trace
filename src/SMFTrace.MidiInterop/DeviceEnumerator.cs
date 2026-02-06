using System.Runtime.InteropServices;

namespace SMFTrace.MidiInterop;

/// <summary>
/// Enumerates available MIDI devices and monitors for hot-plug events.
/// </summary>
public sealed class DeviceEnumerator : IDisposable
{
    private readonly System.Timers.Timer? _pollTimer;
    private List<MidiDeviceInfo> _cachedDevices = [];
    private bool _disposed;

    /// <summary>
    /// Raised when the device list changes (device connected or disconnected).
    /// </summary>
    public event EventHandler<DeviceListChangedEventArgs>? DeviceListChanged;

    /// <summary>
    /// Creates a new device enumerator with optional polling for hot-plug detection.
    /// </summary>
    /// <param name="enablePolling">Whether to poll for device changes.</param>
    /// <param name="pollIntervalMs">Polling interval in milliseconds.</param>
    public DeviceEnumerator(bool enablePolling = true, int pollIntervalMs = 1000)
    {
        _cachedDevices = [.. GetOutputDevices()];

        if (enablePolling)
        {
            _pollTimer = new System.Timers.Timer(pollIntervalMs);
            _pollTimer.Elapsed += OnPollTimerElapsed;
            _pollTimer.AutoReset = true;
            _pollTimer.Start();
        }
    }

    /// <summary>
    /// Gets the currently cached list of output devices.
    /// </summary>
    public IReadOnlyList<MidiDeviceInfo> OutputDevices => _cachedDevices;

    /// <summary>
    /// Gets all available MIDI output devices.
    /// </summary>
    /// <returns>List of device information.</returns>
    public static List<MidiDeviceInfo> GetOutputDevices()
    {
        var devices = new List<MidiDeviceInfo>();
        var numDevices = NativeMethods.midiOutGetNumDevs();

        for (uint i = 0; i < numDevices; i++)
        {
            var result = NativeMethods.midiOutGetDevCaps(
                i,
                out var caps,
                (uint)Marshal.SizeOf<NativeMethods.MidiOutCaps>());

            if (result == NativeMethods.MmsyserrNoerror)
            {
                devices.Add(new MidiDeviceInfo(i, caps.Name, caps.DriverVersion));
            }
        }

        return devices;
    }

    /// <summary>
    /// Forces a refresh of the device list.
    /// </summary>
    public void Refresh()
    {
        var newDevices = GetOutputDevices();
        UpdateDeviceList(newDevices);
    }

    private void OnPollTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        try
        {
            var newDevices = GetOutputDevices();
            UpdateDeviceList(newDevices);
        }
        catch
        {
            // Ignore polling errors
        }
    }

    private void UpdateDeviceList(List<MidiDeviceInfo> newDevices)
    {
        var oldNames = _cachedDevices.Select(d => d.Name).ToHashSet();
        var newNames = newDevices.Select(d => d.Name).ToHashSet();

        if (!oldNames.SetEquals(newNames))
        {
            var added = newDevices.Where(d => !oldNames.Contains(d.Name)).ToList();
            var removed = _cachedDevices.Where(d => !newNames.Contains(d.Name)).ToList();

            _cachedDevices = newDevices;

            DeviceListChanged?.Invoke(this, new DeviceListChangedEventArgs(added, removed));
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _pollTimer?.Stop();
        _pollTimer?.Dispose();
    }
}

/// <summary>
/// Event arguments for device list changes.
/// </summary>
public sealed class DeviceListChangedEventArgs : EventArgs
{
    /// <summary>
    /// Devices that were added.
    /// </summary>
    public IReadOnlyList<MidiDeviceInfo> Added { get; }

    /// <summary>
    /// Devices that were removed.
    /// </summary>
    public IReadOnlyList<MidiDeviceInfo> Removed { get; }

    public DeviceListChangedEventArgs(IReadOnlyList<MidiDeviceInfo> added, IReadOnlyList<MidiDeviceInfo> removed)
    {
        Added = added;
        Removed = removed;
    }
}
