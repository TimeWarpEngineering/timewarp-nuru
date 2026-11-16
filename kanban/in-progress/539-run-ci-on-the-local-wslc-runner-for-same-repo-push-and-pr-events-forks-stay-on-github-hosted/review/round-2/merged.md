# Round 2 — merged findings
**Date:** 2026-11-16
**Sources:** general

Post-fix re-review of delta `aabb1737..fea0ecf3`. Prior round `review/round-1/` is unchanged. M1 and M2 stay the live ids. No new ids.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 fixed in `e4f28c60`: `.github/actions/install-pwsh/` and the stage `Install PowerShell` step are gone. Goal, the Results pwsh bullet, and How to validate match the image's `/usr/bin/pwsh`.
- M2 fixed in the same pass: Branch base, the Grok Session line, and the Gates preamble record `d7f84ffd`, `0486ecc5`, and the `origin/master` merge `c964078c`. Master `5f0d2c7c` is an ancestor of HEAD.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage job, Setup .NET through Runner size); `.github/actions/install-pwsh/` removed
- Description: The wslc image already has pwsh. The fix removes the unused install action and the workflow step, and corrects the Goal line, the Results bullet, and How to validate so they no longer say the image has no pwsh or expect `Installed 7.7.0`.
- Suggestion: none remaining
- Source: general (round 1, reconfirmed pre-fix in this folder, verified fixed this re-review)
- Disposition notes: Fixed by the round-2 fix pass. Re-review read the deletion and the kitchen text. No install-pwsh path remains under `.github/`.

### M2 — Severity: nit — Status: fixed
- File: kanban/in-progress/539-…/task.md Branch base bullet, Session line, Gates preamble
- Description: The narrative now includes the origin/master replay, the groom merge, and the fix pass's merge of `origin/master`. The product delta against master is the two CI files.
- Suggestion: none remaining
- Source: general (round 1, updated pre-fix in this folder, verified fixed this re-review)
- Disposition notes: Fixed by the round-2 fix pass. `git merge-base HEAD master` is `5f0d2c7c`.

## Duplicates / conflicts

- None (single reviewer). Round 1 was not edited. This file replaces the pre-fix open ledger that the earlier review wrote into this same folder, because this host node reads `review/round-2/`.
