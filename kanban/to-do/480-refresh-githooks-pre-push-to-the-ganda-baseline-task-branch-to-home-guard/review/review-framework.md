# Review framework

## Budget (by-diff)

- Lines changed: 53
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

# Review framework — task 480

**Date:** 2026-10-01
**Host task:** kanban/to-do/480-refresh-githooks-pre-push-to-the-ganda-baseline-task-branch-to-home-guard/
**Diff scope:** commit 08d4f201 (`.githooks/pre-push.cs` +19, task.md) vs master
**Plan / brief:** refresh pre-push hook to ganda baseline via `ganda repo audit --fix --checks memsearch-scaffold` (task→home guard)
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** ganda task work review oracle (Claude Opus 5.5, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
