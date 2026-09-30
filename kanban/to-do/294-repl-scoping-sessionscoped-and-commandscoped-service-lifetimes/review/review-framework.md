# Review framework — task 294

**Date:** 2026-09-30
**Host task:** kanban/to-do/294-repl-scoping-sessionscoped-and-commandscoped-service-lifetimes/
**Diff scope:** branch `task/294-repl-scoping-sessionscoped-and-commandscoped-servi` vs `origin/master`
**Plan / brief:** Option A lifetimes (`AddSessionScoped`, `AddCommandScoped`) on the source-generated resolver and `UseMicrosoftDependencyInjection()`, including REPL disposal, from the decision table on `task.md`.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor implementer-cursor profile, headless)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
