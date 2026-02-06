# SMF Trace

**See every event. Trust every tick.**

SMF Trace is a Windows desktop workbench for inspecting and playing Standard MIDI Files (*.mid). It provides a high-performance piano roll synchronized to playback and a live, filterable event trace with decoded fields and raw bytes—ideal for validating program/bank changes, controller automation, tempo/meta events, and SysEx routing.

## Features

- **Piano Roll View**: Left-to-right scrolling with a fixed playhead at 33% width, 30-second default visible window, and zoom controls
- **Diagnostics View**: See every event (including meta and SysEx) with filters and decoded details
- **Deterministic Playback**: Verify exactly what is sent, in order, down to raw bytes
- **Seek with State Rebuild**: Silent scrubbing with automatic bank/program/controller state restoration
- **SysEx Safety**: Optional "Disable SysEx output" to prevent device reconfiguration while still allowing inspection
- **Hot-plug Device Support**: Automatic detection of connected MIDI output devices

## Requirements

- Windows 10 or later
- .NET 10.0 SDK

## Building

```powershell
# Clone the repository
git clone https://github.com/your-org/smf-trace.git
cd smf-trace

# Build the solution
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/SMFTrace.Wpf
```

## Project Structure

```
SMF Trace/
├── src/
│   ├── SMFTrace.Core/           # Core logic, models, sequencer (net10.0)
│   ├── SMFTrace.MidiInterop/    # MIDI device I/O, P/Invoke (net10.0)
│   └── SMFTrace.Wpf/            # WPF application (net10.0-windows)
├── tests/
│   ├── SMFTrace.Core.Tests/     # Core unit tests
│   └── SMFTrace.MidiInterop.Tests/
├── docs/
│   └── spec/                    # Specification documents
└── .github/
    ├── copilot-instructions.md  # Repo-wide Copilot rules
    ├── instructions/            # Path-specific instruction files
    └── prompts/                 # Slash-command prompt files
```

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open MIDI file |
| `Space` | Play / Pause |
| `S` | Stop |
| `Home` | Seek to start |
| `Left/Right` | Nudge seek ±1s |
| `Shift+Left/Right` | Nudge seek ±5s |
| `Ctrl++` / `Ctrl+-` | Zoom in/out |

## Documentation

- [Product Specification](docs/spec/SMF_Trace_Spec_Final.md)
- [Technical Design](docs/spec/SMF_Trace_Companion_Technical_Final.md)
- [Branding & UI Copy](docs/spec/SMF_Trace_Branding_Pro_Icon_Concepts_v2.md)

## License

See [LICENSE](LICENSE) for details.

---

© 2026 SMF Trace
