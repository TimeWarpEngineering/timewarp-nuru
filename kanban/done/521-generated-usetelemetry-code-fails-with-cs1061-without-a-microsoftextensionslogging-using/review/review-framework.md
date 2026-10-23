# Review framework — task 521

**Date:** 2026-10-23
**Host task:** kanban/to-do/521-generated-usetelemetry-code-fails-with-cs1061-without-a-microsoftextensionslogging-using/
**Diff scope:** branch `task/521-generated-usetelemetry-code-fails-with-cs1061-with` vs `master` (`d896df9f`; changelog, telemetry emitter, interceptor usings, generator-63, CI standalone registration)
**Plan / brief:** Emit `UseTelemetry()` extension calls in static form with `global::` declaring types taken from the referenced assemblies, without adding `using` directives, and compile a `UseTelemetry()` app that has no `Microsoft.Extensions.Logging` using.
**Effort:** 2 (general only)
**Reviewer roster:** general
**Session IDs:** cursor session not reported; max-turns uncapped (review budget 120)

## Budget (by-diff)

- Lines changed: 220
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
