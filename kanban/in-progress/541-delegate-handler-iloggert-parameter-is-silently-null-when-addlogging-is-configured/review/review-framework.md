# Review framework — task 541

**Date:** 2026-10-26
**Host task:** kanban/in-progress/541-delegate-handler-iloggert-parameter-is-silently-null-when-addlogging-is-configured/
**Diff scope:** branch `task/541-delegate-handler-iloggert-parameter-is-silently-nu` vs merge-base `3f595ea9` (local `master`). Product commits `553f8b40` and `8a9fa0e6`. 902 lines changed.
**Plan / brief:** Delegate `.WithHandler((ILogger<T> logger) => ...)` parameters resolve through the same logger path as endpoint and behavior constructors (`LoggerExpressions`), with `NURU_H007` when logging is absent and `NURU050` when a handler parameter would otherwise compile to a silent `default!`.
**Effort:** 3 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer session not reported

## Budget (by-diff)

- Lines changed: 92
- Effort: 1
- TCB hits: none
- Roster axes: general
- Turn cap: 80 (--max-turns; cursor uncapped)

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-N/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-(N+1)/`

## Round 2

**Date:** 2026-10-26
**Diff scope:** `2e0fd8e8..3b5a6504` (M1 fix pass). 92 lines changed.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** reviewer session not reported
