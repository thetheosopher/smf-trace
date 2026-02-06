namespace SMFTrace.Core.Models;

/// <summary>
/// System Exclusive event.
/// </summary>
public sealed record SysExEvent : MidiEventBase
{
    /// <summary>
    /// The complete SysEx data including F0 start and F7 end bytes.
    /// </summary>
    public required byte[] Data { get; init; }

    /// <summary>
    /// Manufacturer ID (1 or 3 bytes after F0).
    /// </summary>
    public byte[] ManufacturerId => ExtractManufacturerId();

    private byte[] ExtractManufacturerId()
    {
        if (Data.Length < 2)
        {
            return [];
        }

        // Skip F0, check if extended ID (00 xx xx)
        if (Data[1] == 0x00 && Data.Length >= 4)
        {
            return [Data[1], Data[2], Data[3]];
        }

        return [Data[1]];
    }
}
