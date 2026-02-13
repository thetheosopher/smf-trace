---
applyTo: "**/*.xaml,**/*.cs"
---
# WPF/UI conventions (SMF Trace)

- Use MVVM (ViewModels + Commands); keep rendering in a dedicated custom control.
- Piano roll must render via DrawingContext/DrawingVisual; do not create per-note Rectangle UI elements.
- Provide toggles: Tempo display, Bars/Beats grid, Render FPS 60, Disable SysEx output.
- Diagnostics list must be virtualized; append events in batches; keep UI responsive.

