# Round 1 — merged findings
**Date:** 2026-09-28
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/validation/repl-default-route-validator.cs:56
- Description: `IsTopLevelDefaultRoute` flags every top-level `""` pattern. A `[NuruRoute("")]` with a required positional parameter does not match `routeArgs.Length == 0`; the matcher skips it, and `AutoStartWhenEmpty` only intercepts that empty list. The diagnostic is a false Error and claims the route is unreachable. Required options have the same skip. Catch-all parameters can bind empty and still conflict. Optional parameters still match length 0 and should keep NURU_R004.
- Suggestion: Require that the top-level `""` route can match an empty argument list (no required non-catch-all parameter, no required option). Cover the required-parameter negative and the optional-parameter positive in generator-49.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer).
