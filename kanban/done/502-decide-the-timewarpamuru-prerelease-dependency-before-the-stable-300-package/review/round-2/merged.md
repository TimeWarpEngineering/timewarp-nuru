# Round 2 — merged findings
**Date:** 2026-10-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 1 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 1 | 0 | 0 |

## Resolved prior (round 1)

### M1 — Severity: bug — Status: fixed
- The claim that every beta through beta.80 used Amuru 2.0.0-beta.2 was removed from all five locations (06acf71a).
- The replacement wording has a new error, tracked as M3.

### M2 — Severity: suggestion — Status: fixed
- `migrating-to-3.0.md:49-51` now says that Nuru 2.0.0 had no Amuru dependency. The 2.0.0 and 2.1.0-beta.32 nuspecs confirm this.

## Issues

### M3 — Severity: bug — Status: open
- File: changelog.md:13; documentation/user/guides/migrating-to-3.0.md:51-52; 487 release-notes.md:54-55;
  487 task.md:66; 502 task.md:38, 88, 129
- Description: The text says "earlier 3.0 betas depend on Amuru 1.x". Per the nuget.org nuspecs, 3.0.0-beta.2 through
  beta.57 have no Amuru dependency. Only beta.58–79 depend on 1.x. Round 1's suggested wording caused this.
- Suggestion: Write "3.0.0-beta.58 through beta.79 depend on Amuru 1.x; earlier betas have no Amuru dependency." Or
  remove the beta history from the guide and the release notes and keep it only in the changelog.
- Source: general (Issue 1)
- Disposition notes:

### M4 — Severity: nit — Status: open
- File: documentation/user/guides/migrating-to-3.0.md:53
- Description: The line is 130 characters. The rest of the guide wraps at about 80 columns.
- Suggestion: Rewrap the paragraph in the same edit as M3.
- Source: general (Issue 2)
- Disposition notes:

## Duplicates / conflicts

- None. There was one reviewer. M3 is a regression from M1's fix, so it gets a new ID instead of reopening M1.
