# Review framework

## Budget (by-diff)

- Lines changed: 700
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Round 1 scope

**Date:** 2026-11-09
**Host task:** kanban/in-progress/569-devcli-check-version-packable-project-scan-takes-minutes-and-prints-nothing/
**Diff scope:** branch `task/569-devcli-check-version-packable-project-scan-takes-m` at 93650965 vs merge-base d87d67dd (master)
**Plan / brief:** task.md `## Goal (implementer restatement)` — bounded-parallel MSBuild evaluation, stderr progress line, Design note update, tests for order and named failure
**Effort:** 2 — roster: general
**Session IDs:** review oracle (claude, claude-opus-5-5), headless ganda task work

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
