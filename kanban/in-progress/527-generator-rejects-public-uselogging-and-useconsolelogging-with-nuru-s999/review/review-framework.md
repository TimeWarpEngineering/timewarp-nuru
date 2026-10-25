# Review framework — task 527

**Date:** 2026-10-25
**Host task:** kanban/in-progress/527-generator-rejects-public-uselogging-and-useconsolelogging-with-nuru-s999/
**Diff scope:** branch `task/527-generator-rejects-public-uselogging-and-useconsole` vs merge-base `cc2176cb` (product commit `56229740`; 1545 lines changed)
**Plan / brief:** Remove `UseLogging`, `UseConsoleLogging`, `UseDebugLogging`, and `ConfigureLogging`. Keep `ConfigureServices(s => s.AddLogging(...))` plus injected `ILogger<T>`. Changelog, generator-66, the `add-logging` probe, and the logging docs follow that decision.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped, review budget 200)

## Budget (by-diff)

- Lines changed: 1545
- Effort: 3
- TCB hits: none
- Roster axes: general
- Turn cap: 200 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
