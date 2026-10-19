# Served endpoints: daemon host and forwarding client for Nuru apps

## Description

A framework-level way for a Nuru app to run as a long-lived server and have its CLI forward routes to that server instead of handling them in-process. Working name from the client-server eval: `TimeWarp.Nuru.Daemon`. Today an app that needs warm state (Roslynk's loaded Roslyn workspaces) must write a nested forwarding `Handler` per endpoint and its own client, transport, daemon discovery and lifetime handling.

Backlog until task 506 reports cold-vs-warm timing for Roslynk. If the gap is small the daemon is nice-to-have and this task is archived; if large, it moves to to-do.

## Requirements (when promoted)

- `serve` built-in route (or builder option) that hosts the app's endpoints behind a per-user local transport (Unix domain socket or named pipe preferred over a fixed TCP port; loopback HTTP acceptable as a first transport).
- Client mode: when a server is reachable (or `--daemon` is set), the generated CLI serialises the bound request (508-003's binder output), sends it, and renders the returned typed result exactly as in-process invocation would. Probe-or-start semantics with idle shutdown.
- A route can opt out of forwarding (always local), for example `serve` itself and version/help.
- Behaviors: define which chain runs where (CLI-side telemetry versus server-side validation).
- Works with 508-004 so the MCP host and the CLI share one running server.

## Goal (implementer restatement)

Task 506 measured the gap: a fresh Roslynk process pays about 6 s (solution load plus first compile), a warm call about 26 ms. Its verdict is GREEN for a client that starts a process per command, so this task is promoted, not archived.

Ship `TimeWarp.Nuru.Daemon` (`source/timewarp-nuru-daemon/`): an opt-in package that lets any Nuru app run as a long-lived local server and have its own CLI forward routes to it, with no per-route code.

Acceptance criteria:

1. `AddServeRoute(...)` on the builder (generator-interpreted, like `AddMcpRoute()`) gives the app a built-in `serve` route that hosts the app's routes on a per-user local transport: a Unix domain socket (AF_UNIX works on Linux, macOS and Windows 10+), in a per-user directory, never a TCP port. `serve --status` and `serve --stop` report and stop a running server.
2. Client mode: with `AddServeRoute()`, a matched route is forwarded to a reachable server instead of running in-process. The server runs the same generated match, bind and handler code, and the client prints the server's stdout and stderr and returns its exit code, so the output equals in-process invocation. `--json-args` calls forward too (stdin and `@path` are resolved on the client side).
3. Probe-or-start: no server means run locally, unless `--daemon` is passed or `autoStart` is set, which starts `<app> serve` detached and waits for it. `--no-daemon` forces local. The server stops after an idle timeout. A server from a different build of the app is stopped instead of used.
4. Opt-out: `[LocalOnly]` on an endpoint class, or `.LocalOnly()` on a fluent route, keeps the route in the CLI process; the catalog carries `localOnly: true`. Built-ins (help, version, capabilities, completion, `serve`, `mcp`) never forward.
5. Behavior placement is defined and documented: argv parsing, type conversion and built-ins run on the CLI; Nuru behaviors (validation, telemetry behaviors) and the handler run on the server, once.
6. Works with 508-004: the McpHost `tools/call` path forwards to the same running server (the `invoke` operation binds the JSON arguments with the public binder), so the MCP host and the CLI share one warm server.
7. Tests (protocol round trip, forwarding through a real CLI process, opt-out, catalog member, MCP forwarding) and docs (a page beside `mcp-hosting.md`, built-in routes, changelog), with a transport and discovery design note.

## Checklist

- [x] Promote or archive from task 506 timing results (promoted: 506 verdict GREEN)
- [x] Transport and discovery design note
- [x] Server host and forwarding client
- [x] Opt-out and behavior placement
- [x] Tests and docs

## Notes

- Parent: 508. Reference design: SimCubeLtd/roslynk-cli (System.CommandLine client plus local daemon). Roslynk already has `DaemonLauncher` and an idle-eviction service in its MCP host that do this by hand.
- Related eval: timewarp-intel `evaluations/2026-10-15-nuru-client-server.md`.

## Session

- Created: 1479989 (2026-10-17)
- Implemented: implementer-claude (claude-opus-5-5) under `ganda task work`, 2026-10-19. Promoted from 506's GREEN verdict. Package, generator hooks, McpHost forwarding, sample, tests, docs. Full CI and `ganda repo audit` green. The task folder stays in `backlog/` on this branch because the host-owned walk logs live beside the kitchen; the host's `kanban done` moves it.

## Results

### Decision from task 506

Promoted, not archived. Task 506 measured about 6 s cold (solution load plus first compile) and about 26 ms for a warm call that still does work. Its verdict was GREEN for a client that starts a process per command.

### Transport and discovery design note

- **Transport**: a Unix domain socket (AF_UNIX) on Linux, macOS, and Windows 10 1803+. One transport, no TCP port. Loopback HTTP was not needed. Each connection carries one request line and one response line of UTF-8 JSON, protocol version 1. The requests are `ping`, `stop`, `run` (argv plus the caller's cwd), and `invoke` (route plus a JSON object for `TryBind`, used by the MCP host). It is written with `Utf8JsonWriter` / `JsonDocument`, so there is no reflection and the package is `IsAotCompatible`.
- **Discovery**: the socket is `$XDG_RUNTIME_DIR/nuru/<app>.sock`, else `<temp>/nuru-<user>/<app>.sock` (directory created 0700), else `%LOCALAPPDATA%\TimeWarp.Nuru\daemon\<app>.sock` on Windows. `<app>` is the catalog name, hashed when the path would pass 100 bytes. Override order: `AddServeRoute(socketPath)`, then `NURU_DAEMON_SOCKET`, then the default.
- **Identity**: `ping` returns name, version, pid, socket, and a build id (name, version, entry file, entry write time). A client from another build stops that server instead of using it.
- **Start and lifetime**: probe-or-start. No server runs the call locally, unless `--daemon` or `autoStart` is set. In that case the client starts `<exe> serve` (or `dotnet <entry.dll> serve`) with stdio redirected, under `setsid` on Linux, and waits up to `StartTimeout` (30 s). The server stops after the idle timeout (default 600 s, counted from the end of the last call), on `serve --stop`, or on SIGINT or SIGTERM, and then deletes the socket. `serve` refuses to start when another server answers. A stale socket file is deleted.
- **Cancellation**: the server cancels a call's token when its client disconnects before the response.

### Package `TimeWarp.Nuru.Daemon` (`source/timewarp-nuru-daemon/`)

- `AddServeRoute(socketPath, idleTimeoutSeconds, autoStart)`: a builder stub that the generator interprets. The arguments are compile-time constants.
- `NuruDaemonServer.RunAsync / StatusAsync / StopAsync / IsServing(app)`. Calls run under one process-wide gate, in the caller's directory, with the console captured. `ping` and `stop` are answered outside the gate.
- `NuruDaemonClient : INuruRouteForwarder`: `ForwardAsync` (probe, build check, start, forward, decline), plus `PingAsync`, `StopAsync`, `RunAsync(args)`, and `InvokeAsync(route, json)`. A request is never retried locally once it has been sent.
- `NuruDaemonOptions`, `NuruDaemonPaths`, `NuruDaemonStatus`, `NuruDaemonException`.
- `ProcessStartInfo` is used in `nuru-daemon-launcher.cs` only, with a scoped `RS0030` pragma. The reason: Amuru's `Shell.Builder` awaits the child and owns its streams, and a detached daemon needs the opposite. Console access is confined to `console-redirect.cs`.
- A Release build packs `TimeWarp.Nuru.Daemon.4.0.0-beta.1.nupkg`.

### Core (`timewarp-nuru`)

- `INuruRouteForwarder`, `NuruForwardRequest` (`ForArgs`, `ForInvoke`), `NuruForwardResult`, `NuruForwardMode`, `NuruForwarding` (`PeelFlags`, `TryForwardAsync`, `WriteResult`).
- `NuruApp.RouteForwarder`. `NuruApp.ExecuteAsync(args)` with its generated `RouteExecutor`: argv routing for code the generator does not intercept. The daemon server uses it, and so does any test that builds its app in another method.
- `[LocalOnly]`, plus `.LocalOnly()` on `EndpointBuilder` and `GroupEndpointBuilder`. `EndpointCapability.LocalOnly` (`bool?`, omitted when null).

### Generator

- `AddServeRoute()`: `DslInterpreter` reads it into `ServeRouteModel` on `AppModel.ServeRoute`. The `ExtractConstantArgument` error text now names the method.
- Interceptor:
  - The `Build()` interceptor attaches `RouteExecutor` and, for served apps, `RouteForwarder = new NuruDaemonClient(__DaemonOptions())`. The `ExecuteRouteAsync` fallback attaches the forwarder only when `Build()` was not intercepted, so `RouteForwarder = null` sticks.
  - `--daemon` / `--no-daemon` are peeled into `__forwardMode` before routing.
  - Built-in `serve`, `serve --status`, and `serve --stop` come after user routes.
  - Help rows. The JSON built-in predicate knows the serve forms.
- Route matcher: for a non-local route in a served app, `NuruForwarding.TryForwardAsync(app, pattern, args, null, mode, ct)` runs after type conversion and before the missing-handler check and the behavior pipeline. Primary and alias matches both have it. It is skipped in the REPL.
- JSON dispatch: once the route is selected, the forwarder gets the resolved object (`__jsonDoc.RootElement.GetRawText()`) before the bind case. The client inlines that object in place of the `--json-args` value.
- `[LocalOnly]` / `.LocalOnly()` set `RouteDefinition.IsLocalOnly`. The capabilities emitter emits `LocalOnly = true` only then, so other documents are byte-identical.

### Behavior placement (defined)

The CLI runs argv matching, type conversion, parse errors, built-ins, local-only routes, and writing the forwarded output. The server runs Nuru behaviors (validation, telemetry behaviors, retry, authorization), the handler, services, the command scope, and rendering. Each behavior runs once per call, where the handler runs.

### McpHost (508-004 integration)

`NuruMcpToolInvoker.CallAsync` sends a call to a non-local route to `App.RouteForwarder` (`invoke` op) before the local path. The result follows the same rules: a non-zero exit is `isError` with stderr, and a bind error keeps the `--json-args` message. JSON structured content is parsed from the rendered text using the catalog return type. So `myapp mcp` and the CLI share one warm server.

### Sample

`samples/endpoints/16-daemon/served-app.cs` has endpoints `count` (lazy warm index), `pid`, `[LocalOnly] local-pid`, and `fail {code}`, and fluent routes `cwd` and `.LocalOnly()` `local-echo`, plus `AddServeRoute` and `AddMcpRoute`. One run on this machine (WSL2, Debug, apphost exe): `count hello` took 1.07 s in-process and 0.08 s forwarded to a warm server. `--daemon` (start plus first load) took 1.19 s.

### Tests

- `tests/timewarp-nuru-daemon-tests/daemon-01-server.cs` (10): in-process server on a real socket. Covers ping/build id, argv output, stderr and exit code, unknown command, cwd, binder invoke with a bind error equal to `TryBind`, cancel on disconnect (the gate is released), stop plus socket removal, idle shutdown, refusing a second server, the forwarder declining (no server, `Never`, served app), forwarding from another instance, inlined `--json-args`, and default socket paths.
- `daemon-02-forwarding.cs` (14): generated hooks with a recording forwarder. Covers the client attached, forward plus printing plus exit code, endpoint routes, decline means local, `--daemon` gives Require with the flag removed, `--no-daemon`, local-only (fluent and attribute) and built-ins never forwarded, parse errors local, `--json-args` resolved object, `RouteForwarder = null`, catalog `localOnly`, help rows, `serve --status/--stop` with no server, and `PeelFlags`.
- `daemon-03-served-app.cs` (1, about 30 s): the sample as real processes. Covers `--daemon` start, same server pid on later calls, local-only and `--no-daemon` local, caller cwd, stderr and exit 3, `--json-args -` from stdin, an MCP client on `served-app mcp` getting the server pid (local-only stays local, bind error is `isError`), `serve --stop`, and `--status`.
- `mcphost-01-tools.cs` gains `Should_send_tool_calls_to_the_route_forwarder` (15 total).
- CI wiring: the `tests/ci-tests` glob and ProjectReference, `timewarp-nuru.slnx`, and `dev build` (`build-command.cs`).
- Full CI after `ganda runfile cache --clear`: multi-mode 1991 total, 1985 passed, 6 skipped, 0 failed. Standalone 0 failed.
- `ganda repo audit`: 30 passed, 0 failed, nothing to fix.

### Docs

- New `documentation/user/features/served-apps.md` covers the quick start, how a call is forwarded, which routes forward, behavior placement, transport and discovery, MCP, things to know, and the API.
- `built-in-routes.md` gains the serve rows and section. `overview.md` gains an entry. `agent-invocation.md` gains `localOnly`. `mcp-hosting.md` gains the forwarding note.
- Also updated: `skills/tw-nuru/SKILL.md`, the changelog (Added: Served apps, Forwarding contract), the package readme, and `samples/endpoints/readme.md`.

### Notes and follow-ups (not done here)

- **Source-generated DI builds singletons eagerly** in `EnsureServicesInitialized`, on every CLI call, including forwarded ones and `--version`. So a served app must load warm state lazily (the sample uses `Lazy<T>`, and the docs say so). Making singletons lazy in the generator would let the CLI skip that cost without app changes. It is worth its own task.
- The server runs calls one at a time (process-wide service state). Concurrent calls queue.
- A forwarded call uses the server's environment variables. Output has no color (the server has no TTY).
- Windows AF_UNIX is supported by .NET but was not exercised here (Linux only). The `setsid` wrapper is Linux-only. On macOS, a Ctrl+C during the very first `--daemon` start can reach the new server.
- The next release publishes a new package ID, `TimeWarp.Nuru.Daemon`. As with McpHost, confirm the NuGet trusted-publishing policy covers new IDs.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-daemon-tests/daemon-01-server.cs
dotnet run tests/timewarp-nuru-daemon-tests/daemon-02-forwarding.cs
dotnet run tests/timewarp-nuru-daemon-tests/daemon-03-served-app.cs
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs
export NURU_DAEMON_SOCKET=/tmp/nuru-smoke/served.sock
S=samples/endpoints/16-daemon/served-app.cs
dotnet run $S -- local-pid
dotnet run $S -- --daemon pid
dotnet run $S -- pid
dotnet run $S -- local-pid
dotnet run $S -- fail 3; echo "exit=$?"
dotnet run $S -- serve --status
dotnet run $S -- serve --stop
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- `daemon-01-server.cs`: Total 10, Passed 10.
- `daemon-02-forwarding.cs`: Total 14, Passed 14.
- `daemon-03-served-app.cs`: Total 1, Passed 1 (about 30 s).
- `mcphost-01-tools.cs`: Total 15, Passed 15.
- Sample:
  - `--daemon pid` and the next `pid` print the same number (the server's pid).
  - Each `local-pid` prints a different number (the CLI's own pid).
  - `fail 3` prints `failing with 3` on stderr and `exit=3`.
  - `serve --status` prints `served-app server is running: pid <n>, ...`.
  - `serve --stop` prints `served-app server stopped.`.
- CI runner: multi-mode 1991 total, 1985 passed, 6 skipped, 0 failed. Standalone 0 failed.
