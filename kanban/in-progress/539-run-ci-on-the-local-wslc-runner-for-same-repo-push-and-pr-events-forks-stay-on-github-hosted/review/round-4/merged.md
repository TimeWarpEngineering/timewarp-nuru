# Round 4 — merged findings
**Date:** 2026-11-16
**Sources:** general

Re-review of HEAD `b161f9be` against `master` `51693a74`. Prior rounds `review/round-1/`, `review/round-2/`, and `review/round-3/` are unchanged. M1, M2, and M3 stay fixed. No new id.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 2 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 fixed (round 2, still true): `.github/actions/install-pwsh/` and the stage `Install PowerShell` step are gone. Goal, the Results pwsh bullet, and How to validate match `/usr/bin/pwsh`.
- M2 fixed (round 2, still true): Branch base, the Grok Session line, and the Gates preamble record `d7f84ffd`, `0486ecc5`, and `c964078c`. Later sections record the CI-red test change and merge `871a7691`.
- M3 fixed (round 4): `master` `51693a74` is an ancestor of HEAD `b161f9be` (0 behind, 16 ahead). Merge `871a7691` is in the branch. The `task.md` conflict against `4747af42` is gone. The branch kitchen text was kept.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage job, Setup .NET through Runner size)
- Description: The wslc image already has pwsh. The unused install action and workflow step are gone, and the kitchen no longer says the image has no pwsh.
- Suggestion: none remaining
- Source: general (round 1, fixed in round 2, reconfirmed rounds 3 and 4)
- Disposition notes: Still fixed on HEAD `b161f9be`. No `install-pwsh` path under `.github/`.

### M2 — Severity: nit — Status: fixed
- File: `task.md` Branch base bullet, Session line, Gates preamble
- Description: The narrative records the origin/master replay, the groom merge, and the fix pass's merge of `origin/master`.
- Suggestion: none remaining
- Source: general (round 1, fixed in round 2, reconfirmed rounds 3 and 4)
- Disposition notes: Still fixed.

### M3 — Severity: suggestion — Status: fixed
- File: `kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/task.md`
- Description: Round 3: `git merge-tree --write-tree master HEAD` exited 1 because `4747af42` (`land kanban 539`) conflicted on `task.md`. Fix-3 merged `origin/master`. HEAD contains that commit. `git merge-base HEAD master` is `51693a74` (current `master`).
- Suggestion: none remaining
- Source: general (round 3, fixed in the fix-3 pass, reconfirmed round 4)
- Disposition notes: Fixed. The branch is 0 behind `master`. Product files were not in the old conflict and are unchanged since round 3. No conflict markers in `task.md`.

## Duplicates / conflicts

- None (single reviewer). Rounds 1, 2, and 3 were not edited.
