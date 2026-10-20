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

## Goal (implementer restatement)

Take the process-wide `CallGate` out of `TimeWarp.Nuru.Daemon` and `TimeWarp.Nuru.McpHost` so one warm server
runs many calls at once, and give each call its own state instead of process state:

- **Per-call context (core):** a `NuruCallContext` that flows with the call (`AsyncLocal`): its own terminal, working
  directory and exit code. `NuruApp.Terminal`, the DI-injected `ITerminal`, and the generated exit-code reads and
  writes follow it. Outside a call context (a plain CLI run, the REPL) nothing changes: `Environment.ExitCode` and the
  app terminal as today, and `RunAsync` still leaves the exit code in `Environment.ExitCode`.
- **Hosts:** daemon (`run`, `invoke`) and McpHost (in-process) run each call in its own context with its own
  `TestTerminal`; no `Console.SetOut`/`SetError` and no `SetCurrentDirectory` per call. Handler `Console` writes are
  routed to the current call by an `AsyncLocal` writer installed once (not per call).
- **Service scope:** generated DI keeps per-call state (command/session fields, disposables, Scoped instances, the
  MS DI command scope and Nuru command/session caches) per call when a call context is active, so two concurrent calls
  never share or dispose each other's scope. Singletons stay shared.
- **Exclusive opt-in:** `[Exclusive]` / `.Exclusive()` routes, and an `ExclusiveCalls` host option, serialize against
  other exclusive calls on the same app; default is concurrent.
- **Cancellation:** a client disconnect, MCP `notifications/cancelled`, or the forwarding CLI's cancel cancels only that
  call's token.
- `ping`/`stop` answer while calls run; the idle timer never fires while any call runs.
- **Invoke then render is one call (review round 1, M1):** the exit code a host returns for a bound call is the
  call's one value. Render continues the invoke call's context: it starts from the exit code the handler left in
  `NuruCallContext.CurrentExitCode` and resolves paths against the call's working directory; problem details written
  during render still overwrite the code. McpHost (in-process and forwarded) and daemon `invoke` return that code.

Acceptance: concurrency, isolation (output, exit code, working directory), exclusive, cancellation and soak tests pass
through the daemon (forwarded CLI) and McpHost (in-process and forwarded); the existing suites stay green; timings for
single vs 8 parallel forwarded calls before and after are in Results; daemon and McpHost docs and Design regions
describe the concurrency model, `NuruCallContext`, and the exclusive opt-in. Two concurrent McpHost calls
(in-process and forwarded) whose handlers set distinct `NuruCallContext.CurrentExitCode` values each get their own code
back, and a renderer that resolves a relative path during render sees the call's directory.

## Checklist

- [x] Per-call terminal on the daemon and McpHost paths; no console swap
- [x] Per-call exit code; `RunAsync` keeps setting `Environment.ExitCode` for plain CLI runs
- [x] Per-call working directory context; no `SetCurrentDirectory` on the call path
- [x] Per-call service scope audited and enforced
- [x] Exclusive opt-in
- [x] Cancellation per call over the socket and over MCP
- [x] Concurrency, isolation, cancellation and soak tests; timings in Results
- [x] Docs and Design regions

## Session

- Created: 2532391 (2026-10-20) by the Roslynk cockpit. Steven decided 2026-10-20 that the Roslynk fork moves
  onto the Nuru daemon and McpHost as the proof of Nuru's served-app architecture, dropping byte-identical
  MCP parity; this gate is the blocker found while planning that move.

- 2026-10-20 implement (claude, headless): a first implement run wrote the product, tests and docs but
  stopped before committing; the resumed run verified it and committed. Build green; new tests, existing
  daemon/McpHost suites, full CI suite and 66/66 samples pass; `ganda repo audit` passes. Timings taken
  against a `git archive master` export (before) and this branch (after).
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-20T07:02:44Z
- Reviewer: grok session 01a1581e-a3a7-76b2-ba52-ec583804afec (2026-10-20). Round 1, effort 3, roster general.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-20T07:17:46Z
- 2026-10-20 implement-fix-2 (claude, headless): fixed review M1. Render continues the invoke call's context
  (`RenderAsync(..., context)`), generated code no longer resets the exit code on a render, and both hosts render with
  the call. New tests in routing-35, daemon-04, mcphost-03. Full CI suite 0 failed (multi-mode 2028 passed),
  daemon-01..03 and mcphost-01..02 pass, `dev verify-samples` 66/66, `ganda repo audit` passes.

