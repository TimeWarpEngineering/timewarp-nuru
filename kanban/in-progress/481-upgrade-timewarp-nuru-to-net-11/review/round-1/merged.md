# Round 1 — merged findings
**Date:** 2026-10-31
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 2 | 0 |

## Issues

### M1 — Severity: nit — Status: fixed
- File: benchmarks/timewarp-nuru-benchmarks/program.cs:31
- Description: The toolchain moniker is `net11.0`, but `NetCoreAppSettings.Name` is still `.NET 10.0`. The comment on line 26 still says ".NET 10" and the variable is still `net10Toolchain`. BenchmarkDotNet prints `Name` as the job label, so a run reports .NET 10 while it compiles `net11.0`.
- Suggestion: Set `name` to `.NET 11.0` and rename the comment and the variable to match.
- Source: general
- Disposition notes: Fixed in fix pass 2. The toolchain `name` is `.NET 11.0`, the comment says .NET 11, and the variable is `net11Toolchain`.

### M2 — Severity: nit — Status: fixed
- File: changelog.md:57
- Description: The Unreleased "Neutral contracts" bullet still says the reference is `TimeWarp.Contracts` 2.0.0-beta.24. This commit moved that package to 2.0.0-beta.29, and the new .NET 11 bullet on line 52 says beta.29. That older sentence is the one this changelog updates when the pin moves (it already records beta.21, then beta.23, then beta.24).
- Suggestion: Change the current reference in that sentence to 2.0.0-beta.29 and keep the earlier versions in the parenthetical history.
- Source: general
- Disposition notes: Fixed in fix pass 2. The sentence says 2.0.0-beta.29 and the history now reads `2.0.0-beta.24 before 481, 2.0.0-beta.23 before 518, 2.0.0-beta.21 before 508-014`.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse.
