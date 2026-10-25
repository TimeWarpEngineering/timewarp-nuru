# Round 2 — merged findings
**Date:** 2026-10-26
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/generators/emitters/logger-expressions.cs:48
- Description: Round 1 asked for an exact `ILoggerFactory` test in `Resolve` and in the NURU_H007 skip, plus a generator-67 case for `ILogger<ILoggerFactory>`. `IsLoggerFactoryType` is that test at both call sites (`logger-expressions.cs:77`, `nuru-generator.cs:308`). The two new tests expect `CreateLogger<global::Microsoft.Extensions.Logging.ILoggerFactory>()` and, with logging off, one `NURU_H007`.
- Suggestion: Landed in `3b5a6504`.
- Source: general
- Disposition notes: Fixed on the M1 delta. Fully qualified `ILogger<T>` ends in `>`, so the old suffix test already left this category on `CreateLogger<T>()`; the exact-name check is what both call sites use now, and generator-67 guards that output.

## Duplicates / conflicts

- Single reviewer. M1 is round-1 general issue 1, carried forward as fixed. No new findings.
