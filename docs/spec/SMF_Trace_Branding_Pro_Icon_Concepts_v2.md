# SMF Trace — Professional Branding & Icon Concepts (v2)

**App Name:** **SMF Trace**
**Tagline:** **See every event. Trust every tick.**

This pack is tailored for a **MIDI-professional** audience: engineers, composers, live techs, integration specialists, and power users who need **deterministic, inspectable** MIDI playback.

---

## 1) Positioning (Professional)

### One-line positioning
**SMF Trace** is a Standard MIDI File player and diagnostic workbench that streams events to your chosen output device while exposing a precise, timestamped trace of every message.

### Short product description (store/README)
SMF Trace is a Windows desktop workbench for inspecting and playing Standard MIDI Files (.mid). It provides a high‑performance piano roll synchronized to playback and a live, filterable event trace with decoded fields and raw bytes—ideal for validating program/bank changes, controller automation, tempo/meta events, and SysEx event flow.

### “Why SMF Trace?” (for pros)
- **Determinism:** You can verify exactly what is sent, in order, down to raw bytes.
- **Observability:** Every event is visible (meta included) and correlated to time/tick.
- **Operational confidence:** SysEx transmission and event visibility remain predictable during inspection and playback.

### “What is SMF?” (precise help blurb)
> **SMF** stands for **Standard MIDI File** (*.mid). SMF stores time‑stamped MIDI event streams for reliable playback sequencing and can include multiple tracks, tempo and time signature meta events, track names, lyrics, and more. citeturn10search312turn10search306

---

## 2) About Dialog Copy (Professional)

**Title:** About SMF Trace

**Body (concise):**
SMF Trace is a Standard MIDI File playback and diagnostics workbench. It streams events to a selected MIDI output while providing a synchronized piano roll and a complete event trace.

**Body (detailed):**
SMF Trace is designed for professional MIDI validation and troubleshooting. Load a .mid file, route output to any available device (including Microsoft GS Wavetable Synth), and confirm behavior with a timestamped trace of every event: Note On/Off, CC, Program Change, Pitch Bend, Meta events (tempo/time signature), and SysEx. Use seek-with-state-rebuild to verify mid‑song program/bank changes and device state at any point in the timeline.

**Footer (template):**
Version {VERSION}  •  © {YEAR} SMF Trace

---

## 3) UI Labels & Microcopy (Professional)

### Tabs
- **Piano Roll**
- **Playlist**
- **Diagnostics**

### Toolbar
- **Open…**
- **Add to Playlist…**
- **Play**
- **Pause**
- **Stop**
- **Previous** / **Next**
- **Loop Playback**
- **Panic (All Notes Off)**
- **Output Device:**
- **Default Instrument:**
- **Position:**
- **Show Tempo (BPM)**
- **Show Bars/Beats Grid**
- **Show Note Names**
- **Show Piano Keys**
- **Compact Pitch Range**
- **Overlay Mode**
- **Track Mute/Solo Panel**
- **Lyrics Lane**
- **Tempo Adjust (BPM ±)**
- **Zoom In** / **Zoom Out**
- **Vertical Zoom In** / **Vertical Zoom Out**

### Status line (optional)
- **Ready**
- **Loaded:** {FileName}
- **Output:** {DeviceName}
- **Running** • {Time} / {Duration}
- **Paused** • {Time}
- **Stopped**
- **Loading…**

---

## 4) Tooltips (Professional, explicit)

### Open…
Load a Standard MIDI File (.mid) for playback and inspection.

### Output Device
Select the MIDI output target that will receive the streamed events.

### Play
Start playback from the current position.

### Pause
Pause playback and send **All Notes Off** to prevent stuck notes.

### Stop
Stop playback, send **All Notes Off**, and reset position to start.

### Panic
Immediately sends **All Notes Off** and resets controllers.

### Seek / Position
- Seek interaction is available while playback is paused or stopped.
- Seeking rebuilds channel and instrument state at the selected time.

### Add to Playlist…
Append one or more MIDI files to the existing playlist.

### Previous / Next
Navigate within the playlist when multiple entries are present.

### Loop Playback
Loop single-file playback or wrap playlist playback to the first entry.

### Default Instrument
Select fallback program used when the file has no Program Change events.

### Tempo Adjust (BPM ±)
Apply a signed BPM offset to playback tempo.

### Show Tempo (BPM)
Overlay the current tempo at the playhead.

