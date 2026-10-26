# Review framework — task 544

**Date:** 2026-10-26
**Host task:** kanban/in-progress/544-apps-in-one-compilation-share-the-first-apps-logger-factory/
**Diff scope:** branch `task/544-apps-in-one-compilation-share-the-first-apps-logge` vs merge-base `0c94984a`. Product commits `e8913db5` and `17b4a0ba`. Full branch diff is 15 files, +562/−100 (662). Local `master` is five `land kanban 544` commits ahead of that merge-base; they are not in this diff.
**Plan / brief:** Each app in one compilation must log through the running app's AddLogging factory or telemetry `app.LoggerFactory`. An app with neither gets a NullLogger. Framework `ILogger<T>` fields are named from the fully qualified type argument. generator-66 returns to the CI multi-mode assembly.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped). Review budget 120. Round 2 is the same oracle, same bound, 2026-10-26.
**Round 2:** re-review of the same product diff. Round 1 stays in `review/round-1/`.

## Budget (by-diff)

- Lines changed: 662
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
