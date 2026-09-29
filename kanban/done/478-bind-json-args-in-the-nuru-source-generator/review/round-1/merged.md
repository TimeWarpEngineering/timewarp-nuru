# Round 1 — merged findings
**Date:** 2026-09-29
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 2 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:338
- Description: Absent `--json-args` keys initialize non-list locals to `default!`, so an endpoint option's property default (`DefaultValueLiteral`) is wiped. The matcher keeps that literal, and the recommendation says an absent key leaves the existing default.
- Suggestion: Initialize from `DefaultValueLiteral` or `ParameterDefinition.DefaultValue` for non-flag, non-list slots.
- Source: general
- Disposition notes:

### M2 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:491
- Description: A missing required positional with a later reserved literal forces `__litOk = false` before that literal is tested. `promote now --json-args '{}'` on `promote {env} now` becomes "Unknown command" instead of a missing-`env` error.
- Suggestion: Keep `__litOk` true until a later literal actually fails.
- Source: general
- Disposition notes:

### M3 — Severity: suggestion — Status: open
- File: source/timewarp-nuru-analyzers/generators/emitters/json-args-emitter.cs:901
- Description: Argv conversion failures are reported with the JSON type-mismatch sentence.
- Suggestion: Use an argv invalid-value sentence that names the key and the expected type and does not echo the value.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
