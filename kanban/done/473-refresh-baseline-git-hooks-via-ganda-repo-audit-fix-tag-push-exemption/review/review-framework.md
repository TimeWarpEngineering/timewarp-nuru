# Review framework — task 473

**Date:** 2026-09-24
**Host task:** kanban/to-do/473-refresh-baseline-git-hooks-via-ganda-repo-audit-fix-tag-push-exemption/
**Diff scope:** branch `task/473-refresh-baseline-git-hooks-via-ganda-repo-audit-fi` vs merge-base with master — `.githooks/pre-push.cs` + kanban task docs
**Plan / brief:** Refresh baseline pre-push via `ganda repo audit --fix` so tag-only pushes from master/main are allowed (`IsTagDest` / `IsExemptDest`); no product code
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** implementer-cursor / review oracle (ganda task-work)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
