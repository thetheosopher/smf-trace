# SMF Trace — See every event. Trust every tick.

**Product / App Name:** **SMF Trace**

**Tagline:** *See every event. Trust every tick.*

**Status:** Final, implementation-ready specification.

---

## 1) Product Goal

Build a Windows desktop application named **SMF Trace** that loads and plays **Standard MIDI Files (SMF, *.mid)**, streams the MIDI events to a selected output device, and provides two synchronized views:

1. A left-to-right scrolling **piano roll** with a fixed playhead.
2. A **Diagnostics** view that reveals **every event** (including meta and SysEx), with filters and a detailed decode pane.

SMF Trace’s design goal is “observability for MIDI files”: users should be able to confirm *what* events exist, *when* they occur, and *exactly what* is being sent.

---

## 2) Scope / Constraints

- **General MIDI 1.0 focused** playback and visualization.
- SMF formats supported: **Type 0** and **Type 1** (Type 2 optional / not required).
- Windows 10/11 desktop.
- Primary implementation language: **C#**.

---

## 3) Locked Requirements (Final)

### A) Piano Roll Layout & Semantics
- Time scroll: **left → right**, playhead fixed.
- Playhead position: **33%** of view width.
- Lanes: **Auto** split by **(track, channel)**.
  - If a track has multiple channels, create separate lanes per channel.
- No keyboard graphic; use **pitch grid** only.
- Pitch range default: **A0–C8** (MIDI 21–108).
- Pitch grid: emphasize octaves and show note labels.
- Note pairing:
  - NoteOn velocity=0 treated as NoteOff.
  - Overlapping NoteOn for same (channel, note) closes previous note immediately.
- Note color: **velocity-based gradient**.
- Default time window: **30 seconds** visible.
- Zoom controls: mouse wheel and/or +/- buttons.

### B) Instrument Assignment (Bank + Program)
- Display instrument **per channel**.
- Track Bank Select **CC0 (MSB)** and **CC32 (LSB)**.
- Track Program Change.
- If no Program Change encountered prior to the first note: display **“(default)”**.
- Seeking updates labels to reflect state **at the exact time**.

### C) Playback + Seeking
- Play / Pause / Stop.
- Pause sends **All Notes Off**.
- Stop sends **All Notes Off** and resets position to **time 0**.
- Seek slider updates continuously during playback.
- Seek drag performs **silent scrubbing** (no MIDI output).
- Seek release:
  - Send All Notes Off
  - Rebuild channel state at target tick
  - Re-emit bank/program/controller state to device
  - Resume playback only if it was previously playing
- Toggles:
  - Tempo (BPM) ON/OFF
  - Bars/Beats grid ON/OFF
- No looping in v1.

### D) Diagnostics Tab
- Shows **all file events** (meta included).
- Ordering for same tick ensures **Program Change before NoteOn**.
- Auto-scroll:
  - Disabled by manual scroll.
  - Re-enabled by click anywhere in list.
- Filters day-1:
  - message type filter
  - meta-only toggle
  - SysEx show/hide toggle
- Details pane:
  - decoded fields + raw bytes.

### E) SysEx Handling (with Safety)
- Default behavior: **send SysEx** to output device.
- Provide setting: **[ ] Disable SysEx output**
  - If enabled: SysEx not sent, but may still be displayed (subject to show/hide).

### F) MIDI Output Devices
- Dropdown shows only **active** MIDI output devices.
- Hot-plug updates list (device connect/disconnect).
- Include **Microsoft GS Wavetable Synth** option.

### G) Performance
- Large orchestral MIDI support.
- Piano roll refresh target **60 FPS**, user-selectable **30 FPS** fallback.

---

## 4) UI / UX Specification

### Main Window
Toolbar:
- Open
- Play / Pause / Stop
- Device dropdown
- Seek slider + time label (mm:ss.fff)
- Toggle: Tempo display
- Toggle: Bars/Beats grid
- FPS selector: 60 / 30
- Zoom controls (+/-)
- **Disable SysEx output** checkbox

Body:
- TabControl:
  - Piano Roll
  - Diagnostics

### Piano Roll Tab
- Fixed playhead at 33% width.
- 30s visible by default.
- Lanes: (track, channel).
- Lane header: Track name, Track index, Channel, instrument label at current time.

### Diagnostics Tab
- Virtualized list of events + details pane.
- Filters for type/meta-only/SysEx show/hide.
- Click-anywhere to re-enable auto-scroll.

---

## 5) Acceptance Criteria

1. SMF Type 0/1 loads and plays to selected MIDI output.
2. Device dropdown updates on hot-plug; includes GS synth option.
3. Piano roll scrolls left→right with playhead at 33% width.
4. Default view shows 30 seconds; zoom controls adjust time scale.
5. Pitch grid shows octave emphasis and note labels.
6. Lanes split by (track, channel).
7. Notes colored by velocity.
8. Program/bank display per channel; “(default)” when no program change seen.
9. Seeking scrubs silently; release rebuilds state and resumes appropriately.
10. Pause/Stop send All Notes Off; Stop resets to 0.
11. Diagnostics shows all events; ordering ensures PC before NoteOn at same tick.
12. Diagnostics filters + details pane function; click anywhere re-enables auto-scroll.
13. SysEx sent by default; Disable SysEx output prevents transmission but not visibility.
14. 60 FPS target with 30 FPS fallback.

---

## 6) Deliverables

- `SMFTrace.sln` with projects:
  - `SMFTrace.Core`
  - `SMFTrace.MidiInterop`
  - `SMFTrace.Wpf`
- Settings persistence (device selection, FPS, grid toggles, Disable SysEx output).
- Unit tests for: ordering, tempo conversion, note pairing, seek-state rebuild.

