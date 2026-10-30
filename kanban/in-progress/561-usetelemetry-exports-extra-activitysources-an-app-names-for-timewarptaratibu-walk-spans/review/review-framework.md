# Review framework — task 561

**Date:** 2026-10-31
**Host task:** kanban/in-progress/561-usetelemetry-exports-extra-activitysources-an-app-names-for-timewarptaratibu-walk-spans/
**Diff scope:** branch `task/561-usetelemetry-exports-extra-activitysources-an-app` vs merge-base `31382e6cc899e5fa7000d3cf4993d2231b1dc484` (product commit `e11b441b`)
**Plan / brief:** `UseTelemetry` subscribes the generated tracer and meter providers to extra ActivitySource and Meter names an app passes as compile-time constants, so a library such as TimeWarp.Taratibu can be exported. Unconfigured output stays the same.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** review oracle session not exposed (Cursor, Grok 4.7). Implementer session `62028b4e-74d4-4f82-96e4-37a10e56f815`.

## Budget (by-diff)

- Lines changed: 794
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