## Results

**Core (`TimeWarp.Nuru`)**
- `NuruCallContext` (`source/timewarp-nuru/invocation/nuru-call-context.cs`): per-call `Terminal`, `WorkingDirectory`,
  `ExitCode`, `CaptureConsole`, flowing by `AsyncLocal`. Static helpers `CurrentExitCode`, `CurrentDirectory`,
  `GetFullPath(path)` fall back to `Environment.ExitCode` / `Environment.CurrentDirectory` in a plain CLI run.
- `NuruApp.ExecuteAsync(args, context)` and `NuruApp.InvokeAsync(request, context)` run a call in a context;
  `RenderAsync(request, result, terminal, context)` renders that call's result as the rest of the call: it starts from
  the exit code the handler set and the call's directory (problem details overwrite the code) and writes the result to
  `context.ExitCode`. Standalone `RenderAsync(request, result, terminal)` runs in its own context at exit code 0, never
  `Environment.ExitCode`. `NuruApp.Terminal` follows
  the current call. Plain `RunAsync` is unchanged and still leaves the exit code in `Environment.ExitCode`.
- `NuruCallTerminal`: the DI-injected `ITerminal` (generated DI, runtime MS DI, behaviors) forwards to the running
  call's terminal, so singletons built once still write to the right call.
- `NuruConsoleRouter`: installed once as `Console.Out`/`Error`; routes writes made inside a capturing call to that
  call's terminal. No per-call `Console.SetOut`.
- Service scope: generated DI moves session/command fields, disposables, REPL-style Scoped fields and the MS DI command
  scope onto a per-call `__NuruCall` (`AsyncLocal`); `NuruCommandCache`, `NuruSessionCache` and `NuruCommandLease`
  keep per-call state inside a context. Singleton init (`EnsureServicesInitialized`, service provider build) is under
  a lock so concurrent first calls never build singletons twice. Singletons stay shared.
- Exclusive opt-in: `[Exclusive]` (endpoint classes) and `.Exclusive()` (fluent, incl. group endpoints); generated code
  takes the app's `NuruExclusiveLease` around behaviors + handler. Host-wide: `AddServeRoute(exclusiveCalls: true)`,
  `NuruDaemonOptions.ExclusiveCalls`, `NuruMcpHostOptions.ExclusiveCalls`. A call already holding the gate does not
  wait for itself.

**Hosts**
- Daemon: process-wide `CallGate` removed; each call gets its own `TestTerminal` + `NuruCallContext` (client's working
  directory, `CaptureConsole = true`). No `SetCurrentDirectory`, no console swap, no `Environment.ExitCode`. Client
  disconnect cancels only that call. Listen backlog is the system maximum. `ping`/`stop` never wait; idle timer
  unchanged (counts from last call end, never fires while one runs; tested).
- McpHost: process-wide `CallGate` removed; same per-call context; `notifications/cancelled` cancels only that
  request's token; forwarded calls are cancelled in the server by closing that call's connection.

**Tests** (all pass; also in the CI multi-mode assembly)
- `tests/timewarp-nuru-tests/routing/routing-35-call-context.cs` (7): concurrent calls with own output/exit code/
  directory, invoke-then-render keeps the handler's exit code and directory, plain-run exit code, exclusive
  serialization, single-call cancellation, per-call service scope (generated DI and runtime MS DI).
- `tests/timewarp-nuru-daemon-tests/daemon-04-concurrency.cs` (6): concurrent forwarded calls with isolated output,
  exit code and cwd; concurrent `invoke` calls whose handlers set distinct exit codes, rendered in their own cwd; exclusive routes + `ExclusiveCalls`; cancel one client only; no idle stop while a call runs;
  soak of mixed argv/binder, success/problem calls returning each caller its own result.
