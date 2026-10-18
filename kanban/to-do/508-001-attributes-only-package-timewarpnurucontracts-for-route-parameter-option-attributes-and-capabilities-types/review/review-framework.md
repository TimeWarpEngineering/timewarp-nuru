# Review framework

## Budget (by-diff)

- Lines changed: 2802
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Scope — task 508-001

**Date:** 2026-10-18
**Host task:** kanban/to-do/508-001-attributes-only-package-timewarpnurucontracts-for-route-parameter-option-attributes-and-capabilities-types/
**Diff scope:** branch `task/508-001-attributes-only-package-timewarpnurucontracts-for` at `f522c6e2` vs merge-base `5e512c0a` with `master` (`git diff 5e512c0a..f522c6e2`), product paths only
**Plan / brief:** `task.md` `## Goal (implementer restatement)` and `## Requirements` — derive Nuru endpoints from `TimeWarp.Contracts` `[Operation]` requests, mediator dispatch, `IResponseRenderer`, problem rendering, sample and docs
**Effort:** 3
**Reviewer roster:** general (generator correctness), tests (runs builds, unit tests, full CI suite, sample smoke), plan_alignment (requirements, docs, restatement misread check)
**Session IDs:** review oracle (claude-opus-5-5, headless `ganda task work`), three Claude subagents; vendor session ids not reported

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
