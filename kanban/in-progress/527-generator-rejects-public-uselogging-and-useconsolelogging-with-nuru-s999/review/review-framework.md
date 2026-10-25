# Review framework — task 527

**Date:** 2026-10-25
**Host task:** kanban/in-progress/527-generator-rejects-public-uselogging-and-useconsolelogging-with-nuru-s999/
**Diff scope:** round 2 reviews `450640e1..ffeddcda` (the M1 doc fix). Round 1 reviewed branch `task/527-generator-rejects-public-uselogging-and-useconsole` vs merge-base `cc2176cb` (product commit `56229740`; 1545 lines).
**Plan / brief:** Remove `UseLogging`, `UseConsoleLogging`, `UseDebugLogging`, and `ConfigureLogging`. Keep `ConfigureServices(s => s.AddLogging(...))` plus injected `ILogger<T>`. Round 2 checks that both logging.md copies say the generator parses with no logger.
**Effort:** 1 this round (general). Round 1 was effort 3 (general).
**Reviewer roster:** general
**Session IDs:** review-oracle (ganda task work, tw-implementation-review, Cursor headless, session not reported, max-turns uncapped). Round 1 review budget 200. Round 2 review budget 80.

## Budget (by-diff)

- Lines changed: 27
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Round 2 scope

Delta since round-1 base `450640e1`: commit `ffeddcda` rewrites the "Nuru's Own Log Messages" lead in `documentation/user/features/logging.md` and `documentation/developer/guides/logging.md`. Kitchen notes on `task.md` and `review/round-1/merged.md` are outside the product diff. Roster general, effort 1.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`
