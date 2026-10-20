# Round 1 — general
**Date:** 2026-10-20
**Scope reviewed:** branch `task/508-016-concurrent-calls-in-the-nuru-daemon-and-mcphost-pe` vs `master` (`52fdd906`)

## Summary

The branch removes the process-wide `CallGate` and runs each daemon and McpHost call in a `NuruCallContext`: own terminal, `AsyncLocal` console router, per-call generated DI and MS DI scope, `[Exclusive]` / `.Exclusive()` plus host `ExclusiveCalls`, and disconnect or `notifications/cancelled` scoped to that call. Forwarded CLI `ExecuteAsync` keeps the handler exit code, the idle timer waits while `ActiveCalls` is non-zero, and the concurrency tests cover that CLI path. The invoke-then-render path used by McpHost (in-process and forwarded) and by daemon `invoke` builds a fresh context, so the handler's per-call exit code and working directory do not survive into the result the caller sees.

## Issues

### Issue 1 — Severity: bug
- File: source/timewarp-nuru/nuru-app.cs:208
- Description: `InvokeAsync` runs the handler in the call's `NuruCallContext` and discards the executor exit code after checking `Dispatched`. `RenderAsync` then starts a new context (`nuru-app.cs:208`) whose directory is `NuruCallContext.CurrentDirectory`. Invoke has already restored the ambient context, so that is the process directory, and generated `ExecuteRouteAsync` sets `CurrentExitCode = 0` before rendering (`interceptor-emitter.cs:225`). The render path returns that fresh code. Problem-details status is written during render, so those codes still come back. A handler that sets `NuruCallContext.CurrentExitCode` does not. `NuruMcpToolInvoker.InvokeAsync` (`nuru-mcp-tool-invoker.cs:104`) and daemon `Session.InvokeAsync` (`nuru-daemon-server.cs:500`) both publish the render code as the call result. In-process MCP maps a non-zero render code to `CallToolResult.IsError`. Forwarded MCP uses daemon invoke, so it drops the same value. `documentation/user/features/mcp-hosting.md:124` tells the handler to set `NuruCallContext.CurrentExitCode` for the call. Forwarded CLI `ExecuteAsync` is one context and keeps the handler code (`daemon-04`, `routing-35`). A renderer that calls `NuruCallContext.GetFullPath` during render also resolves against the process directory, after the handler saw the client directory. No test sets `CurrentExitCode` on an MCP or daemon-invoke call.
- Suggestion: Seed the render context with the invoke call's `ExitCode` and `WorkingDirectory`, and skip the opening `CurrentExitCode = 0` when the execution is a render, so problem-details rendering can still overwrite the code. Return that code from McpHost and from daemon invoke. Cover two concurrent McpHost calls (in-process and forwarded) that set distinct `CurrentExitCode` values and resolve a relative path while rendering.
- Status: open
