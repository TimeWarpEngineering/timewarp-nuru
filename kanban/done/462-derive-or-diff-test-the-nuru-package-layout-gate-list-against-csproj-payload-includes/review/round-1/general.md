# Round 1 — general
**Date:** 2026-09-24
**Scope reviewed:** branch vs master — `NupkgLayoutCheck.NuruRequiredPackageEntries` + `ResolvePublishedEntry`, workflow-command wiring, csproj comments, `nupkg-layout-02-csproj-gate-parity`, CI multi-mode include path

## Summary

Implementation matches the task: the gate list moved to `NupkgLayoutCheck` (still hand-maintained), three `analyzers/dotnet/cs` entries were added, and a bidirectional parity test parses Pack=`true` None items under build/ and analyzers/. `ResolvePublishedEntry` correctly treats TFM segments and file PackagePaths (including the targets rename). Release path still only calls `FindMissing` with the static array — no csproj parsing. Re-ran nupkg-layout-01 (4 passed) and nupkg-layout-02 (3 passed). No bugs, suggestions, or nits.

## Issues

<!-- none -->
