---
mode: agent
description: "Add xUnit tests with proper edge case coverage"
---

# Add Unit Tests

You are adding unit tests to SMF Trace. Follow these guidelines:

## Testing Framework
- Use xUnit with `[Fact]` and `[Theory]` attributes
- Place tests in `tests/SMFTrace.Core.Tests` or `tests/SMFTrace.MidiInterop.Tests`
- Follow Arrange-Act-Assert pattern

## Required Edge Cases
Always test these MIDI edge cases when applicable:

### Running Status
- Messages using running status (status byte omitted)
- Verify DryWetMIDI handles this correctly

### NoteOn Velocity=0
- Must be treated identically to NoteOff
- Test note pairing logic

### Overlapping NoteOn
- Same (channel, note) with a second NoteOn
- Previous note must close immediately

### Same-Tick Ordering
- Bank Select (CC0/32) before Program Change
- Program Change before NoteOn
- CC affecting onset before NoteOn
- NoteOn before NoteOff

### Tempo Changes
- Multiple Set Tempo events
- Tick-to-time conversion accuracy

### Seek/State Rebuild
- State matches expected after seek
- Bank/program/controller state restored correctly

## Test Structure
```csharp
public class [ComponentName]Tests
{
    [Fact]
    public void MethodName_Scenario_ExpectedBehavior()
    {
        // Arrange

        // Act

        // Assert
    }

    [Theory]
    [InlineData(...)]
    public void MethodName_WithVariousInputs_ExpectedBehavior(...)
    {
    }
}
```

## User Request
{{input}}
