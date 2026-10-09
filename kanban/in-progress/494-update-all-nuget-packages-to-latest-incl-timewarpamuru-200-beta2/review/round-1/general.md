# Round 1 — general
**Date:** 2026-10-09
**Scope reviewed:** branch vs master: `Directory.Packages.props`, `tests/timewarp-nuru-tests/repl/repl-10-error-handling.cs`, task.md

## Summary

Pin-only change plus a test assertion fix for Shouldly 5 case-sensitive `ShouldContain`. Verified: the repo uses no removed Amuru 1.x Git `*Master*` helpers or bool-returning Git calls; the `WithStandardInput` call sites (`source/timewarp-nuru/repl/input/repl-console-reader.clipboard.cs`, `routing-33-json-args.cs`) pass real text. `repl-10` is in CI via the `../timewarp-nuru-tests/**/*.cs` glob, so CI covers the assertion change. The kept pins (Roslyn 5.6.0, MSBuild 18.9.6, .NET 10 line, System.CommandLine) have reasons recorded in Results. Low risk.

## Issues

### Issue 1 — Severity: suggestion
- File: Directory.Packages.props:26
- Description: `TimeWarp.Nuru` and `TimeWarp.Nuru.Search` take a `PackageReference` on `TimeWarp.Amuru`. The shipped nupkgs now depend on the prerelease `2.0.0-beta.2`. That's fine while Nuru is `3.0.0-beta.79`. Packing a stable `3.0.0` against a prerelease dependency raises NU5104, which is an error under warnings-as-errors.
- Suggestion: Record a release gate: stable Nuru 3.0.0 needs Amuru 2.0.0 stable first, or a pin decision at release time.
- Status: open
