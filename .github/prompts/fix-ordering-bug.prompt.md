---
mode: agent
description: "Debug and fix intra-tick event ordering issues"
---

# Fix Ordering Bug

You are debugging an intra-tick ordering issue in SMF Trace.

## Required Order (LOCKED)
Events at the same tick MUST be ordered as follows:
1. **Bank Select** (CC0 MSB, CC32 LSB)
2. **Program Change**
3. **Other CC** (especially those affecting note onset)
4. **NoteOn**
5. **NoteOff**

## Key Files
- `src/SMFTrace.Core/Ordering/IntraTickComparer.cs` - The comparer implementing this order
- `src/SMFTrace.Core/Ordering/EventSorter.cs` - Applies sorting to flattened timeline

## Debugging Steps
1. Identify the specific events and their tick positions
2. Check if the comparer correctly assigns priority values
3. Verify stable sort is used (preserve relative order for equal priorities)
4. Check for edge cases:
   - Multiple CCs at same tick
   - Bank Select followed immediately by Program Change
   - Running status parsing artifacts

## Common Issues
- CC0 and CC32 not recognized as Bank Select
- Program Change not prioritized over NoteOn
- Sort not stable causing non-deterministic output

## Testing
After fix, run:
```powershell
dotnet test --filter "FullyQualifiedName~Ordering"
```

## User Request
{{input}}
