# SMF Trace — Companion Technical Design (Final)

**App Name:** SMF Trace

**Tagline:** See every event. Trust every tick.

This companion document is the technical counterpart to the SMF Trace final spec and focuses on implementation details: interfaces, sequencing algorithms, ordering rules, seek rebuild, rendering, diagnostics, and safety gates.

---

## 1) Recommended MIDI Library

### Melanchall.DryWetMIDI
Use DryWetMIDI for:
- robust SMF parsing
- tempo map handling and conversions
- extracting events and notes reliably

> Keep your own sequencer loop for exact semantics (pause sends all-notes-off, silent scrubbing, deterministic seek rebuild).

---

## 2) SysEx Safety Gate (Final)

Add user option:
- **Disable SysEx output** (default OFF)

Rules:
- OFF: SysEx transmitted to output.
- ON: SysEx suppressed (not transmitted), but still appears in Diagnostics if SysEx visibility is enabled.

Implementation hook:
- Gate SysEx sends in a single dispatch method before calling `IMidiOutput.SendSysEx(...)`.

---

## 3) Core Interfaces (same as v2 companion)

```csharp
public interface IMidiOutput : IDisposable
{
    string DisplayName { get; }
    void SendShortMessage(byte status, byte data1, byte data2);
    void SendSysEx(ReadOnlySpan<byte> payload);
    void AllNotesOff();
    void ResetControllers();
}

public sealed class PlaybackOptions
{
    public bool DisableSysExOutput { get; set; }
}
```

---

## 4) Event Ordering

Same-tick priority must enforce:
1) bank select (CC0/CC32)
2) program change
3) other CC affecting onset
4) note-on
5) note-off

---

## 5) Seek Rebuild

- Maintain per-channel bank/program state.
- Build checkpoints every N ticks (or per measure) for fast seek.
- On scrub release:
  1) AllNotesOff
  2) Restore snapshot
  3) Replay state events to target tick
  4) Emit bank/program state to device
  5) Resume scheduling from correct event index

---

## 6) Rendering

- Custom render surface using WPF drawing APIs.
- 60/30 FPS timer.
- Pre-index note spans per lane.
- Visible window computed from playhead at 33% of view width.

