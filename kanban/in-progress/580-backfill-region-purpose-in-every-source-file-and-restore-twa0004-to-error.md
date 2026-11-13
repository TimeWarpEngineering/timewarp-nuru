# Backfill #region Purpose in every source file and restore TWA0004 to error

## Description

TimeWarp.SourceGenerators 1.0.0-beta.15 (adopted in task 579) ships TWA0004: every C# file needs a
`#region Purpose` block. 290 of 366 files under `source/` have none, so 579 set
`dotnet_diagnostic.TWA0004.severity = none` in `.editorconfig`. This task backfills the regions
and removes that override so the package default (error) applies.

## Requirements

- Every `.cs` file under `source/`, `tools/dev-cli/`, and `tests/` carries a `#region Purpose`
  block (a single `//` line suffices) that says what the file is for; follow the tw-agent-context-regions
  skill (Purpose, optional Design, optional Open Questions). Generated files and `global-usings.cs`
  may use `generated_code = true` in `.editorconfig` instead, as timewarp-architecture does.
- Remove the TWA0004 override added by 579 from `.editorconfig`.
- `dev build` clean. Reference: timewarp-state task 099 did this backfill.

## Checklist

- [ ] Purpose regions added (source/)
- [ ] Purpose regions added (tools/dev-cli/, tests/)
- [ ] TWA0004 override removed
- [ ] Build clean

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-13)

## Notes

Follow-up of 579.
