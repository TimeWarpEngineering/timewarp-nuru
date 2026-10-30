# Round 1 — general
**Date:** 2026-10-30
**Scope reviewed:** same as the framework (branch vs master, commit 507051ba)

## Summary

`dev` now calls `UseTelemetry()` with the literal service name `timewarp-nuru-dev` and sets no endpoint. `workflow-command.cs` wraps each PR/merge and release stage in `InStageAsync`, which opens a `TimeWarp.Nuru` child span through `WorkflowStageSpan`. `IncomingTraceContext` restores `TRACEPARENT` before `RunAsync`. A whitespace-insensitive diff against master shows the pipeline control flow did not change. Every former `return` is now `Result.Stop(...)` followed by a `return` in the caller. Every terminal line and `WriteStepDuration` call is still in the same place. The stage handlers (clean, build, verify-samples, test) throw on failure, so the `catch` in `InStageAsync` records `error.type` as the exception type name and never records the message. The generated setup only builds a tracer provider inside `if (!string.IsNullOrEmpty(__otlpEndpoint))` (`telemetry-emitter.cs:63-66`), so an unset endpoint exports nothing by construction. No `.github` file changed. The risk is low. The remaining points are about robustness and trace fidelity, not correctness.

Verified in this worktree:

- `dotnet tools/dev-cli/telemetry-proof/proof.cs` printed the command-span, unset-endpoint, and stage-span lines, then `proof ok` (16s).
- `dotnet tools/dev-cli/telemetry-proof/proof.cs aot` printed `aot bin/dev: one workflow span, parented to TRACEPARENT; unset endpoint exports nothing` against `bin/dev`, a native ELF built at 11:02, just before commit 507051ba. The `UnsafeAccessor` path therefore works under NativeAOT on the current runtime.

The implementer restatement matches the original task. It narrows "TimeWarp.Nuru or TimeWarp.Nuru.Behavior" to `TimeWarp.Nuru`, which the task allows. No misread.

## Issues

### Issue 1 — Severity: suggestion
- File: tools/dev-cli/endpoints/incoming-trace-context.cs:49-53
- Description: `Restore()` writes the caller's span id into the private `Activity._spanId` field through `[UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_spanId")]`. That field is a runtime implementation detail. If a future System.Diagnostics.DiagnosticSource renames it, `SpanIdField(activity)` throws `MissingFieldException` at startup. That happens on every `dev` run where `TRACEPARENT` is set, which is every ganda walk. `dev` would then fail before dispatching any command. GitHub Actions never sets `TRACEPARENT`, so CI would not catch it. After the patch, `activity.Id` still holds the random span id while `activity.SpanId` returns the caller's, so the two disagree. The task only asked for this context to be documented. Restoring it goes beyond the requirement.
- Suggestion: wrap the accessor call in `try { … } catch (MissingFieldException) { }` so a failure falls back to the plain restored activity (trace id still joined, parent is the unexported local span). Separately, file a Nuru-core follow-up so `TelemetryBehavior` takes a remote `ActivityContext` (from `TRACEPARENT`) as an explicit parent. That makes the private-field write unnecessary.
- Status: open

### Issue 2 — Severity: suggestion
- File: tools/dev-cli/endpoints/workflow-command.cs:309-315
- Description: part 2 of the package integrity gate aborts a full PR pipeline (`Environment.ExitCode = 1`, "The artifact must not ship") outside every stage span. That trace shows every stage child as Ok. `TelemetryBehavior` also sets the `workflow` span to Ok whenever the handler returns without throwing. A pipeline that aborts here therefore looks fully green in the trace. The same holds for the workflow span on any `AbortPipeline` path, though in those cases the aborting stage child is Error.
- Suggestion: wrap the integrity check in its own stage span (for example `package-integrity`, with `Result.Stop(...)` on failure), or record the gap in `telemetry.md`. The workflow-span status comes from `TelemetryBehavior` in Nuru core, which is out of scope for a change limited to `tools/dev-cli`.
- Status: open

### Issue 3 — Severity: nit
- File: tools/dev-cli/telemetry-proof/proof.cs:83-102, 147-166, 346
- Description: (a) The unset-endpoint check only shows that nothing reached the sink's random loopback port. An exporter with no endpoint would target the SDK default (`localhost:4317/4318`), not the sink. The real guarantee is the generated endpoint guard, so this check passes trivially. (b) The stage proof calls `WorkflowStageSpan.Begin/Succeed/Fail` directly and never calls `InStageAsync`. The exception path (`error.type` = exception type name, message omitted) is never exercised. The task allows "a focused test of the stage helper", so this is not a requirement gap. (c) `ProofRun.Traces` is always `[]` and never read.
- Suggestion: (a) add a comment that names the generated guard as the guarantee, or also bind a listener on 4318. (b) Optional: add one throwing stage to the stage app. (c) Drop the unused field.
- Status: open
