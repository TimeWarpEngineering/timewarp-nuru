# Round 1 — general
**Date:** 2026-10-31
**Scope reviewed:** Product commit `c97f4fb5` against `## Goal (implementer restatement)` and the original task. TFMs, packaging paths, layout gate, CI SDK install, CPM and tool bumps, CA1307 call sites, the Mediator 14 holdback, and the docs this commit edited.

## Summary

The repo now targets `net11.0`, `global.json` resolves to `11.0.100-rc.3.26522.148` with `allowPrerelease: true`, and CI installs `11.0.x` at preview quality. Runtime packages are on `11.0.0-rc.3.26522.148`, `Microsoft.Build.Utilities.Core` 18.11.0 still lists the Framework DLL and the other net11.0 dependencies the nupkg packs, and `TimeWarp.Contracts` 2.0.0-beta.29 still depends on `TimeWarp.Mediator.Contracts` 14.0.0 while Mediator 15 drops `ICommandHandler` / `IQueryHandler` and returns `ValueTask`. Holding Mediator at 14 matches the task's holdback clause and the no-API-redesign rule. `timewarp-nuru-parsing` and `timewarp-nuru-analyzers` build with 0 warnings. Two labels this commit touched are still on the old version.

## Issues

### Issue 1 — Severity: nit
- File: benchmarks/timewarp-nuru-benchmarks/program.cs:31
- Description: The toolchain moniker is `net11.0`, but `NetCoreAppSettings.Name` is still `.NET 10.0`. The comment on line 26 still says ".NET 10" and the variable is still `net10Toolchain`. BenchmarkDotNet prints `Name` as the job label, so a run reports .NET 10 while it compiles `net11.0`.
- Suggestion: Set `name` to `.NET 11.0` and rename the comment and the variable to match.
- Status: open

### Issue 2 — Severity: nit
- File: changelog.md:57
- Description: The Unreleased "Neutral contracts" bullet still says the reference is `TimeWarp.Contracts` 2.0.0-beta.24. This commit moved that package to 2.0.0-beta.29, and the new .NET 11 bullet on line 52 says beta.29. That older sentence is the one this changelog updates when the pin moves (it already records beta.21, then beta.23, then beta.24).
- Suggestion: Change the current reference in that sentence to 2.0.0-beta.29 and keep the earlier versions in the parenthetical history.
- Status: open
