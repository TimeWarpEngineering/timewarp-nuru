# Round 2 — merged findings
**Date:** 2026-11-14
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 1 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Resolved prior

### M1 — Severity: bug — Status: wontfix
- File: source/timewarp-nuru-analyzers/generators/emitters/type-conversion-map.cs:117
- Description: Round 1 reported task 586's `{name:string}` binding in the diff against `master`. Fix pass 1 marked it wontfix: the task stacks on `feature/generator-fixes-2026-11-14`, and that base already contains PR #405.
- Suggestion: (round 1) Replay `b6c50b72` onto `master`. Not applied.
- Source: general
- Disposition notes: Confirmed in round 2 (2026-11-14). `git merge-base origin/feature/generator-fixes-2026-11-14 HEAD` is `75b4475f`. `git diff --name-only origin/feature/generator-fixes-2026-11-14...HEAD` has the 583 value-kind files, generator-75, routing-39, and this kitchen. It has no `type-conversion-map.cs`, no `routing-38-string-type-constraint.cs`, and no other 586 product file. `bec98676` did not change product code. Replaying onto `master` would still break the work-set. 586 reaches `master` through the work-set's master PR.

## Issues

<!-- No new issues. -->

## Duplicates / conflicts

- M1 carried from round 1. general raised no new issue.
