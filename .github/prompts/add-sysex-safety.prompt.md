---
name: add-sysex-safety
description: Implement the Disable SysEx output setting in UI, options model, and sequencer dispatch gate.
agent: agent
---
Implement the SysEx safety requirement from the spec:
- Default: send SysEx
- Option: Disable SysEx output (blocks transmission, still visible in diagnostics)

Provide:
- UI binding (WPF)
- Settings persistence
- Core PlaybackOptions flag
- Sequencer dispatch gating pseudocode and file edits