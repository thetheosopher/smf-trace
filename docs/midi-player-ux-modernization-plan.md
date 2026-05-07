# SMF Trace — UX/UI Modernization & Redesign Plan

> **Document purpose.** This is the living design plan **and** the implementation
> tracker for modernizing the SMF Trace MIDI player UI. It is grounded in the
> existing specs ([SMF_Trace_Spec_Final.md](spec/SMF_Trace_Spec_Final.md),
> [SMF_Trace_Companion_Technical_Final.md](spec/SMF_Trace_Companion_Technical_Final.md),
> [SMF_Trace_Branding_Pro_Icon_Concepts_v2.md](spec/SMF_Trace_Branding_Pro_Icon_Concepts_v2.md))
> and the current shipped UI ([MainWindow.PNG](MainWindow.PNG),
> [EventDisplay.png](EventDisplay.png)).
>
> Update the [Implementation Progress](#13-implementation-progress-tracking)
> section as work lands. Do **not** delete superseded ideas — annotate them with
> a dated decision note instead.

---

## Table of Contents

1. [Overall Product Experience](#1-overall-product-experience)
2. [User Personas and Use Cases](#2-user-personas-and-use-cases)
3. [Information Architecture](#3-information-architecture)
4. [Workflow and Usability Review](#4-workflow-and-usability-review)
5. [Visual Design and Modernization](#5-visual-design-and-modernization)
6. [Creative Direction Options](#6-creative-direction-options)
7. [Interaction Design Recommendations](#7-interaction-design-recommendations)
8. [Layout Redesign Strategy](#8-layout-redesign-strategy)
9. [Accessibility and Readability](#9-accessibility-and-readability)
10. [Competitive Benchmarking](#10-competitive-benchmarking)
11. [Proposed Design System](#11-proposed-design-system)
12. [Prioritized Modernization Roadmap](#12-prioritized-modernization-roadmap)
13. [Implementation Progress Tracking](#13-implementation-progress-tracking)

---

## 1. Overall Product Experience

### What the product is today
SMF Trace is a Windows desktop MIDI file player with three primary views in a
single window (Piano Roll, Playlist, Events), a top action bar with
transport + global toggles, a synth/program selector, a session timeline
scrubber at the bottom, and a per-track color legend on the left of the piano
roll.

### What it should *feel* like
A **professional MIDI inspection and playback instrument** — the love-child of:

- a clean media player (immediate, single-window, instant transport),
- a forensic MIDI debugger (every byte visible, ordering guarantees honored),
- and a piano-roll viewer (musical, scannable, alive).

The emotional target is **"calm precision"**: dark, focused, time-confident,
never noisy. Color is reserved for *meaning* (track identity, state, warnings),
not decoration.

### Current strengths
- Single-window, three-tab layout is immediately understandable.
- Dark theme is on-brand for studio/stage use.
- Piano roll already implements the spec's signature feature: fixed playhead,
  left→right scroll, per-(track,channel) lanes with stable colors.
- The Events view exposes the technical truth of the file — a real
  differentiator versus generic MIDI players.

### Current weaknesses
- **Top action bar is a wall of identical blue pill buttons.** Transport,
  view-mode toggles, feature toggles, and the tempo nudger are visually
  indistinguishable. There is no hierarchy, no grouping, no iconography for
  scanning, and no indication of which toggles are *on*.
- **Two competing tempo controls.** A "Tempo" pill button and a "Tempo / − / +"
  cluster sit next to each other; the "216.0 BPM" readout floats inside the
  piano roll. Users cannot tell at a glance which control does what.
- **Synth + program dropdowns share the toolbar with transport,** competing for
  attention with Play/Pause/Stop.
- **Track legend on the left is read-only.** It is a legend, not a mixer — no
  mute, no solo, no volume, no expand, no per-lane focus. The spec calls for
  Mute/Solo (the toolbar has a "Mute/Solo" toggle) but the affordance is hidden.
- **Playhead is a thin red line on a black grid** — fine, but lacks a subtle
  glow / time readout and there is no measure/beat ruler above the roll, only
  pitch labels on the left.
- **The bottom scrubber is a generic `Slider`** with start/end timecodes; no
  waveform-of-activity, no markers, no loop region, no hover preview.
- **Events view is monospaced columns with no zebra-striping, sticky header,
  type icons, or color coding by event family.** The right "Event Details"
  pane is empty until you click — and even then it is just text.
- **No empty state, no recent files surface, no drop target affordance** when
  the app is launched without a file.
- **No status bar.** Latency, output device health, file counts, tempo source
  (file vs. user override), and "writing SysEx" indicators have nowhere to live.
- **Window chrome is default Win32.** A custom title bar with project name +
  current file + transport state would feel substantially more modern.

### Missed opportunities
- The Piano Roll could double as the **timeline ruler** (measures, beats,
  markers, tempo changes, time-signature changes).
- Track headers could become a **mini-mixer strip** (mute / solo / volume /
  pan / current program / level meter).
- A **mini-keyboard at the bottom** that lights up with currently sounding
  notes (per the spec's "Keys" mode) would be a signature visual.
- The diagnostics Events view could become a true **inspector** linked to the
  playhead and to clicked notes in the roll.

---

## 2. User Personas and Use Cases

| Persona | Primary goal | Workflow | Pain points today | UI must prioritize |
|---|---|---|---|---|
| **Casual musician previewing files** | "Does this MIDI sound right?" | Open file → Play → maybe loop a section | Toolbar overwhelms; no recent files; no drop zone | Big Play, drag-drop, recent files, tasteful default visualization |
| **Producer auditioning grooves** | Compare arrangements quickly | Queue several files → play → A/B → tweak tempo | Playlist hidden behind a tab; tempo controls duplicated | Persistent playlist rail, tempo + speed in transport, fast next/prev |
| **Composer reviewing exports** | Verify their export plays correctly | Open → scrub → check tracks → check program changes | Track list is read-only; no measure ruler; no marker list | Mixer-style track strip, measure/beat ruler, marker sidebar |
| **Piano learner / practice user** | Watch the keyboard, slow down, loop a phrase | Open → Keys mode → slow tempo → loop bars | "Keys" is buried in a toggle; no slow-down control; no loop region | Always-on keyboard option, prominent speed slider, drag-to-loop on ruler |
| **Teacher demonstrating** | Project clean visuals, narrate over playback | Open → fullscreen → highlight a track | No fullscreen; no per-track highlight; toolbar visible on stage | Performance/Focus mode, track solo from the legend, large readouts |
| **Developer / MIDI technician** | Inspect every byte, diagnose ordering | Open → Events tab → filter → search → step | Events view is dense and disconnected from playback; no jump-to-event | Events view linked to playhead, click event → seek, color-coded families |
| **Live performer** | Reliable playback to a hardware synth | Pick output → load setlist → cue → fire | Output device picker shares space with everything; no setlist UX; no panic button | Persistent output/status bar, setlist view, large All-Notes-Off panic |

---

## 3. Information Architecture

### Current IA (observed)
```
Window
├── Toolbar (one row)
│   ├── New / Add (files)
│   ├── Play / Pause / Stop / Loop
│   ├── Theme toggle, Diagnostics toggle
│   ├── Output device dropdown
│   ├── Program dropdown
│   ├── View toggles: Compact, Overlay, Grid, Notes, Keys, Tempo
│   ├── Feature toggles: Mute/Solo, No SysEx
│   └── Tempo label + − / +
├── Tab strip: Piano Roll | Playlist | Events
├── Main area (per tab)
└── Bottom scrub slider with start/end timecodes
```

### Recommended IA
Group by **purpose**, not by chronological-implementation order. Push device
config and feature toggles out of the transport row.

```
Window (custom chrome with title + file name + transport state)
├── App Header
│   ├── App title / file name / "modified" indicator
│   └── Window controls
├── Primary Transport Bar  (hero row)
│   ├── Transport cluster: Prev | Rewind | Play/Pause | Stop | Next | Loop
│   ├── Time readout: 00:07.739 / 06:30.967  +  bar.beat (e.g. 5.2.480)
│   ├── Tempo cluster: BPM readout + nudge + "follow file" lock
│   └── Speed cluster: 0.25×–2.00× slider + reset
├── Secondary Toolbar (contextual to current view)
│   ├── View switcher: Piano Roll | Playlist | Events  (segmented control)
│   └── View-specific options (e.g. Compact/Overlay/Grid/Keys for Piano Roll)
├── Main Workspace
│   ├── Left rail: Track Inspector / Mixer (collapsible)
│   ├── Center: Active view (roll / list / events)
│   └── Right rail: Inspector (note details, event details, file metadata)
├── Optional bottom dock: Mini-keyboard ("Keys" mode)
├── Timeline Scrubber (full width, with markers + loop region)
└── Status Bar
    ├── MIDI output device + activity LED
    ├── SysEx state ("SysEx out: ON / Suppressed")
    ├── Latency / underrun indicator
    └── File: tracks / channels / events / duration
```

### Hierarchy
- **Primary** — Open, Play/Pause, Stop, Seek, Tempo, Speed, Output Device.
- **Secondary** — Loop, Mute/Solo per track, View mode, Program preview.
- **Advanced** — Compact/Overlay rendering, Grid density, SysEx suppression,
  Diagnostics overlay, intra-tick ordering proof view.
- **Configuration** — Settings window (themes, default zoom, output defaults,
  keyboard shortcuts, file associations).
- **Diagnostic** — Events tab + a collapsible "Inspector" pane that shows
  decoded bytes for the selected event or playhead-adjacent events.

---

## 4. Workflow and Usability Review

### 4.1 Loading and managing files
| Friction | Recommendation |
|---|---|
| No drop zone on empty state | Big "Drop a `.mid` file here" target with browse button and recent files list |
| "New" / "Add" icons unclear | Replace with labeled `Open File…` and `Add to Playlist…` (icon + text) |
| Recent files not surfaced | File menu + an empty-state recents card + a quick-switcher (Ctrl+P) |
| Playlist hidden in tab | Promote to a collapsible left rail; tab still available for full view |
| Metadata invisible | Show title / copyright / time-sig / key / tempo in a header card under the file name |

### 4.2 Playback
| Friction | Recommendation |
|---|---|
| Play/Pause/Stop are equal-weight pills | Make Play/Pause the visual hero (filled, larger), Stop secondary |
| No prev/next track buttons | Add when a playlist is loaded |
| No "go to start" / "go to end" | Add ⏮ / ⏭ buttons or bind Home / End |
| Loop on/off without region | Add drag-on-ruler loop region; toolbar Loop button toggles whether region loops |
| Tempo controls duplicated | Single tempo widget: BPM readout, ±, "Follow file" lock, double-click to reset |
| No speed (rate) control distinct from tempo | Add 0.25×–2× slider; tempo and speed are independent (spec-aligned) |
| No metronome / count-in | Optional metronome toggle in transport overflow |

### 4.3 Tracks & channels
| Friction | Recommendation |
|---|---|
| Legend is read-only | Convert each row into a track strip: color swatch · name · M / S · volume slider · level meter · program name |
| No "focus this track" | Click track name → solo-with-dim (others fade in roll) |
| Channel info hidden | Show `Trk 4 · Ch 3` subtitle under each strip |
| Active track invisible | When a note plays, briefly pulse its strip's color swatch |

### 4.4 Visualization (Piano Roll)
| Friction | Recommendation |
|---|---|
| No measure/beat ruler above the roll | Add a sticky time ruler with bars, beats, time-signature changes, tempo changes, markers |
| Pitch labels only every octave-ish | Highlight C notes; subtle "white-key band" so pitch reads at a glance |
| Playhead is a flat line | Add a subtle glow + small time chip at the top |
| No velocity cue | Note opacity or a thin velocity bar below each note |
| No hover information | Hover note → tooltip with pitch · velocity · duration · track · channel |
| Overlay / Compact / Grid toggles unexplained | Add icons + tooltips; persist per file or globally |
| 30s window not adjustable from UI | Surface the zoom (window-seconds) as a small +/− with a "30 s" readout, plus pinch/Ctrl+wheel |

### 4.5 Visualization (Events)
| Friction | Recommendation |
|---|---|
| Monospaced wall of text | Zebra rows, sticky header, monospace only for the Data column |
| No event-family colors | Color the "Type" cell: Notes (track color), CC (cyan), PC (amber), Meta (gray), SysEx (purple), warnings (red) |
| No icons | Tiny glyphs per family for fast scanning |
| Disconnected from playback | Auto-scroll-to-playhead toggle; clicking a row seeks; current-row highlight |
| Filters are checkbox row | Convert to chip-style filter bar with counts per family (e.g. "Notes 4,212") |
| Search is a tiny textbox | Make it the primary control, with regex toggle and field selector (Type/Data/Track/Channel) |
| Event Details pane empty | Always show the event under the playhead when nothing is selected |

### 4.6 Settings & routing
| Friction | Recommendation |
|---|---|
| Output device in main toolbar | Move to status bar (with a click-to-change popover) and Settings |
| Program dropdown ambiguous | Label it "Preview Program" and place it next to the keyboard mode toggle; clarify it does not modify the file |
| No latency / sync controls visible | Add to Settings → Audio/MIDI Out |
| No "panic / All Notes Off" | Add a dedicated red-outlined Panic button near transport (spec's All-Notes-Off semantics) |

### Cross-cutting: feedback, shortcuts, undo
- **Feedback:** every transport action should produce a visible state change
  within 100 ms (button pressed state, playhead motion, status bar text).
- **Shortcuts:** Space = Play/Pause, `.` = Stop, `,` / `'` = prev/next file,
  `L` = loop, `M`/`S` on focused track, `[` `]` = nudge tempo, `Ctrl+O` = open,
  `Ctrl+,` = settings, `Esc` = panic / All Notes Off, `F11` = focus mode.
- **Undo:** the player itself does not edit MIDI, so undo is N/A — but
  *settings changes during a session* (mute/solo, tempo override, loop region)
  should support `Ctrl+Z` / `Ctrl+Y`.

---

## 5. Visual Design and Modernization

### Diagnosis
- **Color overuse on toolbar.** Every toggle is the same saturated blue. Blue
  should mean "primary action / selected" only.
- **Spacing.** 8 px paddings throughout flatten everything; the eye has nowhere
  to rest. Use a 4 / 8 / 12 / 16 / 24 / 32 px scale.
- **Typography.** A single weight at a single size. Introduce a scale: display
  (file name), title (panel), body, caption, mono (data).
- **Icons.** Icons-only buttons in the toolbar lack tooltips and labels.
  Pair icon + label for primary, icon-only with tooltip for secondary.
- **Borders & dividers.** Subtle 1 px `#2A2F38`-style hairlines instead of the
  current hard panel splits.
- **Buttons.** Three button styles only: *primary filled*, *secondary tonal*,
  *ghost / icon*. Toggles use a fourth: *segmented*.
- **Cards / panels.** Round all panels at 8 px, give 1 px stroke + soft shadow
  for depth.

### Recommended cohesive direction
A **dark studio surface** (`#0E1116` background, `#161A22` panels) with one
*musical accent* (teal/cyan `#3DDBD9`) used for active state, a warm accent
(amber `#F5A524`) for warnings/tempo, red (`#E5484D`) for the playhead and
panic, and **track colors that survive on dark** (saturated, mid-light, ~70%
luminance). Light mode is a real first-class theme, not a tint.

---

## 6. Creative Direction Options

### Option A — Minimal Professional MIDI Utility
- **Tone:** quiet, surgical, close to a "code editor for MIDI".
- **Palette:** near-black (`#0E1116`) + one cool accent + grayscale ramp.
- **Typography:** Inter 13/14 + JetBrains Mono for data.
- **Layout:** flat single window, almost no chrome, maximum content.
- **Material:** flat, hairline borders, no gradients, minimal motion.
- **Audience:** developers, MIDI technicians, advanced producers.
- **Pros:** scannable, low fatigue, fast to ship, aligns with current bones.
- **Cons:** could feel "too much like a debugger" for casual users.
- **Risk:** low.

### Option B — Modern DAW-Inspired Piano Roll Player
- **Tone:** confident, professional, Ableton/Logic-adjacent without copying.
- **Palette:** dark studio + saturated track colors + one teal accent.
- **Typography:** Inter + tabular numerals for time and BPM.
- **Layout:** transport bar, left mixer rail, center roll, right inspector,
  bottom keyboard, status bar.
- **Material:** subtle elevation, soft shadows, 8 px radii, micro-animations.
- **Audience:** producers, composers, working musicians.
- **Pros:** broadest appeal, fits the spec's piano-roll-first identity.
- **Cons:** more surfaces to design and maintain.
- **Risk:** medium.

### Option C — Educational Practice Companion
- **Tone:** friendly, readable, encouraging.
- **Palette:** soft dark or warm light; large keyboard; pastel-but-grounded
  track colors.
- **Typography:** larger sizes (14/16), high line-height.
- **Layout:** giant keyboard at the bottom, large playhead, big transport,
  always-on tempo & speed sliders, loop region as a hero affordance.
- **Material:** rounded (12 px), generous padding, slow ease-out motion.
- **Audience:** piano learners, teachers, students.
- **Pros:** high differentiation in a crowded space.
- **Cons:** feels "less pro" for studio users.
- **Risk:** medium.

### Option D — Performance-Oriented MIDI Playback Deck
- **Tone:** stage-ready, high-contrast, glanceable from 2 m.
- **Palette:** very dark (`#000`) + one bright accent; oversize numerals.
- **Typography:** large, condensed, tabular numerals dominant.
- **Layout:** big transport, big BPM, big file name, setlist rail, panic
  button, no clutter; piano roll secondary.
- **Material:** flat, near-zero animation, hardware-button feel.
- **Audience:** live performers, MDs, theater pit operators.
- **Pros:** unique positioning; very low-risk on stage.
- **Cons:** weakest for MIDI inspection workflows.
- **Risk:** medium.

### Option E — Futuristic Musical Visualizer
- **Tone:** immersive, animated, "Synthesia at studio quality."
- **Palette:** deep navy/black with neon track gradients and bloom.
- **Typography:** modern geometric sans, glow effects.
- **Layout:** roll dominates fullscreen; inspectors collapse into floating
  glass panels.
- **Material:** translucent panels, glow, particle accents on note-on.
- **Audience:** demos, streamers, casual visualization fans.
- **Pros:** memorable, screenshottable.
- **Cons:** can fight long-session readability; risk of looking "toy".
- **Risk:** high.

### Recommendation
**Hybrid of B (primary) + A (mode) + D (mode):**

- Default experience = **Option B** (Modern DAW-Inspired Piano Roll Player).
- Provide a **"Compact / Inspector" mode** that presents Option A's
  utilitarian density for power users (and aligns with the existing "Compact"
  toggle).
- Provide a **"Performance / Focus mode" (F11)** that adopts Option D's
  oversized transport and hides inspectors — for stage and teaching.
- Keep a sliver of Option E only as opt-in "visualizer" eye candy (e.g. note
  glow on note-on) so it never compromises readability.

This hybrid honors all the spec's existing toggles (Compact, Overlay, Grid,
Notes, Keys) by re-framing them as **rendering modes within one coherent
visual system** instead of independent visual experiments.

---

## 7. Interaction Design Recommendations

- **Drag-and-drop** anywhere in the window opens a file (or appends to the
  playlist if Shift is held). Show a translucent overlay during drag.
- **Timeline scrubber:** click to seek, drag to scrub silently (per spec; on
  release, rebuild bank/program/CC state and re-emit), shift-drag to set loop
  region, hover shows time + bar.beat tooltip, double-click to clear loop.
- **Loop region** on the *roll's* ruler: drag to create, drag edges to resize,
  drag center to move, right-click for "Loop selection" / "Clear".
- **Zoom & pan** the roll: `Ctrl+Wheel` zooms time, `Shift+Wheel` zooms pitch,
  middle-drag pans, `0` to reset to the spec's 30 s window.
- **Track strip:** click name = focus (dim others); `M` / `S` on focus =
  mute/solo; drag volume slider with hold-`Shift` for fine; double-click = reset.
- **Note hover:** tooltip + persistent inspector update; click = pin in
  inspector with "Show in Events" link.
- **Keyboard visualization:** clickable mini-keyboard preview-plays the note
  through the current "Preview Program."
- **Tempo nudging:** scroll-on-BPM-readout to nudge ±1, hold `Shift` for ±0.1,
  click readout to type a value, `Ctrl+click` to "follow file" (lock).
- **Marker navigation:** markers extracted from Meta events become chips on
  the ruler; `J` / `K` jumps prev/next.
- **Playback speed control:** dedicated slider, distinct from tempo; right-
  click to reset; numeric tooltip while dragging.
- **Multi-file navigation:** `Ctrl+Tab` opens the file switcher (recent +
  playlist), arrow keys to choose, Enter to load, Space to preview.
- **MIDI output routing:** status-bar device chip → click → popover with all
  outputs, search, "Test note" button, latency readout.
- **Keyboard shortcuts:** every primary control bindable; `?` opens a cheat
  sheet overlay. Shortcuts visible in tooltips: `Play (Space)`.
- **Context menus:** right-click on track strip, on note, on event row, on
  ruler — every right-click discoverable, no hidden config.
- **Tooltips & inline help:** every toolbar control has a tooltip with name +
  shortcut + one-line description. First-run coachmarks for the three tabs.

**Direct manipulation > hidden menus:** loop region from the ruler, tempo
from the BPM chip, mute/solo from the track strip, output device from the
status bar.

---

## 8. Layout Redesign Strategy

```
┌──────────────────────────────────────────────────────────────────────────────┐
│  ◐ SMF Trace · Beethoven-Symphony5-1.mid · ▸ Playing            [_] [□] [×] │  ← App header / custom chrome
├──────────────────────────────────────────────────────────────────────────────┤
│  ⏮  ⏪  ▶  ⏹  ⏭   ⟲ Loop   00:07.739 / 06:30.967   ♩ 216.0 BPM ▾   1.00× ▾ │  ← Primary transport
├──────────────────────────────────────────────────────────────────────────────┤
│  [Piano Roll] [Playlist] [Events]      Compact · Overlay · Grid · Keys · ⓘ  │  ← Secondary toolbar (segmented + view options)
├────────────┬─────────────────────────────────────────────────────┬───────────┤
│  Tracks    │  ▏Bars 1   2   3   4   5   6   7   8   9   10      │ Inspector │
│  ┌──────┐  │  ▏──────────────────────────|playhead|──────────────│ ┌───────┐ │
│  │■ Flu │  │ C5 ░░░░░░░░░ █████ ░░░░░░░░|  ░░░░░░ ░░░░░░░░░░░░  │ │ Note  │ │
│  │M S 75│  │ C4 ░░░░░░░░░░░░░░░░░░░░░░░░|  ████░░ ░░░░░░░░░░░░  │ │ A4 v94│ │
│  └──────┘  │ C3                          |                       │ │ Trk 4 │ │
│  ┌──────┐  │                                                     │ │ Ch 1  │ │
│  │■ Obo │  │            ▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒▒         │ ├───────┤ │
│  │M S 75│  │                                                     │ │ File  │ │
│  └──────┘  │                                                     │ │ Meta  │ │
│   …        │                                                     │ └───────┘ │
├────────────┴─────────────────────────────────────────────────────┴───────────┤
│  ▏▏▏▏▏▏▏▏▏▏▏▏▏▏▏▏▏▏▏ [ Mini-keyboard with currently sounding notes ] ▏▏▏▏  │  ← Optional dock
├──────────────────────────────────────────────────────────────────────────────┤
│  ◀════════●═════ loop ═════════════════════════════════▶ 00:07.739 / 06:30  │  ← Full-width scrubber w/ markers
├──────────────────────────────────────────────────────────────────────────────┤
│  ● VirtualMIDISynth #1   SysEx: ON   Lat: 4 ms   12 trk · 16 ch · 18,402 ev │  ← Status bar
└──────────────────────────────────────────────────────────────────────────────┘
```

### Attention flow
1. **App header** (file identity + state) — peripheral confidence.
2. **Transport bar** — what users instinctively reach for.
3. **Center workspace** — the work itself (roll / playlist / events).
4. **Track rail** — the parallel mental model of "who is playing."
5. **Inspector** — on-demand depth.
6. **Scrubber** — orientation in time.
7. **Status bar** — silent reassurance about routing/health.

### Responsive behavior
- Below ~1100 px wide: collapse Inspector by default; track rail becomes
  icon-only with hover popovers.
- Below ~900 px wide: stack secondary toolbar under transport.
- Below ~700 px tall: hide the mini-keyboard dock by default.
- Window remembers per-monitor splitter positions and which dock was open.

### Performance / Focus mode (F11)
- Hides app header chrome, secondary toolbar, inspector, status bar.
- Oversize transport, BPM, time, file name.
- Roll fills the screen; mini-keyboard always shown.
- One-key escape (`F11` or `Esc`).

### Settings live in their own window
File · MIDI Out · Appearance (theme, density) · Piano Roll defaults
(window-seconds, grid, key labels) · Diagnostics · Shortcuts · About.
**Removed from toolbar:** Theme toggle, Diagnostics toggle, No SysEx — all
move into Settings or the status bar (SysEx state).

---

## 9. Accessibility and Readability

- **Text size.** Minimum body 13 px; allow a 110% / 125% UI scale in Settings.
- **Contrast.** Body text ≥ 4.5:1 against panel bg; non-text UI ≥ 3:1.
- **Color blindness.** Never rely on hue alone:
  - Track identity = color **+** unique pattern strip (solid, striped, dotted)
    on the track strip swatch and (optionally) on the note edges.
  - Mute = dim + slash icon. Solo = filled + ring icon. Warnings = red + ⚠.
  - Provide a "Color-blind safe" palette (Okabe–Ito based) in Settings.
- **Hit targets.** Minimum 32×32 px for primary, 24×24 px for dense rows.
- **Keyboard navigation.** Tab order through transport → secondary toolbar →
  track rail → roll → inspector → scrubber → status. Visible focus ring
  (2 px accent outline + 1 px contrast halo).
- **Screen readers.** Every interactive control has `AutomationProperties.Name`
  and `HelpText`. Status bar updates use `LiveSetting=Polite`.
- **Reduced motion.** Respect Windows "Show animations" setting; provide an
  in-app "Reduce motion" toggle that disables note glow, scroll easing, and
  loading shimmer.
- **Dark / light.** Both themes are first-class. Light mode uses warmer
  paper-white panels (`#F7F7F4`) so it does not bleach in studio lighting.
- **Studio + stage.** Add a "Stage" theme (true black + max contrast accent)
  that pairs with Performance mode for projector / theater pit.
- **Long-session fatigue.** No pure-white-on-pure-black; prefer
  `#E6E8EC` on `#0E1116`. Avoid saturated red except for playhead/panic.

---

## 10. Competitive Benchmarking

| Product | Patterns to learn from | Patterns to avoid |
|---|---|---|
| **Ableton Live** | Clip-color discipline; left device rail; minimal ornament | Modal session/arrange split (overkill here) |
| **Logic Pro** | Beautifully restrained transport; bar/beat readout next to time | Mac-only conventions; heavy inspector |
| **Cubase** | MIDI list editor (great reference for the Events view) | Toolbar density |
| **FL Studio** | Bright track colors; piano-roll velocity bars | Skinned / non-system widgets |
| **Reaper** | Customizability; dense but orderly | Out-of-the-box visuals |
| **Synthesia** | Falling-notes feel; keyboard-led learning | Game-like chrome |
| **MuseScore 4** | Modern, cohesive dark theme; clear typography | Score-centric flows |
| **Foobar2000 / Winamp** | Lightweight player feel; playlist/queue rigor | Skin chaos |
| **Modern media players (mpv UIs, Plex)** | Bottom scrubber with hover preview | Auto-hide chrome (bad for studio) |
| **DJ tools (Rekordbox, Traktor)** | Stage-grade contrast; large numerals | Heavy decoration |
| **Music ed apps (flowkey, Simply Piano)** | Friendly tempo/loop UI | Gamification |

### Where SMF Trace can differentiate
1. **First-class diagnostics + first-class playback in one window.** No DAW
   exposes raw events this cleanly; no media player exposes a piano roll this
   well.
2. **Spec-grade ordering guarantees made visible.** A small "ordering proof"
   indicator next to a tick (Program-Change-before-NoteOn etc.) is unique.
3. **Silent-scrub with state rebuild on release** is a delightful detail most
   players get wrong; the UI should celebrate it (subtle "rebuilding state…"
   chip on release).
4. **Disable-SysEx-output toggle** with the file still rendering SysEx in the
   Events view — a feature for techs that no consumer player offers.

---

## 11. Proposed Design System

### 11.1 Typography
| Role | Family | Weight | Size / line-height |
|---|---|---|---|
| Display (file name) | Inter | 600 | 18 / 24 |
| Title (panel header) | Inter | 600 | 14 / 20 |
| Body | Inter | 400 | 13 / 18 |
| Caption / meta | Inter | 400 | 11 / 16 |
| Numerals (time, BPM) | Inter (tabular nums) | 600 | 14–24 / context |
| Mono (events, data) | JetBrains Mono | 400 | 12 / 18 |

### 11.2 Color palette (dark)
| Token | Hex | Usage |
|---|---|---|
| `bg.app` | `#0E1116` | Window background |
| `bg.panel` | `#161A22` | Panels, cards |
| `bg.panel.alt` | `#1B2030` | Zebra rows, inputs |
| `bg.elevated` | `#202636` | Popovers, menus |
| `border.subtle` | `#262C38` | Hairlines |
| `border.strong` | `#384154` | Focus, active borders |
| `text.primary` | `#E6E8EC` | Body |
| `text.secondary` | `#A4ABB8` | Captions |
| `text.disabled` | `#5C6473` | Disabled |
| `accent` | `#3DDBD9` | Primary action / selected |
| `accent.fg` | `#04221F` | Text on accent |
| `warning` | `#F5A524` | Tempo override, attention |
| `danger` | `#E5484D` | Playhead, panic, errors |
| `success` | `#46A758` | OK / confirmed |
| `info` | `#5EAEFF` | Information chips |

Light theme mirrors with `#F7F7F4` / `#FFFFFF` / `#E5E7EB` and adjusts
text/accent for contrast. Stage theme uses `#000000` / `#0A0A0A` /
`#F5F5F5` text / `#FFC857` accent.

### 11.3 Semantic colors
- **Playhead** = `danger`.
- **Loop region** = `accent` at 25% opacity fill, 100% stroke.
- **Selected event / note** = `accent`.
- **Warning event** = `warning`.
- **Muted track** = desaturate to 30%, dim to 60%.
- **Soloed track** = full saturation; non-soloed tracks dim to 25%.

### 11.4 Track color strategy
Curated 16-color palette tuned for dark backgrounds, max ΔE separation,
~70% perceptual luminance, and pattern-pairing for color-blind safety:

```
1  Cyan      #3DDBD9
2  Mint      #5DD39E
3  Lime      #B0E436
4  Amber     #F5A524
5  Coral     #FF7A59
6  Pink      #F26FB1
7  Magenta   #C36CFF
8  Violet    #8A7BFF
9  Sky       #5EAEFF
10 Teal      #2EBFA5
11 Olive     #B5C25A
12 Gold      #E0C151
13 Rose      #E45A77
14 Plum      #9C6CFF
15 Steel     #8FA3C0
16 Sand      #C8B98D
```
Assignment by `(trackIndex, channel)` per spec, deterministic and persistent
across sessions.

### 11.5 Spacing scale
`2 · 4 · 8 · 12 · 16 · 24 · 32 · 48` — never use other values.

### 11.6 Radii
`0 · 4 · 8 · 12 · 999`. Default panel = 8, button = 6, chip/pill = 999.

### 11.7 Buttons
| Variant | Use | Spec |
|---|---|---|
| Primary | Play, Open, Save | Filled `accent`, `accent.fg` text, h=32 |
| Secondary | Stop, Loop, Settings | Tonal `bg.elevated`, `text.primary` |
| Ghost | Toolbar icon-only | Transparent, hover `bg.panel.alt` |
| Segmented | View switcher, mode toggles | Group with selected = tonal |
| Destructive | Panic | Outlined `danger`, hover fills |

### 11.8 Icons
Stroke-based, 1.5 px stroke, 20 px grid (Lucide / Phosphor style). Custom
icons for MIDI-specific concepts: track, channel, program, SysEx, tempo,
marker, panic.

### 11.9 Panels & cards
`bg.panel`, 1 px `border.subtle`, radius 8, optional 0/2/8 rgba(0,0,0,0.25)
shadow only on popovers and modals.

### 11.10 Transport controls
Pill cluster with hairline dividers; Play is filled accent and 8 px taller;
all others ghost. Time + BPM use tabular numerals at 16/14.

### 11.11 Timeline & roll
- Background `#10131A` with 8 px / 32 px / 128 px grid hierarchy.
- Beat lines `#1E2330`, bar lines `#2A3142`, downbeats `#384154`.
- Notes: track color, 3 px radius, 1 px darker stroke, opacity by velocity
  (0.55–1.0).
- Playhead: 2 px `danger` line + 6 px outer glow at 20% alpha.
- Markers: 1 px dashed line + chip on the ruler.

### 11.12 Piano roll keyboard (left)
- Black keys darker than `bg.app`, white keys `bg.panel.alt`.
- Currently sounding notes light up in the corresponding track color (with
  alpha mixing if multiple).

### 11.13 States
| State | Treatment |
|---|---|
| Hover | +6% lightness on bg, cursor change |
| Active | +12% lightness, 1 px inner accent border |
| Focus | 2 px `accent` outline + 1 px `bg.app` halo |
| Selected | `bg.elevated` + accent left bar (4 px) |
| Disabled | 40% opacity, no hover |

### 11.14 Empty / loading / error
- **Empty (no file):** centered illustration (record/disc), "Drop a `.mid`
  file here", primary `Open File…`, secondary `Open Recent ▾`.
- **Loading:** subtle shimmer on the roll's background grid + "Parsing
  18,402 events…" caption in the status bar.
- **Error:** in-panel banner with icon + plain-language message + "Show
  details" disclosure (links to Events tab filtered to warnings).

### 11.15 Motion
- 120 ms ease-out for state changes.
- 200 ms ease-in-out for panel show/hide.
- No motion on the roll's content while playing — the roll itself moves at
  playback rate; do not double-animate.
- Note glow on note-on: 180 ms fade, opt-out via Reduce motion.

---

## 12. Prioritized Modernization Roadmap

### Phase 1 — UX & Visual Quick Wins
**Goals:** make the existing surface feel intentional without rewriting layout.

| Task | Benefit | Risk | Acceptance |
|---|---|---|---|
| Establish design tokens (colors, type, spacing, radii) in WPF resource dictionaries | Foundation for everything | Low | All hard-coded brushes/sizes in `MainWindow.xaml` reference tokens |
| Re-style toolbar buttons into 3 variants (primary/secondary/ghost) | Visual hierarchy | Low | Play is primary; toggles read as toggles (selected vs unselected) |
| Add tooltips with shortcut hints to every toolbar control | Discoverability | Low | Every button has `ToolTip` with name + shortcut + 1-line desc |
| Group transport vs. view toggles vs. config with hairline dividers | Scannability | Low | Three visible groups in the top bar |
| Replace duplicate Tempo controls with a single BPM chip + nudge | Removes confusion | Low | Only one tempo widget in the UI |
| Promote Output Device + SysEx state to a status bar | De-clutters toolbar | Low | New status bar at window bottom shows device + SysEx + counts |
| Empty state with drop target + recent files | First-run is no longer blank | Low | Launching with no file shows the empty-state card |
| Tabular numerals + monospace for time and event Data column | Stops jitter | Low | Time/BPM no longer jitter; events Data column aligns |
| Zebra rows + sticky header in Events view | Readability | Low | Events list scans in <1 s |

**Acceptance for the phase:** the app *looks* deliberately designed without
any layout changes; no regressions in piano-roll behavior.

### Phase 2 — Workflow Improvements
**Goals:** remove friction in the main loops.

| Task | Benefit | Risk | Acceptance |
|---|---|---|---|
| Drag-and-drop file open + Shift-drop to append to playlist | Faster loading | Low | Dropping a `.mid` from Explorer loads or appends |
| File menu + Recent files | Standard expectation | Low | Recent list persists across sessions |
| Quick switcher (Ctrl+P) over recents + playlist | Power-user nav | Med | Type-to-filter, Enter to load |
| Loop region on the scrubber and on the roll's ruler | Direct manipulation | Med | Drag to set, drag edges to resize, double-click to clear |
| Speed control (0.25×–2×) separate from tempo | Practice/audition | Med | Speed and tempo can be changed independently per spec |
| Track strip with Mute/Solo/Volume/Program | Mixer-grade control | Med | Replaces read-only legend; per-track mute/solo work end-to-end |
| Auto-scroll + click-to-seek in Events view | Connects views | Med | Selected event seeks; playhead-following toggle works |
| Event filter chips with counts | Faster filtering | Low | Filter row shows live counts per family |
| Search box upgrade (regex, field selector) in Events | Power use | Low | Regex toggle + scope dropdown |
| Keyboard shortcuts cheat sheet (`?`) | Discoverability | Low | Overlay lists all shortcuts grouped by area |
| Panic button (All Notes Off) bound to `Esc` | Safety | Low | Panic flushes all channels per spec |

**Acceptance:** every persona's primary task can be completed in fewer steps
and without hunting through toggles.

### Phase 3 — Major Interface Redesign
**Goals:** ship the layout in §8.

| Task | Benefit | Risk | Acceptance |
|---|---|---|---|
| Custom window chrome with title + file name + transport state | Modern look | Med | App header replaces native title bar; min/max/close intact |
| Implement primary transport bar from §8 | Hero clarity | Med | Transport, time, tempo, speed in one cohesive row |
| Segmented view switcher (Piano Roll / Playlist / Events) | Cleaner nav | Low | Replaces tab strip |
| Left rail: Track Inspector / Mixer (collapsible) | Persistent access | Med | Visible across all views, collapsible to icons |
| Right rail: Inspector (note / event / file metadata) | Always-on depth | Med | Updates on selection and on playhead idle |
| Measure/beat ruler over the roll with tempo & marker chips | Musical orientation | Med | Bars, beats, tempo changes, markers all visible |
| Mini-keyboard dock | Signature visual + practice value | Med | Lights up sounding notes; click previews via current Program |
| Full-width scrubber with hover preview + markers + loop | First-class timeline | Med | Hover shows time/bar.beat; markers clickable |

**Acceptance:** the app matches the §8 layout sketch and the §6 hybrid
direction (B + A mode + D mode).

### Phase 4 — Advanced Polish
**Goals:** the difference between "nice" and "memorable."

| Task | Benefit | Risk | Acceptance |
|---|---|---|---|
| Performance / Focus mode (F11) | Stage + teaching | Med | F11 toggles oversized layout per §8 |
| Theme system: Dark / Light / Stage / Color-blind safe | Inclusive | Med | All themes pass contrast checks |
| Note-on glow + reduce-motion respect | Delight without harm | Low | Animation honors OS + in-app toggles |
| Velocity rendering on notes (alpha or bar) | More musical info | Low | Toggleable; off by default |
| Settings window consolidation | Removes toolbar toggles | Med | Theme, Diagnostics, SysEx, Defaults all live in Settings |
| Intra-tick "ordering proof" overlay in Events | Spec-unique feature | Low | At a tick boundary, show the guaranteed PC-before-NoteOn order |
| Silent-scrub "rebuilding state…" feedback chip | Communicates spec-correct behavior | Low | Chip appears for the duration of state rebuild on scrub release |
| First-run coachmarks for the three views | Onboarding | Low | One-time, dismissible |
| Accessibility audit pass (focus, names, contrast, keyboard) | Inclusive | Low | Manual a11y checklist passes |

---

## 13. Implementation Progress Tracking

> Update this table as work lands. Add a dated entry to the
> [Decision Log](#decision-log) for any deviation from the plan.

**Status legend:** ⚪ Not Started · 🟡 In Progress · 🟢 Done · ⛔ Blocked · ✋ Deferred

### Phase 1 — UX & Visual Quick Wins

| Area | Task | Status | Started | Completed | Notes |
|---|---|---|---|---|---|
| Design System | Establish design tokens (colors, type, spacing, radii) in WPF resources | ⚪ |  |  |  |
| Visual | Refactor toolbar buttons into Primary / Secondary / Ghost variants | ⚪ |  |  |  |
| Discoverability | Add tooltips (name + shortcut + description) to all toolbar controls | ⚪ |  |  |  |
| Layout | Group transport vs. view toggles vs. config with dividers | ⚪ |  |  |  |
| Transport | Replace duplicate Tempo controls with a single BPM chip + nudge | ⚪ |  |  |  |
| Layout | Add status bar; move Output Device + SysEx state into it | ⚪ |  |  |  |
| Empty State | Drop target + recent files card when no file is loaded | ⚪ |  |  |  |
| Typography | Tabular numerals for time/BPM; mono for Events Data column | ⚪ |  |  |  |
| Events View | Zebra rows + sticky header | ⚪ |  |  |  |

### Phase 2 — Workflow Improvements

| Area | Task | Status | Started | Completed | Notes |
|---|---|---|---|---|---|
| Files | Drag-and-drop open (Shift = append to playlist) | ⚪ |  |  |  |
| Files | Recent files menu + persistence | ⚪ |  |  |  |
| Files | Ctrl+P quick switcher over recents + playlist | ⚪ |  |  |  |
| Playback | Loop region on scrubber and ruler (drag to set/resize) | ⚪ |  |  |  |
| Playback | Speed control (0.25×–2×) independent of tempo | ⚪ |  |  |  |
| Tracks | Track strip with Mute / Solo / Volume / Program | ⚪ |  |  |  |
| Events | Auto-scroll + click-to-seek; current row highlight | ⚪ |  |  |  |
| Events | Filter chips with live counts per family | ⚪ |  |  |  |
| Events | Search upgrade: regex + scope selector | ⚪ |  |  |  |
| Discoverability | Shortcut cheat sheet overlay (`?`) | ⚪ |  |  |  |
| Safety | Panic / All-Notes-Off button bound to `Esc` | ⚪ |  |  |  |

### Phase 3 — Major Interface Redesign

| Area | Task | Status | Started | Completed | Notes |
|---|---|---|---|---|---|
| Chrome | Custom title bar with app + file + transport state | ⚪ |  |  |  |
| Layout | Primary transport bar per §8 | ⚪ |  |  |  |
| Nav | Segmented view switcher (replaces tab strip) | ⚪ |  |  |  |
| Layout | Left rail: Track Inspector / Mixer (collapsible) | ⚪ |  |  |  |
| Layout | Right rail: Inspector (note / event / file metadata) | ⚪ |  |  |  |
| Roll | Measure/beat ruler with tempo & marker chips | ⚪ |  |  |  |
| Visualization | Mini-keyboard dock with currently sounding notes | ⚪ |  |  |  |
| Timeline | Full-width scrubber with hover preview + markers + loop | ⚪ |  |  |  |

### Phase 4 — Advanced Polish

| Area | Task | Status | Started | Completed | Notes |
|---|---|---|---|---|---|
| Modes | Performance / Focus mode (F11) | ⚪ |  |  |  |
| Theming | Dark / Light / Stage / Color-blind safe themes | ⚪ |  |  |  |
| Motion | Note-on glow with reduce-motion respect | ⚪ |  |  |  |
| Roll | Velocity rendering (alpha or bar), toggleable | ⚪ |  |  |  |
| Config | Consolidated Settings window | ⚪ |  |  |  |
| Diagnostics | Intra-tick ordering proof overlay in Events | ⚪ |  |  |  |
| Feedback | Silent-scrub "rebuilding state…" chip | ⚪ |  |  |  |
| Onboarding | First-run coachmarks for the three views | ⚪ |  |  |  |
| Accessibility | Full audit pass: focus, names, contrast, keyboard | ⚪ |  |  |  |

### Decision Log

| Date | Decision | Reason | Author |
|---|---|---|---|
| 2026-05-04 | Adopted hybrid Option B (DAW-inspired) + Option A (Compact mode) + Option D (Performance mode). | Best fit for spec's piano-roll-first identity while preserving power-user density and stage usability. | Initial plan |
| 2026-05-04 | Move Theme toggle, Diagnostics toggle, Output Device, and "No SysEx" out of the primary toolbar. | Top bar must be transport-first; configuration belongs in Settings or status bar. | Initial plan |
| 2026-05-04 | Single tempo control replaces the duplicated "Tempo" pill + "Tempo / − / +" cluster. | Removes the most visible source of toolbar confusion. | Initial plan |
| 2026-05-04 | Track legend is replaced by a real Track Inspector / Mixer strip. | Spec calls for Mute/Solo and per-channel work; legend cannot deliver it. | Initial plan |

### Change Log

| Date | Change | Linked files / PR |
|---|---|---|
| 2026-05-04 | Plan created. |  |

---

*Maintainers: keep this document the single source of truth for SMF Trace UX
modernization. When implementing a row, change its status, fill the dates,
and add a one-line note pointing to the relevant files or PR.*
