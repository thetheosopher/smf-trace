namespace SMFTrace.MidiInterop;

/// <summary>
/// Information about a MIDI output device.
/// </summary>
/// <param name="DeviceId">The device ID used for opening.</param>
/// <param name="Name">Display name of the device.</param>
/// <param name="DriverVersion">Driver version number.</param>
public readonly record struct MidiDeviceInfo(uint DeviceId, string Name, uint DriverVersion);
