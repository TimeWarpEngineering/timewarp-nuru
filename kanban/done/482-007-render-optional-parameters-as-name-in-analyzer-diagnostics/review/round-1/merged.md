# Round 1 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: source/timewarp-nuru-analyzers/validation/route-location-lookup.cs:18
- Description: `Find` tries `OriginalPattern` before `EffectivePattern`. Endpoint locations are stored under `EffectivePattern`. A fluent `Map` of the same literal (`Map("")` vs `[NuruRoute("")]` with `{name?}`, or `Map("list")` vs `[NuruRoute("list")]` plus a parameter) wins the source-form key, so the endpoint diagnostic squiggles the fluent call.
- Suggestion: Command routes resolve only `EffectivePattern`. Fluent routes keep `OriginalPattern`, then `FullPattern`, then `EffectivePattern`. Cover it with a NURU_R004 test whose spans include both the `Map("")` literal and the `[NuruRoute("")]` attribute.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- Single reviewer. No overlaps.
