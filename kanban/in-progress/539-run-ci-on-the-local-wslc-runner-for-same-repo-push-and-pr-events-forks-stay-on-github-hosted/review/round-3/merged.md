# Round 3 — merged findings
**Date:** 2026-11-16
**Sources:** general

Re-review of HEAD `8839dfd6` against `master` `4747af42`. Prior rounds `review/round-1/` and `review/round-2/` are unchanged. M1 and M2 stay fixed. New id: M3.

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 1 | 1 | 0 |
| nit | 0 | 1 | 0 |

## Resolved prior

- M1 fixed (round 2, still true): `.github/actions/install-pwsh/` and the stage `Install PowerShell` step are gone. Goal, the Results pwsh bullet, and How to validate match `/usr/bin/pwsh`.
- M2 fixed (round 2, still true): Branch base, the Grok Session line, and the Gates preamble record `d7f84ffd`, `0486ecc5`, and `c964078c`. Merge-base with master is `5f0d2c7c`.

## Issues

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage job, Setup .NET through Runner size)
- Description: The wslc image already has pwsh. The unused install action and workflow step are gone, and the kitchen no longer says the image has no pwsh.
- Suggestion: none remaining
- Source: general (round 1, fixed in round 2, reconfirmed round 3)
- Disposition notes: Still fixed on HEAD `8839dfd6`. No `install-pwsh` path under `.github/`.

### M2 — Severity: nit — Status: fixed
- File: `task.md` Branch base bullet, Session line, Gates preamble
- Description: The narrative records the origin/master replay, the groom merge, and the fix pass's merge of `origin/master`.
- Suggestion: none remaining
- Source: general (round 1, fixed in round 2, reconfirmed round 3)
- Disposition notes: Still fixed. The later CI-red section names the test file that landed after that merge.

### M3 — Severity: suggestion — Status: open
- File: `kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/task.md`
- Description: `git merge-tree --write-tree master HEAD` exits 1. The one commit on master that this branch lacks is `4747af42` (`land kanban 539`). It conflicts with this branch's `task.md`. Product files merge clean. The branch `task.md` already includes the landed round-2 kitchen and the CI-red record.
- Suggestion: Merge `master` into this branch and keep this branch's `task.md`.
- Source: general (round 3)
- Disposition notes:

## Duplicates / conflicts

- None (single reviewer). Round 1 and round 2 were not edited.
