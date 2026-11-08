# Round 1 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 1 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs:6
- Description: File-wide `#pragma warning disable IDE1006` added only so 17 local constants named
  `stdout` pass the naming rule standalone. It silences IDE1006 instead of fixing the names; there is
  no external naming contract here.
- Suggestion: Rename the constants (e.g. `Stdout`) and their uses; remove the pragma and its comment.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:93
- Description: Progress is written through the static `Terminal.WriteErrorLine`, bypassing the app's
  injected `ITerminal` that every DevCli endpoint uses. Endpoint tests with `TestTerminal` (for example
  `check-version-07-endpoint-fail-closed.cs`) leak the line to real stderr and cannot observe it.
- Suggestion: Inject `ITerminal` into `PackableProjectService` and pass `terminal.WriteErrorLine` to
  `EvaluateProjectsAsync` (update `new PackableProjectService()` test call sites), or report progress
  from the calling handlers through their `ITerminal`.
- Source: general
- Disposition notes:

### M3 — Severity: nit — Status: open
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs:181
- Description: The multi-failure `InvalidOperationException` drops the original exceptions.
- Suggestion: Pass `new AggregateException(failures)` as the inner exception.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
