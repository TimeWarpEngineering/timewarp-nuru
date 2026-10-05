# Round 2 — merged findings
**Date:** 2026-10-05
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-analyzers/validation/route-location-lookup.cs:22
- Description: `Find` tried `OriginalPattern` before `EffectivePattern`, so an endpoint diagnostic could squiggle a fluent `Map` of the same literal. Command routes now resolve only `EffectivePattern`. Fluent routes still prefer the source literal.
- Suggestion: Covered by `Should_anchor_endpoint_nuru_r004_beside_fluent_empty_route`.
- Source: general
- Disposition notes: Fixed in the review loop. Re-verified by generator-49 (10 passed) and generator-30 (5 passed).

## Resolved prior

- M1 was open in round 1. Round 2 confirms it is fixed. No new findings.

## Duplicates / conflicts

- Single reviewer. No overlaps.
