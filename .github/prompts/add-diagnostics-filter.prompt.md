---
mode: agent
description: "Add new filter type to Diagnostics view"
---

# Add Diagnostics Filter

You are adding a new filter to the Diagnostics view in SMF Trace.

## Existing Filter Architecture
- Filters are defined in `DiagnosticsViewModel`
- Filter state is exposed via observable properties
- `FilteredEvents` computed collection applies all active filters
- UI bindings in `DiagnosticsView.xaml`

## Current Filters (Day 1)
- Message type filter (multi-select)
- Meta-only toggle
- SysEx show/hide toggle

## Implementation Checklist
1. Add filter property to `DiagnosticsViewModel`:
   ```csharp
   [ObservableProperty]
   private bool _myNewFilter;
   ```
2. Update `FilteredEvents` logic to respect new filter
3. Add UI control in `DiagnosticsView.xaml` toolbar
4. Ensure filter changes trigger collection refresh

## Auto-Scroll Behavior
- Filters MUST NOT affect auto-scroll state
- Auto-scroll disabled by manual scroll
- Auto-scroll re-enabled by click anywhere in list

## Performance Requirements
- Filter evaluation must be O(1) per event
- Avoid re-fetching or re-parsing events
- Batch UI updates when filter changes

## User Request
{{input}}
