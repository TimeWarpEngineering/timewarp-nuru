# Round 2 — merged findings
**Date:** 2026-09-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/validation/repl-default-route-validator.cs:59
- Description: `IsTopLevelDefaultRoute` flagged every top-level `""` pattern, including a `[NuruRoute("")]` whose required positional parameter does not match `routeArgs.Length == 0`.
- Suggestion: Report NURU_R004 only when that route can match an empty argument list.
- Source: general
- Disposition notes: Fixed on this id. The predicate excludes required non-catch-all parameters and required options. generator-49: 9 passed, 0 failed (required-parameter negative, optional-parameter positive, plus the original seven cases).

## Resolved prior

- M1 fixed. No new findings.

## Duplicates / conflicts

- None (single reviewer).
