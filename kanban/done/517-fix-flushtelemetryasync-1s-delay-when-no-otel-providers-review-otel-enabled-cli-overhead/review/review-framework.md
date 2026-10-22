# Review framework — task 517

**Date:** 2026-10-22
**Host task:** kanban/to-do/517-fix-flushtelemetryasync-1s-delay-when-no-otel-providers-review-otel-enabled-cli-overhead/
**Diff scope:** branch `task/517-fix-flushtelemetryasync-1s-delay-when-no-otel-prov` vs `master` (`d39e6c0e`..`3db4ef44`; product change is `nuru-app.cs` and `telemetry-03-flush-without-providers.cs`)
**Plan / brief:** `FlushTelemetryAsync` returns immediately when tracer, meter, and logger providers are all null. The 1000 ms grace delay stays when any provider is set. Emitters stay as they are. A no-endpoint command test, a direct no-provider flush test, and a tracer-provider flush/delay/dispose test cover the contract. Perf notes against a live collector are in the task, not a code change.
**Effort:** 2 (by-diff; roster axis general only)
**Reviewer roster:** general
**Session IDs:** session not reported (cursor, grok 4.7, 2026-10-22)

## Budget (by-diff)

- Lines changed: 238
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
