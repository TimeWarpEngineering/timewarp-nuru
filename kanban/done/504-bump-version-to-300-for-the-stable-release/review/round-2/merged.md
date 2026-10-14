# Round 2 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 1 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: documentation/user/guides/migrating-to-3.0.md:177
- Description: (round 1) The changelog pointer misstated which betas have sections.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2 against the changelog headings and local tags. The guide's
  range (a section per published beta through beta.79; `[3.0.0]` = changes since beta.79) is accurate.

### M2 — Severity: nit — Status: fixed
- File: changelog.md:12
- Description: (round 1) The `[3.0.0]` lead had an ambiguous Packaging pointer and did not state its range.
- Source: general (round 1)
- Disposition notes: Re-verified in round 2. The lead states the range since beta.79 and names the first
  Packaging entry under Fixed (503), which is changelog.md:21.

No new findings on the fix delta.

## Duplicates / conflicts

- None (single reviewer).