### Show Bars/Beats Grid
Render measure/beat grid lines derived from time signature meta events.

---

## 5) Diagnostics View Copy (Professional)

### Filters
- **Notes / CC / PC / Meta / SysEx / Other**
- **Meta events only**
- **Show SysEx**
- **Search**

### Auto-scroll behavior copy
- **Auto‑scroll OFF** — click anywhere in the event list to re‑enable.

### Event list columns (recommended)
- Index
- Tick
- Time
- Track
- Channel
- Kind
- Summary
- Raw (short)

### Details pane sections
- **Decoded** (structured fields)
- **Raw Bytes** (full)
- **Meta Payload** (when applicable)

---

## 6) Error / Warning Copy (Professional)

### Invalid file
**Unable to load MIDI file**
The selected file could not be parsed as a Standard MIDI File.

### Unsupported format
**Unsupported SMF format**
Supported formats: Type 0 and Type 1.

### Output device missing
**No output device selected**
Select a MIDI output device to enable playback.

### Output device disconnected
**Output device unavailable**
Playback stopped because the selected output device is no longer available.

---

## 7) Shortcut Set (Professional defaults)

- **Ctrl+O** — Open
- **Ctrl+Shift+O** — Add files to playlist
- **Ctrl+Shift+P** — Panic (All Notes Off)
- **Space** — Play / Pause
- **S** — Stop
- **Left/Right** — Previous / Next playlist item
- **L** — Toggle loop playback
- **Plus / Minus** — Horizontal zoom in/out
- **Shift+Plus / Shift+Minus** — Vertical zoom in/out

---

# 8) Icon Concepts (Professional)

Below are **three icon families**, each with multiple variants. All concepts are designed to read well at small sizes (16–32px) and look strong at 256px.

## Icon Family A — “Playhead Trace” (primary recommendation)
**Motif:** Vertical playhead line cutting through a timeline trace.

### A1: Playhead + event ticks
- Background: deep charcoal (#111318)
- Foreground: neon cyan playhead (#00D6FF)
- Secondary: thin event tick marks in gray-blue (#3A475A)
- Optional accent: small red/orange “current event” dot (#FF4D4D)

**Why it fits pros:** implies instrumentation/telemetry and deterministic timing.

### A2: Playhead + stacked tracks
- Same as A1 but with 3 subtle horizontal lane bands (tracks) behind the playhead.
- Adds immediate association with multi-track SMF.

### A3: Playhead + hex nibble
- Add a tiny “0x” glyph in the corner (very subtle) to signal low-level inspection.
- Use only for larger sizes (>= 48px) to avoid clutter.

## Icon Family B — “SMF Monogram + Trace”
**Motif:** The letters **SMF** as a monogram formed by a single continuous trace line.

### B1: Single-line SMF
- One continuous path draws S→M→F like an oscilloscope trace.
- Use cyan (#00D6FF) on charcoal.

### B2: SMF + play triangle
- Add a small play triangle embedded in the negative space of the “M”.

### B3: SMF + tick ruler
- Add a bottom ruler line with 3–5 tick marks.

## Icon Family C — “Chunk Header” (SMF-nerd easter egg)
**Motif:** SMF is chunked; hint at header chunk (MThd/MTrk) without being literal text-heavy.

### C1: Chunk blocks
- Two stacked rounded rectangles: top smaller (header), bottom longer (track).
- A playhead line overlays both.

### C2: Chunk + braces
- Similar to C1 but add subtle bracket glyphs to imply structured binary data.

---

## 9) Icon Production Notes (Windows / WPF)

### Sizes to generate
- **.ico multi-resolution**: 16, 20, 24, 32, 40, 48, 64, 128, 256

### Visual rules
- Avoid fine text at <= 32px.
- Use 1–2 primary colors + one accent.
- Keep a 10–12% padding safe area inside the canvas.

### Suggested accent palette (dark theme)
- Background: #111318
- Primary: #00D6FF
- Secondary: #3A475A
- Accent: #FF4D4D or #FFB020

---

## 10) Optional: App Title Bar / Splash Copy

### Title bar
**SMF Trace**

### Splash / loading
**Loading MIDI timeline…**
**Building tempo map…**
**Indexing note spans…**

---

## 11) Legal / Reference Snippet (optional)

Standard MIDI Files (SMF) are a common interchange format for time‑stamped MIDI event data used for reliable playback sequencing. citeturn10search312turn10search306

