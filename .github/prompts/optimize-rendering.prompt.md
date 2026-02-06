---
mode: agent
description: "Optimize piano roll rendering performance"
---

# Optimize Rendering

You are optimizing the piano roll rendering in SMF Trace.

## Performance Targets
- **60 FPS** primary target
- **30 FPS** fallback option (user-selectable via FPS toggle)
- Must handle large orchestral MIDI files (10K+ notes)

## Rendering Architecture
- Use `DrawingVisual` / `DrawingContext` for all rendering
- **NO** per-note `Rectangle` or other UI elements
- Pre-index note spans per lane for fast lookup
- Only render notes in visible time window

## Key Optimizations

### Visible Window Calculation
```
windowStart = playheadTime - (visibleDuration * 0.33)
windowEnd = windowStart + visibleDuration
```
- Playhead is fixed at 33% of view width
- Default visible window: 30 seconds

### Spatial Indexing
- Pre-compute note rectangles grouped by lane
- Use binary search to find notes in visible range
- Cache lane assignments (track, channel) → lane index

### Drawing Batches
- Group draw calls by color/brush
- Reuse Pen/Brush objects (freeze them)
- Minimize geometry recalculation

### Timer Strategy
- `CompositionTarget.Rendering` for 60 FPS
- `DispatcherTimer` at 33ms for 30 FPS fallback

## Profiling
1. Use VS profiler or dotTrace
2. Target: <16ms per frame at 60 FPS
3. Watch for GC pressure from allocations

## User Request
{{input}}
