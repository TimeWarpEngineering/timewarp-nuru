# Round 2 — merged findings
**Date:** 2026-11-09
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: tests/timewarp-nuru-tests/devcli/packable-projects-01-parse.cs
- Description: file-wide `#pragma warning disable IDE1006` silenced the naming rule for `stdout` constants.
- Source: general (round 1)
- Disposition notes: fixed in 9fff2abb — constants are `Stdout`, pragma and comment removed; 21/21 pass standalone.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs
- Description: progress line went through static `Terminal.WriteErrorLine`, bypassing the injected `ITerminal`.
- Source: general (round 1)
- Disposition notes: fixed in 9fff2abb — constructor takes `ITerminal`; generated DI passes `__fw_ITerminal`; fixture asserts the line on `TestTerminal`.

### M3 — Severity: nit — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/services/packable-project-service.cs
- Description: multi-failure `InvalidOperationException` dropped the original exceptions.
- Source: general (round 1)
- Disposition notes: fixed in 9fff2abb — inner `AggregateException(failures)`; asserted in `-03-parallel`.

## New findings

None.

## Duplicates / conflicts

- None (single reviewer).
