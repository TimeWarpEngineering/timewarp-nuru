# Disposition — task 556

**Date:** 2026-10-30
**Outcome:** accepted-exceptions
**Rounds:** 1
**Final open count:** 0

## Summary

The implementation meets the restated goal and the original requirements. `dev` calls `UseTelemetry()` with the literal service name `timewarp-nuru-dev` and sets no endpoint. Each PR/merge and release stage is a `TimeWarp.Nuru` child span of the `workflow` command span. Stage spans carry `ci.mode` and `ci.stage`. A stage that aborts the pipeline records Error status and `error.type` only. The pipeline span carries `ProcessorCount`. The terminal duration lines are unchanged. The env vars are documented in `tools/dev-cli/telemetry.md`. The change stays inside `tools/dev-cli`. The reviewer re-ran both proofs in this worktree, `proof.cs` and `proof.cs aot` against the native `bin/dev`, and both passed. The review raised no bugs. The two suggestions and one nit are accepted as exceptions.

## Exception log

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M1 | suggestion | The `UnsafeAccessor` write to `Activity._spanId` works today under JIT and NativeAOT (both proofs pass). A rename would fail loudly, not silently. The proper fix is for Nuru core's `TelemetryBehavior` to accept a remote parent, which is outside the `tools/dev-cli` scope. Follow-up recommended. | reviewer-claude (review oracle) |
| M2 | suggestion | The integrity gate is not in the task's stage list. The terminal output and exit code still report the abort. Workflow-span status is owned by Nuru core. | reviewer-claude (review oracle) |
| M3 | nit | The proof's guarantee comes from the generated endpoint guard. The task explicitly allows a focused stage-helper test. These are polish items on a manual harness. | reviewer-claude (review oracle) |

## Escalations

- None. Recommended follow-up for the human at PR time: a Nuru-core task so `TelemetryBehavior` parents to an incoming `TRACEPARENT` context and lets a handler mark the command span Error without throwing. That would remove the private-field accessor (M1) and the green-on-abort workflow span (M2).
