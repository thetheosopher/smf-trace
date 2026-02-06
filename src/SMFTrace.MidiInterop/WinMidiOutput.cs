using System.Runtime.InteropServices;

namespace SMFTrace.MidiInterop;

/// <summary>
/// Windows Multimedia MIDI output device implementation.
/// </summary>
public sealed class WinMidiOutput : IMidiOutput
{
    private readonly uint _deviceId;
    private nint _handle;
    private bool _disposed;

    /// <inheritdoc />
    public string DisplayName { get; }

    private WinMidiOutput(uint deviceId, string displayName, nint handle)
    {
        _deviceId = deviceId;
        DisplayName = displayName;
        _handle = handle;
    }

    /// <summary>
    /// Opens a MIDI output device.
    /// </summary>
    /// <param name="deviceId">The device ID to open.</param>
    /// <returns>An open MIDI output device.</returns>
    public static WinMidiOutput Open(uint deviceId)
    {
        var result = NativeMethods.midiOutGetDevCaps(
            deviceId,
            out var caps,
            (uint)Marshal.SizeOf<NativeMethods.MidiOutCaps>());

        if (result != NativeMethods.MmsyserrNoerror)
        {
            throw new MidiException("Failed to get device capabilities", result);
        }

        result = NativeMethods.midiOutOpen(out var handle, deviceId, 0, 0, 0);
        if (result != NativeMethods.MmsyserrNoerror)
        {
            throw new MidiException($"Failed to open MIDI device '{caps.Name}'", result);
        }

        return new WinMidiOutput(deviceId, caps.Name, handle);
    }

    /// <summary>
    /// Opens a MIDI output device by name.
    /// </summary>
    /// <param name="deviceName">The device name to match.</param>
    /// <returns>An open MIDI output device.</returns>
    public static WinMidiOutput Open(string deviceName)
    {
        var devices = DeviceEnumerator.GetOutputDevices();
        var device = devices.FirstOrDefault(d =>
            d.Name.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

        if (device.Name is null)
        {
            throw new MidiException($"MIDI output device '{deviceName}' not found");
        }

        return Open(device.DeviceId);
    }

    /// <inheritdoc />
    public void SendShortMessage(byte status, byte data1, byte data2)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Pack the message: status in low byte, data1 in next, data2 in next
        uint msg = (uint)(status | (data1 << 8) | (data2 << 16));
        var result = NativeMethods.midiOutShortMsg(_handle, msg);

        if (result != NativeMethods.MmsyserrNoerror)
        {
            throw new MidiException("Failed to send MIDI message", result);
        }
    }

    /// <inheritdoc />
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
    public void SendSysEx(ReadOnlySpan<byte> payload)
#pragma warning restore CA1711
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (payload.IsEmpty)
        {
            return;
        }

        // Allocate unmanaged memory for the SysEx data
        var dataPtr = Marshal.AllocHGlobal(payload.Length);
        try
        {
            Marshal.Copy(payload.ToArray(), 0, dataPtr, payload.Length);

            var header = new NativeMethods.MidiHdr
            {
                Data = dataPtr,
                BufferLength = (uint)payload.Length,
                BytesRecorded = (uint)payload.Length,
                Flags = 0
            };

            var headerSize = (uint)Marshal.SizeOf<NativeMethods.MidiHdr>();

            // Prepare the header
            var result = NativeMethods.midiOutPrepareHeader(_handle, ref header, headerSize);
            if (result != NativeMethods.MmsyserrNoerror)
            {
                throw new MidiException("Failed to prepare SysEx header", result);
            }

            try
            {
                // Send the SysEx message
                result = NativeMethods.midiOutLongMsg(_handle, ref header, headerSize);
                if (result != NativeMethods.MmsyserrNoerror)
                {
                    throw new MidiException("Failed to send SysEx message", result);
                }

                // Wait for the message to be sent (poll the done flag)
                var timeout = DateTime.UtcNow.AddSeconds(5);
                while ((header.Flags & NativeMethods.MhdrDone) == 0)
                {
                    if (DateTime.UtcNow > timeout)
                    {
                        throw new MidiException("Timeout waiting for SysEx to complete");
                    }

                    Thread.Sleep(1);
                }
            }
            finally
            {
                // Unprepare the header
                _ = NativeMethods.midiOutUnprepareHeader(_handle, ref header, headerSize);
            }
        }
        finally
        {
            Marshal.FreeHGlobal(dataPtr);
        }
    }

    /// <inheritdoc />
    public void AllNotesOff()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Send All Notes Off (CC 123) on all 16 channels
        for (byte channel = 0; channel < 16; channel++)
        {
            var status = (byte)(0xB0 | channel); // Control Change
            SendShortMessage(status, 123, 0);    // CC 123 = All Notes Off
        }
    }

    /// <inheritdoc />
    public void ResetControllers()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Send Reset All Controllers (CC 121) on all 16 channels
        for (byte channel = 0; channel < 16; channel++)
        {
            var status = (byte)(0xB0 | channel); // Control Change
            SendShortMessage(status, 121, 0);    // CC 121 = Reset All Controllers
        }
    }

    /// <summary>
    /// Resets the MIDI output device, stopping all notes.
    /// </summary>
    public void Reset()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _ = NativeMethods.midiOutReset(_handle);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_handle != 0)
        {
            // Reset to stop all notes
            _ = NativeMethods.midiOutReset(_handle);
            _ = NativeMethods.midiOutClose(_handle);
            _handle = 0;
        }
    }
}
