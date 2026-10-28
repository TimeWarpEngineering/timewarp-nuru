# Review framework — task 552

**Date:** 2026-10-28
**Host task:** kanban/in-progress/552-usetelemetry-emits-no-command-spans-telemetryemitter-span-methods-are-never-called/
**Diff scope:** branch `task/552-usetelemetry-emits-no-command-spans-telemetryemitt` vs merge-base `bd204a66` (master at the branch point). Product diff is the telemetry registration, the method-group command class, telemetry-04/05, and the telemetry docs. The branch is 3 commits behind master; those commits are outside this review.
**Plan / brief:** `UseTelemetry()` registers `TelemetryBehavior` as the outermost behavior so each command run emits one span, without a second span when the app also adds that behavior. Dead `EmitTelemetryStart/Success/Catch` helpers are removed. Docs state what `UseTelemetry()` emits.
**Effort:** 2
**Reviewer roster:** general
**Session IDs:** reviewer session id not exposed (headless print mode). Turn cap 120; Cursor has no `--max-turns`, so the session is uncapped.

## Budget (by-diff)

- Lines changed: 211
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

## Round 2

**Date:** 2026-10-28
**Diff scope:** fix delta `2f834e7d` vs round-1 base `1c4555d1`. Product files: `behavior-emitter.cs`, `command-class-emitter.cs`, `command-construction-emitter.cs`, `telemetry-05-use-telemetry-command-spans.cs`.
**Effort:** 2
**Reviewer roster:** general
**Turn cap:** 120 (Cursor uncapped; no `--max-turns`)
**Session IDs:** reviewer session id not exposed (headless print mode)
