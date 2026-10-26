# Review framework — task 545

**Date:** 2026-10-26
**Host task:** kanban/in-progress/545-nuru-search-drops-all-its-log-output-because-it-never-configures-logging/
**Diff scope:** branch `task/545-nuru-search-drops-all-its-log-output-because-it-ne` vs merge-base `4ee00816`. Product commit `8a5a4444`. 12 files, +469/−44 (513 lines in the budget count). Local `master` is two kanban-land commits ahead (`8ae3510a`, `cdf2b39e`); those commits touch only this task's `task.md`.
**Plan / brief:** Configure nuru-search logging so warnings and errors reach stderr, and extend NURU_H007 to endpoint handler constructors that inject `ILogger<T>` with no logging configured. The implementation also routes registered-service loggers through the same factory.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped). Review budget 120.

## Budget (by-diff)

- Lines changed: 513
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
