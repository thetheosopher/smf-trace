---
applyTo: "src/SMFTrace.Core/**/*.cs"
---
# Sequencer/Timing rules (SMF Trace)

- Implement intra-tick ordering: Bank Select (CC0/32) -> ProgramChange -> CC -> NoteOn -> NoteOff.
- Pause sends All Notes Off.
- Stop sends All Notes Off + reset position to 0.
- Seek drag = silent scrub (no output). Seek release rebuilds channel state and re-emits bank/program/controller state.
- Gate SysEx transmission behind DisableSysExOutput option.