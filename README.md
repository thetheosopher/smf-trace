# SMF Trace

**See every event. Trust every tick.**

SMF Trace is a Windows desktop workbench for inspecting and playing Standard MIDI Files (*.mid). It provides a high-performance piano roll synchronized to playback and a live, filterable event trace with decoded fields and raw bytes—ideal for validating program/bank changes, controller automation, tempo/meta events, and SysEx routing.

## Features

- **Piano Roll View**: Left-to-right scrolling with a fixed playhead at 33% width, 30-second default visible window, and zoom controls
- **Playlist Workflow**: Replace/append MIDI files, view metadata, and jump entries with previous/next navigation
- **Diagnostics View**: See every event (including meta and SysEx) with filters and decoded details
- **Deterministic Playback**: Verify exactly what is sent, in order, down to raw bytes
- **Transport Controls**: Play/Pause/Stop plus Panic (All Notes Off + Reset Controllers) and loop playback
- **Seek with State Rebuild**: Seek while paused or stopped with automatic channel/instrument state rebuild
- **Tempo & Display Controls**: Signed BPM tempo adjustment, track mute/solo, overlay/compact modes, lyrics lane, and note/key visibility toggles
- **SysEx Routing**: SysEx events are transmitted during playback and remain visible in diagnostics
- **Device Persistence**: Restores the last selected MIDI output device when available

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

## Build Output Layout

Build artifacts are centralized under the repository root `output/` folder:

- `output/bin/<ProjectName>/<Configuration>/<TargetFramework>/...`
- `output/obj/<ProjectName>/<Configuration>/<TargetFramework>/...`

For example, after a Debug build, the WPF app executable is typically at:

- `output/bin/SMFTrace.Wpf/Debug/net10.0-windows/SMFTrace.exe`

To create a source archive that excludes temporary build artifacts (`output/`, `bin/`, `obj/`):

```powershell
$root = (Resolve-Path .).Path; $zip = Join-Path $root 'SMFTrace-source.zip'; if (Test-Path $zip) { Remove-Item $zip -Force }; Get-ChildItem $root -Recurse -File | Where-Object { $_.FullName -notmatch '\\(output|bin|obj)\\' -and $_.Name -ne 'SMFTrace-source.zip' } | Compress-Archive -DestinationPath $zip
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
| `Ctrl+Shift+O` | Add MIDI files to playlist |
| `Ctrl+Shift+P` | Panic (All Notes Off) |
| `Space` | Play / Pause |
| `S` | Stop |
| `Left/Right` | Previous / Next playlist item |
| `L` | Toggle loop playback |
| `+` / `-` | Horizontal zoom in/out |
| `Shift+Plus` / `Shift+Minus` | Vertical zoom in/out |

## Documentation

- [Product Specification](docs/spec/SMF_Trace_Spec_Final.md)
- [Technical Design](docs/spec/SMF_Trace_Companion_Technical_Final.md)
- [Branding & UI Copy](docs/spec/SMF_Trace_Branding_Pro_Icon_Concepts_v2.md)

## License

See [LICENSE](LICENSE) for details.

---

© 2026 SMF Trace
