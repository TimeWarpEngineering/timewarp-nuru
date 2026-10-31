# Round 2 — general
**Date:** 2026-10-31
**Scope reviewed:** Fix commit `b021b79a` since round-1 base `12da89d2`. Product files only: `benchmarks/timewarp-nuru-benchmarks/program.cs` and `changelog.md`. Re-checked M1 and M2 against that delta.

## Summary

The fix renames the benchmark toolchain to .NET 11 and updates the Unreleased Neutral contracts sentence to `TimeWarp.Contracts` 2.0.0-beta.29. Both match the round-1 suggestions. `Directory.Packages.props` still pins `TimeWarp.Contracts` at 2.0.0-beta.29, and the .NET 11 changelog bullet already named that version. No new defect on the delta.

## Resolved prior

### M1 — Severity: nit — Status: fixed
- File: benchmarks/timewarp-nuru-benchmarks/program.cs:31
- Description: The toolchain moniker, `NetCoreAppSettings.Name`, the comment, and the variable now all say .NET 11. `name` is `.NET 11.0`, the comment is ".NET 11", the variable is `net11Toolchain`, and `WithToolchain` uses that variable. No `net10Toolchain` or `.NET 10.0` label remains in the file.
- Disposition notes: Confirmed on the fix diff. The benchmark project was not rebuilt; Results already records that it fails for a pre-existing CliFx reason, and this change is a label rename.

### M2 — Severity: nit — Status: fixed
- File: changelog.md:57
- Description: The current reference is `TimeWarp.Contracts` 2.0.0-beta.29. The parenthetical history is `2.0.0-beta.24 before 481, 2.0.0-beta.23 before 518, 2.0.0-beta.21 before 508-014`. That matches the package pin and the .NET 11 bullet.
- Disposition notes: Confirmed on the fix diff.

## Issues

<!-- No new findings. -->
