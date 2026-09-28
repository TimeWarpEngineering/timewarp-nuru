# Review framework — task 477

**Date:** 2026-09-28
**Host task:** kanban/to-do/477-triage-to-do-2026-09-28-close-superseded-tasks-rewrite-outdated-specs/
**Diff scope:** branch `task/477-triage-to-do-2026-09-28-close-superseded-tasks-rew` vs `origin/master`. Kanban only (`fdad4d13` triage, plus later task.md commits). Nothing under `source/`, `tests/`, or `samples/`.
**Plan / brief:** Archive superseded to-do tasks 434, 436, 255, 257, and 258 with a closing note; move 437 to backlog; rewrite 456, 069, and 365 against current code; regenerate 219's over-500-line test list.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
