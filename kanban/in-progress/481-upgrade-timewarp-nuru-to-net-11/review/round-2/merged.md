# Round 2 — merged findings
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
- Description: The toolchain moniker, `NetCoreAppSettings.Name`, the comment, and the variable now all say .NET 11. `name` is `.NET 11.0`, the comment is ".NET 11", the variable is `net11Toolchain`, and `WithToolchain` uses that variable.
- Suggestion: Set `name` to `.NET 11.0` and rename the comment and the variable to match.
- Source: general
- Disposition notes: Confirmed fixed in round 2 against commit `b021b79a`.

### M2 — Severity: nit — Status: fixed
- File: changelog.md:57
- Description: The Unreleased Neutral contracts sentence now says the reference is `TimeWarp.Contracts` 2.0.0-beta.29, with `2.0.0-beta.24 before 481` kept in the history. The package pin is 2.0.0-beta.29.
- Suggestion: Change the current reference in that sentence to 2.0.0-beta.29 and keep the earlier versions in the parenthetical history.
- Source: general
- Disposition notes: Confirmed fixed in round 2 against commit `b021b79a`.

## Duplicates / conflicts

- Single reviewer. Nothing to collapse. No new findings on the fix delta.
