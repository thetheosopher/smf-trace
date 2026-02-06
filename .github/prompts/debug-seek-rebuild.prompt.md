---
mode: agent
description: "Debug seek and state rebuild issues"
---

# Debug Seek Rebuild

You are debugging seek and state rebuild issues in SMF Trace.

## Seek Behavior (LOCKED)
1. **Drag** = silent scrub (update position, NO MIDI output)
2. **Release**:
   - Send `AllNotesOff` on all channels
   - Rebuild `ChannelState` snapshot at target tick
   - Replay accumulated state events up to target
   - Emit bank/program/CC state to device
   - Resume playback only if was playing before drag

## Key Components
- `SequencerEngine` - Manages playback and seek
- `StateSnapshotBuilder` - Creates periodic checkpoints
- `ChannelState` - Per-channel bank/program/CC state

## Checkpoint Strategy
- Checkpoints every ~960 ticks (1 measure at 4/4, 480 PPQ)
- Each checkpoint stores complete state for all 16 channels
- Seek finds nearest prior checkpoint, then replays forward

## State Rebuild Steps
1. Find nearest checkpoint before target tick
2. Start with checkpoint's `ChannelState[]`
3. Iterate events from checkpoint to target tick
4. For each relevant event, update channel state:
   - CC0 → `BankMsb`
   - CC32 → `BankLsb`
   - Program Change → `Program`, set `HasProgramChange = true`
   - Other CCs → add to `Controllers` dictionary

## Emit State to Device
After rebuild, send to MIDI output:
```csharp
for (channel = 0; channel < 16; channel++)
{
    if (state.HasProgramChange)
    {
        SendCC(channel, 0, state.BankMsb);
        SendCC(channel, 32, state.BankLsb);
        SendProgramChange(channel, state.Program);
    }
    foreach (var (cc, value) in state.Controllers)
    {
        SendCC(channel, cc, value);
    }
}
```

## Common Issues
- Checkpoint not found (seek to before first checkpoint)
- State not cleared before replay
- Bank/Program order incorrect during emit
- AllNotesOff sent AFTER state emit (should be before)

## User Request
{{input}}
