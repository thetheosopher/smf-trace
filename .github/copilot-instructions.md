# SMF Trace — Copilot Instructions (Repo-wide)

## Target / toolchain
- Target framework: net10.0 (Core libs) and net10.0-windows (WPF).
- Use SDK-style projects; solution layout: src/SMFTrace.Core, src/SMFTrace.MidiInterop, src/SMFTrace.Wpf.

## Source of truth (must follow)
- Spec: ./docs/spec/SMF_Trace_Spec_Final.md
- Technical: ./docs/spec/SMF_Trace_Companion_Technical_Final.md
- Branding/UI copy: ./docs/spec/SMF_Trace_Branding_Pro_Icon_Concepts_v2.md

## Non-negotiable behaviors
- Piano roll: left→right scroll, fixed playhead at 33%, 30s default window + zoom controls.
- Lanes: split by (track, channel).
- Diagnostics: show all events; same-tick ordering guarantees Program Change before NoteOn.
- Transport: Pause/Stop => All Notes Off; Stop => reset to 0.
- Seek drag: silent scrub; release: rebuild state and re-emit bank/program/controller state.
- SysEx: send by default + “Disable SysEx output” to suppress sending (display still allowed).
``