# Round 1 — merged findings
**Date:** 2026-10-20
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 1 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

### M1 — Severity: bug — Status: fixed
- File: source/timewarp-nuru/nuru-app.cs:208
- Description: `InvokeAsync` runs the handler in the call's `NuruCallContext` and discards the executor exit code after checking `Dispatched`. `RenderAsync` then starts a new context (`nuru-app.cs:208`) whose directory is `NuruCallContext.CurrentDirectory`. Invoke has already restored the ambient context, so that is the process directory, and generated `ExecuteRouteAsync` sets `CurrentExitCode = 0` before rendering (`interceptor-emitter.cs:225`). The render path returns that fresh code. Problem-details status is written during render, so those codes still come back. A handler that sets `NuruCallContext.CurrentExitCode` does not. `NuruMcpToolInvoker.InvokeAsync` (`nuru-mcp-tool-invoker.cs:104`) and daemon `Session.InvokeAsync` (`nuru-daemon-server.cs:500`) both publish the render code as the call result. In-process MCP maps a non-zero render code to `CallToolResult.IsError`. Forwarded MCP uses daemon invoke, so it drops the same value. `documentation/user/features/mcp-hosting.md:124` tells the handler to set `NuruCallContext.CurrentExitCode` for the call. Forwarded CLI `ExecuteAsync` is one context and keeps the handler code (`daemon-04`, `routing-35`). A renderer that calls `NuruCallContext.GetFullPath` during render also resolves against the process directory, after the handler saw the client directory. No test sets `CurrentExitCode` on an MCP or daemon-invoke call.
- Suggestion: Seed the render context with the invoke call's `ExitCode` and `WorkingDirectory`, and skip the opening `CurrentExitCode = 0` when the execution is a render, so problem-details rendering can still overwrite the code. Return that code from McpHost and from daemon invoke. Cover two concurrent McpHost calls (in-process and forwarded) that set distinct `CurrentExitCode` values and resolve a relative path while rendering.
- Source: general
- Disposition notes: Fixed in the round-2 fix pass (2026-10-20). New `NuruApp.RenderAsync(request, result, terminal, context)` renders in a context seeded from the invoke call (exit code, working directory, console capture, exclusive hold) and writes the result back to `context.ExitCode`; generated `ExecuteRouteAsync` no longer resets `CurrentExitCode` on a render, so problem details still overwrite it. `InvokeAsync(request, context)` stores the executor code on the context. McpHost in-process and daemon `invoke` (so forwarded MCP too) render with the call's context. Tests: routing-35 `Should_carry_the_invoked_calls_exit_code_and_directory_into_render`, daemon-04 `Should_return_the_exit_code_and_directory_of_each_invoked_call_through_render` (two concurrent invokes, distinct codes and cwds, renderer resolves a relative path), mcphost-03 `Should_return_each_tool_call_the_exit_code_its_handler_set` and `Should_return_each_forwarded_tool_call_the_exit_code_its_handler_set`. Docs: mcp-hosting, agent-invocation, served-apps, tw-nuru skill, changelog, both host Design regions.

## Duplicates / conflicts

- Single reviewer. M1 is general issue 1.
