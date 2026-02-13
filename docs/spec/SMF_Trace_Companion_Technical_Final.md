# SMF Trace — Companion Technical Design (Final)

**App Name:** SMF Trace

**Tagline:** See every event. Trust every tick.

This companion document is the technical counterpart to the SMF Trace final spec and focuses on implementation details: interfaces, sequencing algorithms, ordering rules, seek rebuild, rendering, diagnostics, and playlist-aware transport behavior.

---

## 1) Recommended MIDI Library

### Melanchall.DryWetMIDI
Use DryWetMIDI for:
- robust SMF parsing
- tempo map handling and conversions
- extracting events and notes reliably

> Keep your own sequencer loop for exact semantics (pause sends all-notes-off, deterministic seek rebuild, playlist-aware transport flow).

---

## 2) SysEx Handling (Final)

Rules:
- SysEx is transmitted to output during playback.
- SysEx events remain visible in Diagnostics when SysEx visibility is enabled.

---

## 3) Transport, Playlist, and Tempo Controls

- Transport supports Play, Pause, Stop, and Panic (All Notes Off + Reset Controllers).
- Loop mode behavior:
  - single-file playback loops the current file
  - playlist playback advances to next entry and wraps to first when enabled
- Playlist supports replace/add flows, previous/next navigation, and double-click play from the Playlist tab.
- Tempo adjustment applies a signed BPM offset (`TempoAdjustmentBpm`) on top of file tempo, clamped to safe bounds derived from file tempo range.

---

## 4) Core Interfaces (same as v2 companion)

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
  public bool LoopPlayback { get; set; }
  public double TempoAdjustmentBpm { get; set; }
}
```

---

## 5) Event Ordering

Same-tick priority must enforce:
1) bank select (CC0/CC32)
2) program change
3) other CC affecting onset
4) note-on
5) note-off

---

## 6) Seek Rebuild

- Maintain per-channel bank/program state.
- Build checkpoints every N ticks (or per measure) for fast seek.
- Seek interaction is user-driven while playback is paused or stopped.
- On seek, rebuild channel state at target tick and update dependent UI state.

---

## 7) Rendering

- Custom render surface using WPF drawing APIs.
- 60 FPS target.
- Pre-index note spans per lane.
- Visible window computed from playhead at 33% of view width.

---

## 8) Diagnostics and View State

- Diagnostics list is virtualized with category toggles (Notes/CC/PC/Meta/SysEx/Other), meta-only mode, and text search.
- Application settings persist:
  - selected device name
  - window geometry/state
  - loop mode and tempo adjustment
  - piano-roll display toggles (tempo/grid/note names/keys/compact/overlay)
  - default instrument program
  - diagnostics filter state

