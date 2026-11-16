# Round 2 — general
**Date:** 2026-11-16
**Scope reviewed:** fix delta `aabb1737..fea0ecf3` (round-1 `base.sha` through HEAD). Product change: delete `.github/actions/install-pwsh/action.yml` and the stage job's `Install PowerShell` step in `.github/workflows/workflow.yml`. Kitchen: Goal acceptance, Results pwsh bullet, Branch base, Session, Gates, How to validate. The `origin/master` merge `c964078c` is in the delta; its product files match master, so they are not a second review of tasks 496, 497, 520, or 594.

The pre-fix text of this file (open M1 and M2, commit `e4f28c60`) is replaced by this post-fix re-review. The host named `review/round-2/` for this node. `review/round-1/` is unchanged.

## Summary

M1 and M2 are fixed. No new findings.

The stage job still routes same-repo `push` and `pull_request` events to `["self-hosted", "twe-001", "wslc"]`, and sends fork PRs, `workflow_dispatch`, and `CI_HOSTED_ONLY=true` to `ubuntu-latest`. `changes`, `ci`, and `release` stay on `ubuntu-latest`. The fix deletes the `Install PowerShell` step that sat between Setup .NET and Runner size. `defaults.run.shell` is still `pwsh`. The wslc image's `/usr/bin/pwsh` is what those steps run. zsh cache steps stay hosted-only. The NuGet Actions cache input is unchanged.

The Goal acceptance line now says the image already has `pwsh` and that no install step is required. That matches the task. The earlier line that said the image has no `pwsh` and that each stage extracts 7.7.0 was a misread; the fix pass removed it. The routing restatement still matches the task: the post-538 `stage` matrix is the build and test work, and the required `ci` aggregate stays hosted.

The recorded gates cover this tree. `./bin/dev build`, `./bin/dev test`, and `ganda repo audit` were re-run after `c964078c`, which is after the install-pwsh deletion. Audit passed once the master pin merge cleared `package-pin-currency`. `actionlint` is recorded as not installed; the workflow edit is a step deletion, and the surrounding YAML reads as a valid job. No targeted command this round. No full build, test, or audit.

## Resolved prior

### M1 — Severity: suggestion — Status: fixed
- File: `.github/workflows/workflow.yml` (stage steps, after Setup .NET); `.github/actions/install-pwsh/` is gone
- Description: The action file and the `Install PowerShell` step are deleted (`e4f28c60`). No `install-pwsh` reference remains under `.github/`. Goal line 36, the Results pwsh bullet, and How to validate now say the SDK base image ships `/usr/bin/pwsh` and that the first `pwsh` step is `Runner size`. They no longer expect `Installed 7.7.0`.
- Suggestion: none remaining
- Status: fixed

### M2 — Severity: nit — Status: fixed
- File: `task.md` Branch base bullet, Session Grok line, Gates preamble
- Description: Those three places now record the replay `d7f84ffd`, the groom merge `0486ecc5`, the round-2 merge-tree preview, and this pass's `origin/master` merge `c964078c`. `git merge-base HEAD master` is `5f0d2c7c` (master is an ancestor). The product diff against master is `.github/workflows/workflow.yml` and `.github/actions/setup-dotnet/action.yml`. The sentence that master "is 16 commits ahead" is the pre-merge state; the next sentence records the merge that cleared it.
- Suggestion: none remaining
- Status: fixed

## Issues

None. No new defect on the fix delta.
