# Round 2 — merged findings
**Date:** 2026-10-23
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: readme.md:1
- Description: The relative hero `src` could not load on the NuGet package page, because the root readme is packed as `PackageReadmeFile`.
- Suggestion: Use the absolute raw URL on master.
- Source: general (round 1)
- Disposition notes: Fixed in ab4f6f80 and re-verified in round 2. `src` is `https://raw.githubusercontent.com/TimeWarpEngineering/timewarp-nuru/master/assets/mascot/nuru-hero.png`. Alt text and width are unchanged.

No new findings.

## Duplicates / conflicts

- None (single reviewer).
