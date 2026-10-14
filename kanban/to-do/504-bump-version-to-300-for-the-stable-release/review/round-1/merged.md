# Round 1 — merged findings
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
- Description: The rewritten "What did not move" paragraph says changes after beta.19 are under
  `## [3.0.0]` and that per-beta notes for beta.20–beta.77 were not kept. `changelog.md` has per-beta
  sections for beta.20 through beta.79 (backfilled d7c53758, 2026-10-05); `[3.0.0]` holds only changes
  since beta.79.
- Suggestion: State that the changelog has a section per published 3.0 beta through beta.79, that
  `## [3.0.0]` lists changes since beta.79 (including beta.80's), and that later work is under
  `## [Unreleased]`. Drop the "were not kept" sentence.
- Source: general
- Disposition notes: The "What did not move" pointer now says `changelog.md` has a section for each published 3.0 beta through `3.0.0-beta.79`, that `## [3.0.0]` lists changes since beta.79 (including beta.80), and that later work is under `## [Unreleased]`. The "were not kept" sentence is gone.

### M2 — Severity: nit — Status: fixed
- File: changelog.md:12
- Description: Lead says "see the Packaging entry" but `[3.0.0]` has two Packaging entries; it also
  does not state its range the way `[3.0.0-beta.79]` does.
- Suggestion: Point at the first Packaging entry (503) and open with "Changes since `3.0.0-beta.79`;
  beta.80 has no separate section." Optional.
- Source: general
- Disposition notes: The `[3.0.0]` lead opens with "Changes since `3.0.0-beta.79`; beta.80 has no separate section." and points at the first Packaging entry under Fixed (task 503).

## Duplicates / conflicts

- None (single reviewer).
