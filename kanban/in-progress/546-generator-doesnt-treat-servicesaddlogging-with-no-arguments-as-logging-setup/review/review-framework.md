# Review framework — task 546

**Date:** 2026-10-26
**Host task:** kanban/in-progress/546-generator-doesnt-treat-servicesaddlogging-with-no-arguments-as-logging-setup/
**Diff scope:** branch `task/546-generator-doesnt-treat-servicesaddlogging-with-no` vs merge-base `a69e545a`. Product commits `40fd36ee` and `fb7983e1`. 9 files, +349/−36 (385 lines in the budget count). Local `master` is two kanban-land commits ahead (`4358e9bb`, `4aa175a9`); those commits touch only this task's `task.md`.
**Plan / brief:** Treat `services.AddLogging()` with no configure delegate as logging setup. Match `LoggingServiceCollectionExtensions.AddLogging` by method symbol, emit a provider-less `LoggerFactory`, and prove an `ILogger<T>` delegate parameter gets that factory's logger with NURU_H007 quiet. Other call shapes are supported or recorded.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped). Review budget 120.

## Budget (by-diff)

- Lines changed: 385
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
