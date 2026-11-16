# Round 3 — general
**Date:** 2026-11-16
**Scope reviewed:** branch `task/539-run-ci-on-the-local-wslc-runner-for-same-repo-push` (HEAD `8839dfd6`) against `master` (`4747af42`). Product delta since the round-2 review (`fea0ecf3`) is `tests/timewarp-nuru-tests/routing/routing-33b-json-args-stdin-and-defaults.cs`. Full product delta against the merge-base is that test plus `.github/workflows/workflow.yml` and `.github/actions/setup-dotnet/action.yml`. Prior rounds were not edited.

## Summary

M1 and M2 stay fixed. The new test change does what the CI-red addendum asks. One new finding: `task.md` does not merge with master.

Same-repo `push` and `pull_request` still route the `stage` matrix to `["self-hosted", "twe-001", "wslc"]`. Fork PRs, `workflow_dispatch`, and `CI_HOSTED_ONLY=true` stay on `ubuntu-latest`. `changes`, `ci`, and `release` stay hosted. There is no `install-pwsh` step. zsh cache steps stay hosted-only, and `Install zsh` still returns before `sudo` when zsh is already present. The NuGet cache input is still off unless `runner.environment == 'github-hosted'`.

`RunOnPty` now runs util-linux `script -q -e -c` so the child's stdin is a pty and its stdout and stderr go to temp files. The assertion is unchanged: exit 1 and stderr contains `requires redirected stdin`. The generated binder writes that string with `WriteBindErrorAsync` and returns 1 before reading stdin. Nothing is skipped. The file mentions `python3` only in a comment. No other `python3` call remains under `tests/` or `.github/`. The recorded `./bin/dev build`, `./bin/dev test` (the tty test passed), and `ganda repo audit` are from the CI-red fix pass and cover this tree. No full build, test, or audit was run in this review.

The Goal acceptance line for `script` matches the task's CI-red addendum (do not skip; pty from `script`; check other stages). The routing restatement still matches the task. Neither is a misread.

**Targeted command (one).** The branch is 1 commit behind `master`, and that commit is `4747af42` (`land kanban 539`), which edits this kitchen. Command: `git merge-tree --write-tree master HEAD`. Reason: a dirty merge of the kitchen would block the later land. Exit 1. The only conflict is `task.md`. Product files are not in the conflict.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage job, Setup .NET then Runner size)
- Description: `.github/actions/install-pwsh/` is gone. No `Install PowerShell` step. Goal, the Results pwsh bullet, and How to validate say the image already has `/usr/bin/pwsh`.
- Suggestion: none remaining
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: `task.md` Branch base bullet, Session line, Gates preamble
- Description: Those places still record `d7f84ffd`, `0486ecc5`, and `c964078c`. `git merge-base HEAD master` is `5f0d2c7c`. The Branch base sentence that names only the two CI files is the post-`c964078c` delta; the CI-red section names the test file added after that.
- Suggestion: none remaining
- Status: fixed

## Issues

### Issue 1 — Severity: suggestion
- File: `kanban/in-progress/539-run-ci-on-the-local-wslc-runner-for-same-repo-push-and-pr-events-forks-stay-on-github-hosted/task.md`
- Description: `git merge-tree --write-tree master HEAD` exits 1. `master` is `4747af42` (`land kanban 539`), one commit ahead of the merge-base `5f0d2c7c`. That commit and this branch both edit `task.md`, and the merge conflicts there. Workflow, setup-dotnet, and the routing test are not in the conflict. The branch copy of `task.md` already contains the landed round-2 record plus the CI-red section, so a resolution that keeps the branch file keeps the newer kitchen.
- Suggestion: Merge `master` into this branch and resolve `task.md` by keeping this branch's text. Do not drop the CI-red Results, the `script` acceptance line, or the round-2 record.
- Status: open
