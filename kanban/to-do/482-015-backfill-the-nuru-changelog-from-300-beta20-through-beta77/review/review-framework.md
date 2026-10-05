# Review framework — task 482-015

**Date:** 2026-10-05
**Host task:** kanban/to-do/482-015-backfill-the-nuru-changelog-from-300-beta20-through-beta77/
**Diff scope:** commit `d7c53758` on `task/482-015-backfill-the-nuru-changelog-from-300-beta20-throug` (`changelog.md` and this task's `task.md`). Not the rest of `origin/master...HEAD` (parent 482 work already merged or outside this commit).
**Plan / brief:** Backfill dated changelog sections for `3.0.0-beta.20` through `3.0.0-beta.77` from git history and GitHub releases. Do not fabricate entries. Quiet or unpublished betas may be one line or grouped. Keep Unreleased for changes since beta.78 (task text) / after beta.77 (checklist).
**Effort:** 2 (general only; by-diff budget, 398 lines)
**Reviewer roster:** general
**Session IDs:** grok `01a10c45-a1e3-7da0-ae7d-bbcefd35de9f` (2026-10-05)

## Budget (by-diff)

- Lines changed: 398
- Effort: 2
- TCB hits: none
- Roster axes: general
- Turn cap: 120 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
- Check each new section against the GitHub release body and `git log` between adjacent tags. A mismatch that attributes a feature to the wrong beta is a bug. Grouping quiet retries is in scope when the notes match.
