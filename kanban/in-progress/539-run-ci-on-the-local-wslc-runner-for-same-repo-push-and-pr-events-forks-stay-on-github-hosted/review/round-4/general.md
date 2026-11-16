# Round 4 — general
**Date:** 2026-11-16
**Scope reviewed:** branch `task/539-run-ci-on-the-local-wslc-runner-for-same-repo-push` (HEAD `b161f9be`) against `master` (`51693a74`). Product delta is `.github/workflows/workflow.yml`, `.github/actions/setup-dotnet/action.yml`, and `tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs`. Since round 3 (`8839dfd6`) that product tree is unchanged; `871a7691` merges `origin/master`. Prior rounds were not edited.

## Summary

M1, M2, and M3 are fixed. No new findings.

Same-repo `push` and `pull_request` still route the `stage` matrix to `["self-hosted", "twe-001", "wslc"]`. Fork PRs, `workflow_dispatch`, `release`, and `CI_HOSTED_ONLY=true` stay on `ubuntu-latest`. `changes`, `ci`, and `release` stay hosted. There is no `install-pwsh` step. zsh cache steps stay hosted-only, and `Install zsh` returns before `sudo` when zsh is already present. The NuGet cache input is still off unless `runner.environment == 'github-hosted'`.

`RunOnPty` still runs util-linux `script -q -e -c` so the child's stdin is a pty (`Console.IsInputRedirected` is false) and its stdout and stderr go to temp files. The assertion is unchanged: exit 1 and stderr contains `requires redirected stdin`. The file mentions `python3` only in a comment. No other `python3` call remains in the tree this search covered. The recorded `./bin/dev build`, `./bin/dev test`, and `ganda repo audit` from the fix-3 pass ran after merge `871a7691` and cover this product tree. Later commits `2ddff4b7` and `b161f9be` are kanban landings. No full build, test, or audit was run in this review.

The Goal acceptance lines match the task, including the corrected pwsh line and the CI-red `script` addendum. The routing restatement still matches. Neither is a misread.

M3 is fixed without a targeted command. `git merge-base HEAD master` is `51693a74`, which is `master`, and `git rev-list --left-right --count master...HEAD` is `0 16`. Master is an ancestor of HEAD, so the `task.md` conflict against `4747af42` cannot recur on this tip.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage job, Setup .NET then Runner size)
- Description: `.github/actions/install-pwsh/` is gone. No `Install PowerShell` step. Goal, the Results pwsh bullet, and How to validate say the image already has `/usr/bin/pwsh`.
- Suggestion: none remaining
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: `task.md` Branch base bullet, Session line, Gates preamble
- Description: Those places record `d7f84ffd`, `0486ecc5`, and `c964078c`. The later CI-red and fix-3 sections name the test file and merge `871a7691`.
- Suggestion: none remaining
- Status: fixed

### M3 — Severity: suggestion — Status: fixed
- File: `kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/task.md`
- Description: Round 3 found `git merge-tree --write-tree master HEAD` conflicting on `task.md` against `4747af42`. The fix-3 merge `871a7691` brought in `4747af42` and `51693a74`. HEAD `b161f9be` contains `master` (`51693a74` is the merge-base). The kitchen still has the CI-red Results, the `script` acceptance line, and the round-2 record. No conflict markers.
- Suggestion: none remaining
- Status: fixed

## Issues

No new issues.
