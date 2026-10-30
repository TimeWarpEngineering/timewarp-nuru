# Round 2 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:571
- Description: The walk diff passes `--no-renames`, so both sides of a rename are in the path list. Verified with git 2.53: a `source/a.cs` to `kanban/a.cs` move lists both paths with `--no-renames`, including when `diff.renames=copies`. `FindShippingPath` then sees `source/a.cs`.
- Suggestion: none remaining
- Source: general
- Disposition notes: fixed in the fix pass (`61e90dc4`). Re-verified in round 2 against the fix delta. Stays fixed.

### M2 — Severity: suggestion — Status: fixed
- File: source/timewarp-nuru-devcli/content/any/endpoints/release-command.cs:410
- Description: A failed `git show` of the target props prints stderr and exits. A null target version says the props file is unreadable. The version-mismatch sentence is only used when both versions were read and differ.
- Suggestion: none remaining
- Source: general
- Disposition notes: fixed in the fix pass (`61e90dc4`). Re-verified in round 2 against `CheckTargetVersion` and the guard-matrix assertions. Stays fixed.

## Duplicates / conflicts

- No new findings. M1 and M2 carried forward from round 1 as fixed.
