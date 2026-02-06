using System.Runtime.InteropServices;

namespace SMFTrace.MidiInterop;

/// <summary>
/// P/Invoke declarations for Windows Multimedia MIDI functions.
/// </summary>
internal static class NativeMethods
{
    private const string WinMM = "winmm.dll";

    // MIDI output device capabilities
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MidiOutCaps
    {
        public ushort Mid;
        public ushort Pid;
        public uint DriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string Name;
        public ushort Technology;
        public ushort Voices;
        public ushort Notes;
        public ushort ChannelMask;
        public uint Support;
    }

    // MIDI header for SysEx
    [StructLayout(LayoutKind.Sequential)]
    public struct MidiHdr
    {
        public nint Data;
        public uint BufferLength;
        public uint BytesRecorded;
        public nint User;
        public uint Flags;
        public nint Next;
        public nint Reserved;
        public uint Offset;
        public nint Reserved2_0;
        public nint Reserved2_1;
        public nint Reserved2_2;
        public nint Reserved2_3;
    }

    // MIDI header flags
    public const uint MhdrDone = 0x00000001;
    public const uint MhdrPrepared = 0x00000002;

    // Error codes
    public const uint MmsyserrNoerror = 0;
    public const uint MmsyserrBaddeviceid = 2;
    public const uint MmsyserrAllocated = 4;
    public const uint MmsyserrInvalidhandle = 5;

    [DllImport(WinMM)]
    public static extern uint midiOutGetNumDevs();

    [DllImport(WinMM, EntryPoint = "midiOutGetDevCapsW", CharSet = CharSet.Unicode)]
    public static extern uint midiOutGetDevCaps(
        uint deviceId,
        out MidiOutCaps caps,
        uint capsSize);

    [DllImport(WinMM)]
    public static extern uint midiOutOpen(
        out nint handle,
        uint deviceId,
        nint callback,
        nint instance,
        uint flags);

    [DllImport(WinMM)]
    public static extern uint midiOutClose(nint handle);

    [DllImport(WinMM)]
    public static extern uint midiOutShortMsg(nint handle, uint msg);

    [DllImport(WinMM)]
    public static extern uint midiOutPrepareHeader(nint handle, ref MidiHdr header, uint headerSize);

    [DllImport(WinMM)]
    public static extern uint midiOutUnprepareHeader(nint handle, ref MidiHdr header, uint headerSize);

    [DllImport(WinMM)]
    public static extern uint midiOutLongMsg(nint handle, ref MidiHdr header, uint headerSize);

    [DllImport(WinMM)]
    public static extern uint midiOutReset(nint handle);
}
