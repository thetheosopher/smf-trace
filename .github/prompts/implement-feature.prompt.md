---
mode: agent
description: "Implement a new feature slice following SMF Trace spec and conventions"
---

# Implement Feature

You are implementing a new feature for SMF Trace. Follow these guidelines:

## Context
- Read the spec files in `docs/spec/` before implementation
- Follow the instruction files in `.github/instructions/`
- SMF Trace is a MIDI file player with piano roll and diagnostics views

## Checklist
Before writing code:
1. Identify which project(s) the feature belongs to (Core, MidiInterop, or Wpf)
2. Check if the feature touches ordering logic (must follow Bank→PC→CC→NoteOn→NoteOff)
3. Determine if transport state is affected (Pause=AllNotesOff, Stop=AllNotesOff+reset)
4. Verify SysEx handling respects `DisableSysExOutput` option

## Implementation Steps
1. Create/update models in `SMFTrace.Core.Models`
2. Implement business logic in appropriate Core namespace
3. Add MidiInterop integration if device I/O is needed
4. Create ViewModels with `[ObservableProperty]` and `[RelayCommand]` attributes
5. Build Views using MVVM bindings (no code-behind logic)
6. Add unit tests covering edge cases

## Rendering Rules (if UI)
- Piano roll: Use `DrawingVisual`, not per-note UI elements
- Diagnostics: Virtualize list, batch append updates
- Maintain 60 FPS target (or 30 FPS fallback)

## User Request
{{input}}
