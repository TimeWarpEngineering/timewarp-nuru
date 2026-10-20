# Round 2 — general
**Date:** 2026-10-20
**Scope reviewed:** fix delta `74d7b4cc..f2b2bccc` (M1 only). Not the whole branch.

## Summary

The fix makes a bound call's render continue that call. `InvokeAsync` stores the executor's exit code on the call context. `RenderAsync(request, result, terminal, call)` renders in a new context seeded from that call (exit code, working directory, console capture, exclusive hold) and writes the rendered code back. Generated `ExecuteRouteAsync` no longer sets `CurrentExitCode = 0` when `RenderTerminal` is set, so a handler's code stays and problem details still overwrite it. Daemon `invoke` and in-process McpHost pass the call context; forwarded McpHost uses daemon invoke. `routing-35`, `daemon-04`, and `mcphost-03` passed here (7 / 6 / 6), including the new exit-code and directory cases and the existing soak of problem statuses. The updated goal restatement matches the task. No new defects on this delta.

## Resolved prior

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:226
- Description: Re-checked against the fix. Render starts from the invoke call's `ExitCode` and `WorkingDirectory`. `interceptor-emitter.cs` skips the opening reset only when `__invocation.RenderTerminal` is set. Problem-details rendering still assigns `CurrentExitCode` (`handler-invoker-emitter.cs`). Standalone `RenderAsync` still starts at 0 (`routing-35`). Two concurrent daemon invokes with distinct codes and directories return both (`daemon-04`, including soak). Two concurrent McpHost calls, in-process and forwarded, return each handler's code (`mcphost-03`).
- Suggestion: none
- Status: fixed

## Issues

<!-- No new issues. -->
