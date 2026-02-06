using SMFTrace.Core.Sequencer;

namespace SMFTrace.MidiInterop;

/// <summary>
/// Adapts an IMidiOutput to ISequencerOutput for use with the sequencer engine.
/// </summary>
public sealed class MidiOutputAdapter : ISequencerOutput
{
    private readonly IMidiOutput _output;

    public MidiOutputAdapter(IMidiOutput output)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
    }

    public void SendShortMessage(byte status, byte data1, byte data2)
    {
        _output.SendShortMessage(status, data1, data2);
    }

    public void SendSysEx(ReadOnlySpan<byte> payload)
    {
        _output.SendSysEx(payload);
    }

    public void AllNotesOff()
    {
        _output.AllNotesOff();
    }
}
