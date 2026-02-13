# SMF Trace — See every event. Trust every tick.

**Product / App Name:** **SMF Trace**

**Tagline:** *See every event. Trust every tick.*

**Status:** Final, implementation-ready specification.

---

## 1) Product Goal

Build a Windows desktop application named **SMF Trace** that loads and plays **Standard MIDI Files (SMF, *.mid)**, streams the MIDI events to a selected output device, and provides synchronized playback views plus playlist management:

1. A left-to-right scrolling **piano roll** with a fixed playhead.
2. A **Diagnostics** view that reveals **every event** (including meta and SysEx), with filters and a detailed decode pane.
3. A **Playlist** view for queueing and navigating multiple MIDI files.

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
- Panic command sends All Notes Off and resets controllers.
- Pause sends **All Notes Off**.
- Stop sends **All Notes Off** and resets position to **time 0**.
- Loop playback toggle is supported:
  - Single-file mode loops the current file.
  - Playlist mode advances to next entry and wraps to first when loop is enabled.
- Seek slider updates continuously during playback.
- Direct seek interaction is available when playback is paused or stopped.
- Seeking rebuilds channel state at the target tick.
- Toggles:
  - Tempo (BPM) ON/OFF
  - Bars/Beats grid ON/OFF
  - Note names ON/OFF
  - Piano keys ON/OFF
  - Compact pitch range ON/OFF
  - Overlay mode ON/OFF
  - Track mute/solo panel ON/OFF
  - Lyrics lane ON/OFF (when lyrics are present)
- Tempo adjustment slider applies signed BPM offset to playback tempo.

### D) Diagnostics Tab
- Shows **all file events** (meta included).
- Ordering for same tick ensures **Program Change before NoteOn**.
- Auto-scroll:
  - Disabled by manual scroll.
  - Re-enabled by click anywhere in list.
- Filters day-1:
  - category toggles: Notes, CC, Program Change, Meta, SysEx, Other
  - meta-only toggle
  - SysEx show/hide toggle
  - text search filter
- Details pane:
  - decoded fields + raw bytes.

### E) SysEx Handling
- SysEx events are transmitted to the selected output device during playback.
- SysEx visibility in Diagnostics is controlled by the SysEx show/hide filter.

### F) MIDI Output Devices
- Dropdown shows currently available MIDI output devices.
- Last selected device name is persisted and restored when available.
- Include **Microsoft GS Wavetable Synth** option when exposed by the host system.

### G) Performance
- Large orchestral MIDI support.
- Piano roll refresh target **60 FPS**.

---

## 4) UI / UX Specification

### Main Window
Toolbar:
- Open
- Add files to playlist
- Play / Pause / Stop
- Previous / Next playlist item (visible when playlist has multiple entries)
- Loop playback toggle
- Panic (All Notes Off)
- Device dropdown
- Default instrument dropdown
- Seek slider + time label (mm:ss.fff)
- Toggle: Tempo display
- Toggle: Bars/Beats grid
- Toggle: Note names
- Toggle: Piano keys
- Toggle: Compact pitch range
- Toggle: Overlay mode
- Toggle: Track mute/solo panel
- Toggle: Lyrics lane (when lyrics exist)
- Tempo adjustment slider (+/- BPM)
- Zoom controls (+/-)

Body:
- TabControl:
  - Piano Roll
  - Playlist
  - Diagnostics

### Piano Roll Tab
- Fixed playhead at 33% width.
- 30s visible by default.
- Lanes: (track, channel).
- Lane header: Track name, Track index, Channel, instrument label at current time.
- Optional track panel supports per-track mute/solo state.
- Optional lyrics lane renders lyric meta events with active-line highlighting.

### Playlist Tab
- Displays queued MIDI files with metadata columns (duration, tempo, time signature, key, SMF type, track count, SysEx, lyrics, path).
- Double-clicking an entry loads and plays that item.
- Supports replace-playlist and append-to-playlist workflows.

### Diagnostics Tab
- Virtualized list of events + details pane.
- Filters for category/meta-only/SysEx show-hide/search.
- Click-anywhere to re-enable auto-scroll.

---

## 5) Acceptance Criteria

1. SMF Type 0/1 loads and plays to selected MIDI output.
2. Device dropdown populates available devices and restores last selected device when available.
3. Piano roll scrolls left→right with playhead at 33% width.
4. Default view shows 30 seconds; zoom controls adjust time scale.
5. Pitch grid shows octave emphasis and note labels.
6. Lanes split by (track, channel).
7. Notes colored by velocity.
8. Program/bank display per channel; “(default)” when no program change seen.
9. Seek interaction is available when paused or stopped and rebuilds state at target position.
10. Pause/Stop send All Notes Off; Stop resets to 0; Panic performs immediate note/reset-controllers action.
11. Diagnostics shows all events; ordering ensures PC before NoteOn at same tick.
12. Diagnostics category/meta/SysEx/search filters and details pane function; click anywhere re-enables auto-scroll.
13. SysEx events are transmitted during playback and can be shown/hidden in Diagnostics.
14. Piano roll rendering targets 60 FPS.
15. Playlist tab supports multi-file queue, previous/next navigation, and double-click play.
16. Loop toggle loops single-file playback and wraps playlist playback.
17. Tempo adjustment applies signed BPM offset and updates effective tempo display.
18. Track mute/solo controls update active playback lanes.

---

## 6) Deliverables

- `SMFTrace.slnx` with projects:
  - `SMFTrace.Core`
  - `SMFTrace.MidiInterop`
  - `SMFTrace.Wpf`
  - Settings persistence (device selection, window geometry, tempo/grid toggles, loop mode, tempo adjustment, default instrument, piano-roll display toggles, diagnostics filter state).
- Unit tests for: ordering, tempo conversion, note pairing, seek-state rebuild.

