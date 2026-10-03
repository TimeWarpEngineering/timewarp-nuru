# Review framework

## Budget (by-diff)

- Lines changed: 2410
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

# Review framework — task 219

**Date:** 2026-10-03
**Host task:** kanban/to-do/219-review-large-test-files-for-refactoring-opportunities/
**Diff scope:** branch `task/219-review-large-test-files-for-refactoring-opportunit` vs `master` (commit 9bd89c7c)
**Plan / brief:** split generator-26 (subjects → fixtures file) and routing-05 (→ three runfiles); keep nine other large test files; refresh internals-visible-to.g.cs
**Effort:** 3 (by-diff budget); roster axes: general
**Reviewer roster:** general
**Session IDs:** review oracle Claude Opus 5.5 (ganda task work); general reviewer subagent af0976436bd743e3b

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