- `tests/timewarp-nuru-mcphost-tests/mcphost-03-concurrency.cs` (6): concurrent tool calls over one session, handler
  `CurrentExitCode` returned per call (in-process and forwarded; non-zero is `isError`), cancel one call only, `ExclusiveCalls` in-process, forwarded calls concurrent and cancelled alone.
- Shared helpers: `tests/shared/call-probes.cs` (incl. `CcPathResult` / `CcPathRenderer`, a renderer that resolves a
  relative path while rendering). `daemon-01-server.cs` updated to the per-call API.
- Existing: daemon-01..03 (15/14/1), mcphost-01..02 (16/2) pass; full `run-ci-tests.cs` passes (0 failed);
  `dev verify-samples` 66/66; `ganda repo audit` passes.

**Timings** (`tests/scripts/benchmark-daemon-concurrency.cs`, in-process server, 10 rounds, Release)

| Route | Before (master, gated) | After (this branch) |
|-------|------------------------|---------------------|
| `bench-noop` single mean | 0.31 ms | 0.31 ms |
| `bench-noop` 8 parallel: per-call mean / batch mean | 0.32 ms / 0.99 ms | 0.38 ms / 1.14 ms |
| `bench-wait 50` single mean | 51.97 ms | 52.01 ms |
| `bench-wait 50` 8 parallel: per-call mean / max / batch mean | 233.81 / 417.25 / 416.16 ms | 51.67 / 53.24 / 52.01 ms |

**Known / limitations**
- A handler that sets `Environment.ExitCode` or uses relative paths via process cwd is not per call in a served app;
  documented (use `NuruCallContext.CurrentExitCode` / `GetFullPath`). Console writes are captured per call through
  the router; a `UseTerminal(custom)` terminal is not used for forwarded calls (documented).
- `./bin/dev format` reports whitespace issues in `endpoint-extractor.cs` (lines this task did not change),
  `add-configuration-locator.cs`, both `console-redirect.cs` files and `mcphost/global-usings.cs`. They are
  pre-existing, `dev format` is not a CI or audit gate, and the build is clean, so they were left alone.

### How to validate

**Smoke**
```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-tests/routing/routing-35-call-context.cs
dotnet run tests/timewarp-nuru-daemon-tests/daemon-04-concurrency.cs
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-03-concurrency.cs
dotnet run -c Release tests/scripts/benchmark-daemon-concurrency.cs
```

**Expect**
- The three test runfiles report `Total: 7 / 6 / 6`, all passed.
- The benchmark prints `bench-wait 50 ... 8 parallel: ... batch mean` near 52 ms (≈ one call), not ≈ 416 ms
  (eight calls back to back as on master).
- `dotnet run tests/ci-tests/run-ci-tests.cs` finishes with 0 failed and lists `CallContext`, `DaemonConcurrency`,
  `McpHostConcurrency`.

**Review (round 1)**
- Effort 3, roster general. Counts: 0 open, 1 fixed (M1: invoke-then-render dropped the handler's exit code and
  directory), 0 wontfix. Ledger with disposition notes: `review/round-1/merged.md`.

## Learnings

### Misread

The restatement says `RenderAsync` runs in its own `NuruCallContext`, so its exit code belongs to that render and is never `Environment.ExitCode`. The task says the call's exit code is one per-call value returned by render/invoke, and that McpHost (in-process and forwarded) returns that exit code along with the call's output. Following the restatement, render starts a fresh context at exit code 0, which drops the handler's `NuruCallContext.CurrentExitCode` on the invoke-then-render path. See finding M1 in `review/round-1/merged.md` (fixed in the round-2 fix pass).

## Review

- Round 1: Blocked — open M1
- Round 2 fix pass (2026-10-20): M1 fixed; see `review/round-1/merged.md` disposition notes.

## Notes

- Related: 508-004 (McpHost), 508-005 (daemon), 508-007 (lazy singletons), 508-010 (wire contract), 508-011,
  508-015, Roslynk task "move the daemon, stdio and CLI onto the Nuru served app".
