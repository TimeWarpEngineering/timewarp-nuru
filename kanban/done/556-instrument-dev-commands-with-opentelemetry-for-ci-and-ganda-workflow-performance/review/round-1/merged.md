# Round 1 — merged findings
**Date:** 2026-10-30
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 2 |
| nit | 0 | 0 | 1 |

## Issues

### M1 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/incoming-trace-context.cs:49-53
- Description: `UnsafeAccessor` writes the private `Activity._spanId` field. If a later runtime renames that field, `dev` throws `MissingFieldException` at startup whenever `TRACEPARENT` is set, which includes every ganda walk. `Activity.Id` and `Activity.SpanId` disagree after the patch.
- Suggestion: catch `MissingFieldException` and fall back. File a Nuru-core follow-up so `TelemetryBehavior` accepts a remote parent context.
- Source: general
- Disposition notes: wontfix on this task (decided by reviewer-claude, review oracle). The code works today: the JIT proof and the NativeAOT `bin/dev` proof both show the command span parented to the caller's span id. A runtime rename of `_spanId` is unlikely, and if it happened the failure would be loud and immediate rather than silent. The design is recorded in the file's Design region. The real fix belongs in Nuru core (`TelemetryBehavior` taking a remote parent), not in `tools/dev-cli`. Recommended follow-up: a Nuru task that lets `TelemetryBehavior` parent to a `TRACEPARENT` context, after which this accessor can be deleted.

### M2 — Severity: suggestion — Status: wontfix
- File: tools/dev-cli/endpoints/workflow-command.cs:309-315
- Description: the package integrity gate (part 2) aborts a full PR pipeline outside every stage span, so the trace shows all stages Ok. The `workflow` span is Ok on every non-throwing abort because `TelemetryBehavior` sets Ok when the handler returns.
- Suggestion: add a `package-integrity` stage span, or document the gap.
- Source: general
- Disposition notes: wontfix (decided by reviewer-claude). The task names a fixed stage list (attestation, clean, build, verify-samples, test), and the integrity gate is not one of them. The goal is timing ("where the pipeline spent time"), and the gate only re-hashes nupkgs. The terminal banner and the exit code still report the abort. Workflow-span status is owned by `TelemetryBehavior` in Nuru core, outside this task's `tools/dev-cli` scope. Worth folding into the M1 core follow-up (for example, a handler-set status that `TelemetryBehavior` does not overwrite).

### M3 — Severity: nit — Status: wontfix
- File: tools/dev-cli/telemetry-proof/proof.cs:83-102, 147-166, 346
- Description: the unset-endpoint check passes trivially against the sink. The stage proof bypasses `InStageAsync`, so the exception path is not exercised. `ProofRun.Traces` is unused.
- Suggestion: comment on the generated guard, add one throwing stage, and drop the unused field.
- Source: general
- Disposition notes: wontfix (decided by reviewer-claude). The generated guard (`telemetry-emitter.cs:63-66`) does guarantee an unset endpoint exports nothing, and the task explicitly accepts a focused stage-helper test. These are polish items on a manual proof harness that CI does not run.

## Duplicates / conflicts

- None. A single reviewer produced three distinct findings.
