# Round 1 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 0 | 0 |
| suggestion | 1 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: open
- File: changelog.md:13; documentation/user/guides/migrating-to-3.0.md:48-49;
  kanban/to-do/487-*/release-notes.md:54-55; kanban/to-do/487-*/task.md:66 (and the 502 `task.md` Results)
- Description: The docs say every published 3.0 beta through beta.80 depends on Amuru 2.0.0-beta.2. Only
  beta.80 does. Per nuget.org nuspecs, beta.79 depends on `1.1.1`, beta.72–76 on `1.0.0`, beta.58–71 on
  `1.0.0-beta.17`…`1.0.0-beta.32`, and beta.53–57 on no Amuru at all.
- Suggestion: "3.0.0-beta.80 depends on `TimeWarp.Amuru` 2.0.0-beta.2; earlier 3.0 betas depend on Amuru 1.x."
  Make the edit in all four files and in the 502 Results.
- Source: general
- Disposition notes:

### M2 — Severity: suggestion — Status: open
- File: documentation/user/guides/migrating-to-3.0.md:46-54
- Description: Nuru 2.0.0 has no Amuru dependency. For the guide's 2.x audience, Amuru is a new transitive
  dependency in 3.0, and the guide does not say so.
- Suggestion: Add one sentence that 2.x did not depend on Amuru. Fold it into the M1 rewrite.
- Source: general
- Disposition notes:

## Duplicates / conflicts

- None. There was one reviewer.
