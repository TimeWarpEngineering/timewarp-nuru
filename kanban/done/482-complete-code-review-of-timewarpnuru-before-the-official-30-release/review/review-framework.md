# Review framework

## Budget (by-diff)

- Lines changed: 573
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

# Review framework — task 482

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-complete-code-review-of-timewarpnuru-before-the-official-30-release/
**Diff scope:** branch `task/482-complete-code-review-of-timewarpnuru-before-the-of` vs `master` (commits d89ee637, 85f7d89a, f5906835)
**Plan / brief:** 3.0 review record (area files + `findings.md`), migration guide, public XML example fixes (R-7), `skills/tw-nuru/SKILL.md` alignment, child tasks 482-001 … 482-015
**Effort:** 2 (by-diff budget)
**Reviewer roster:** general
**Session IDs:** claude review oracle (ganda task work, 2026-10-05)

Note: the pre-existing `review/*.md` area files and `findings.md` are the task's deliverable (the 3.0 review record). Implementation-review artifacts for this diff live under `round-N/` and `disposition.md`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
