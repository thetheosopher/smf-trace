---
name: scaffold-solution
description: Create the SMF Trace .NET solution and projects (Core/MidiInterop/Wpf) plus initial folder structure.
argument-hint: Optional: specify .NET version (e.g., net8.0-windows)
agent: agent
---
You are creating a new solution for the app described in:
- ../docs/spec/SMF_Trace_Spec_Final.md
- ../docs/spec/SMF_Trace_Companion_Technical_Final.md

Task:
1) Propose a solution structure: SMFTrace.Core, SMFTrace.MidiInterop, SMFTrace.Wpf
2) Generate exact dotnet CLI commands to create the sln and projects.
3) Generate the initial folders/files, including:
   - src/SMFTrace.Core/
   - src/SMFTrace.MidiInterop/
   - src/SMFTrace.Wpf/ (WPF app)
4) Add NuGet package recommendations (with versions left unspecified if unknown).
5) Ensure the plan follows the repo instructions and acceptance criteria.
Return:
- Commands
- File tree
- Next 3 milestones