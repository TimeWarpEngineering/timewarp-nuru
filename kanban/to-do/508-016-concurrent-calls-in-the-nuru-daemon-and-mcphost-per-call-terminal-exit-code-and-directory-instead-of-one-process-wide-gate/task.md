# Concurrent calls in the Nuru daemon and McpHost: per-call terminal, exit code and directory instead of one process-wide gate

## Description

`TimeWarp.Nuru.Daemon` and `TimeWarp.Nuru.McpHost` (4.0.0-beta.3) run every call behind a process-wide
`SemaphoreSlim CallGate = new(1, 1)`:

- `source/timewarp-nuru-daemon/nuru-daemon-server.cs` (`RunGatedAsync`, ~line 515)
- `source/timewarp-nuru-mcphost/nuru-mcp-tool-invoker.cs` (in-process path, ~line 70)

The Design region of the daemon server gives the reasons: generated route execution uses process-wide
service state, `Environment.ExitCode` and the current directory, and console capture swaps
`Console.Out`/`Console.Error`. So a served app can only ever run one call at a time.

That is fine for short CLI calls and wrong for the case the daemon exists for: a warm server shared by
several clients. Roslynk is moving onto the Nuru daemon and McpHost as the proving ground (decision
2026-10-20: the fork drops byte-identical MCP parity in favour of the cleanest Nuru architecture). Roslynk
needs concurrency:

- Read operations run in parallel against pinned immutable snapshots; `multi_query` runs its operations in
  parallel inside one call.
- `get_diagnostics` with analyzers or a first `open_solution` can take tens of seconds. Behind the gate every
  other agent's call waits for it.
- Several agent sessions (MCP over stdio, plus CLI calls) share one warm workspace; that is the point of the
  daemon.

Remove the process-wide state from the call path so calls run concurrently by default:

- **Output:** each call renders into its own `ITerminal` (`NuruApp.RenderAsync(request, result, terminal)`
  already takes one). No `Console.SetOut`/`SetError` swap on the call path; the daemon and McpHost return the
  per-call terminal's text. Anything that still writes `Console` directly from inside a handler is a
  documented limitation (or captured per call through an `AsyncLocal`-routed writer, if that is clean).
- **Exit code:** the call's exit code is a per-call value returned by render/invoke, not
  `Environment.ExitCode`. `RunAsync` for a normal CLI process still sets `Environment.ExitCode` at the end.
- **Current directory:** the client's working directory travels as per-call context (an injectable
  call-context service and/or `AsyncLocal`), not `Directory.SetCurrentDirectory`. Document what a handler
  must use to resolve relative paths.
- **Service state:** audit what generated route execution keeps process-wide (scoped vs singleton services,
  generated DI, 508-007 lazy singletons) and make per-call scope real, so two calls never share a request
  scope.
- **Opt-in serialization:** a route (or the app) can still ask to run exclusively, for handlers that are not
  thread-safe. Default is concurrent. Name it plainly (for example `[NuruExclusive]` or an options flag).
- **Cancellation:** a client disconnect, an MCP `notifications/cancelled`, or Ctrl-C on the forwarding CLI
  cancels that call's token only, and the handler sees it. Test it with a long-running handler.
- `ping` and `stop` keep working while calls run; the idle timer still counts from the last call to finish and
  never fires while any call runs.

## Requirements

- Two calls to a handler that blocks until released run concurrently through the daemon (forwarding CLI)
  and through McpHost (in-process and forwarded); each gets its own output and exit code; neither sees the
  other's working directory.
- A call marked exclusive still serializes against other exclusive calls.
- Cancelling one in-flight call cancels only it.
- A soak test: N parallel forwarded calls with mixed success and problem results return the right text and
  exit code to the right caller.
- Timings recorded: forwarded call latency single vs 8 parallel, before and after.
- Docs: daemon and McpHost pages describe the concurrency model, the per-call context API, and the
  exclusive opt-in. Design regions updated.

## Non-goals

- Verbatim rendering / problem renderer (508-011).
- McpHost schema changes (508-015).
- Changing the socket protocol beyond carrying working directory and cancellation if needed (508-010 owns
  the wire contract).

## Checklist

- [ ] Per-call terminal on the daemon and McpHost paths; no console swap
- [ ] Per-call exit code; `RunAsync` keeps setting `Environment.ExitCode` for plain CLI runs
- [ ] Per-call working directory context; no `SetCurrentDirectory` on the call path
- [ ] Per-call service scope audited and enforced
- [ ] Exclusive opt-in
- [ ] Cancellation per call over the socket and over MCP
- [ ] Concurrency, isolation, cancellation and soak tests; timings in Results
- [ ] Docs and Design regions

## Session

- Created: 2532391 (2026-10-20) by the Roslynk cockpit. Steven decided 2026-10-20 that the Roslynk fork moves
  onto the Nuru daemon and McpHost as the proof of Nuru's served-app architecture, dropping byte-identical
  MCP parity; this gate is the blocker found while planning that move.

## Notes

- Related: 508-004 (McpHost), 508-005 (daemon), 508-007 (lazy singletons), 508-010 (wire contract), 508-011,
  508-015, Roslynk task "move the daemon, stdio and CLI onto the Nuru served app".
