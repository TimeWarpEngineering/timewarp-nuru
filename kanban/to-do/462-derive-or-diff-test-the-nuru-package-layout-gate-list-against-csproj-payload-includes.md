# Derive or diff-test the Nuru package layout gate list against csproj payload includes

## Description

[Brief description of the task]

## Checklist

- [ ] Item 1
- [ ] Item 2

## Notes

[Additional context]

## Description

Follow-up from 461 review (verdict clean, one suggestion). The required-payload list for the
TimeWarp.Nuru nupkg is hand-duplicated in two places, synced only by a comment:

- `source/timewarp-nuru/timewarp-nuru.csproj` — explicit `None Include` pack entries
- `tools/dev-cli/endpoints/workflow-command.cs` — `NuruRequiredPackageEntries` (layout gate)

If a future timewarp-nuru-build dependency is added to one list but not the other, the gate
silently stops covering the new file (FindMissing only checks entries in its own list).

## Requirements

- [ ] Single source of truth: either derive the gate list from the csproj pack items at gate
      time, or add a test that parses the csproj `Build Task Packaging` ItemGroup and diffs it
      against `NuruRequiredPackageEntries`, failing on any mismatch
- [ ] Keep the gate fail-closed semantics from 461 intact

## Notes

- Origin: kanban 461 review round 1 (2026-08-10), non-blocking suggestion.
