# Served apps

`TimeWarp.Nuru.Daemon` lets a Nuru app run as a long-lived local server and have its own CLI forward routes to it. An app that loads expensive state, such as a Roslyn workspace, pays the load once in the server. Every later CLI call, and every MCP `tools/call`, uses the warm state. You write no client, transport, or forwarding handler per route.

Task 506 measured the gap for Roslynk: a fresh process took about 6 s (solution load plus first compile), and a warm call about 26 ms. A served app removes that load for a client that starts a new process per command.

## Quick start

Reference the package and call `AddServeRoute()`:

```csharp
#:project path/to/timewarp-nuru-daemon.csproj   // or: <PackageReference Include="TimeWarp.Nuru.Daemon" />

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddServeRoute(idleTimeoutSeconds: 600)
  .Build();

return await app.RunAsync(args);
```

```bash
myapp count hello            # no server: runs in this process (cold)
myapp --daemon count hello   # starts the server in the background, forwards the call
myapp count hello            # server running: forwarded (warm)
myapp --no-daemon count x    # always runs in this process
myapp serve                  # run the server in the foreground (Ctrl+C stops it)
myapp serve --status         # is a server running? exit 0 yes, 1 no
myapp serve --stop           # stop it
```

`samples/endpoints/16-daemon/served-app.cs` is a complete example. In one run of that sample on a Linux workstation, a call that loads its state took 1.07 s in-process and 0.08 s forwarded to a warm server.

`AddServeRoute` is interpreted by the source generator, as `AddMcpRoute()` is. Its arguments must be compile-time constants:

| Argument | Default | Meaning |
|----------|---------|---------|
| `socketPath` | null | Unix domain socket path. Null uses `NURU_DAEMON_SOCKET`, then the per-user default |
| `idleTimeoutSeconds` | 600 | Seconds without a call before the server stops; 0 never stops |
| `autoStart` | false | When true, a call with no server starts one and forwards, as `--daemon` does |

## How a call is forwarded

With `AddServeRoute()`, the generated CLI does this for every call:

1. It removes `--daemon` and `--no-daemon` from the arguments (only before a `--` separator) and records the mode.
2. It matches the route and converts its arguments, as usual. A parse or conversion error is reported here and is never forwarded.
3. If the matched route is not local-only, it asks `NuruApp.RouteForwarder` to take the call. The forwarder is the `NuruDaemonClient` that `AddServeRoute()` attaches.
4. The client probes the socket. If a server answers, the client sends the arguments and the caller's current directory. It then prints the server's standard output and standard error and returns the server's exit code.
5. If no server answers, the call runs in this process. With `--daemon` or `autoStart`, the client first starts `myapp serve` in the background and waits up to 30 s for it. If the start fails, `--daemon` reports an error and exits 1, and `autoStart` runs the call locally.

The server runs the same generated matching, binding, behaviors and handler code on the same arguments. The output is the text the route would print in-process, and the exit code is the same.

`--json-args` calls are forwarded too. The client reads `-` (stdin) and `@path` itself and sends the JSON object, so the server never reads the client's stdin or resolves a path from its own directory.

A call is never retried locally after its request reached the server, because the handler may already have run. If the server fails mid-call, the CLI prints the error and exits 1.

### Which routes forward

| Route | Where it runs |
|-------|---------------|
| Your routes and endpoints | In the server when one is running (or `--daemon` / `autoStart`), otherwise here |
| `[LocalOnly]` endpoint classes, `.LocalOnly()` fluent routes | Always in the CLI process |
| Built-ins: `--help`, `--version`, `--capabilities`, completion, `--check-updates`, `serve`, `mcp` | Always in the CLI process |
| REPL commands | In the REPL process (it is already warm) |

Mark a route local-only when it reads stdin, prompts, depends on the caller's environment variables, or manages the server:

```csharp
[LocalOnly]
[NuruRoute("login", Description = "Prompt for credentials")]
public sealed class LoginCommand : ICommand<Unit> { /* ... */ }

builder.Map("whoami").WithHandler(() => Environment.UserName).LocalOnly().Done();
```

The catalog carries the mark as `"localOnly": true` on the endpoint (`EndpointCapability.LocalOnly`). Routes without it have no `localOnly` member.

### Where behaviors run

Forwarding happens after argument parsing and before the behavior pipeline:

| Runs in the CLI process | Runs in the server |
|-------------------------|--------------------|
| Argument matching, type conversion, parse errors | Nuru behaviors (`INuruBehavior`): validation, telemetry behaviors, retry, authorization |
| Built-in routes, local-only routes | The handler, its services and the command scope |
| Writing the forwarded output and exit code | Rendering the result (`IResponseRenderer<T>`, problem details, output strategy) |
| Process-level telemetry set-up in `RunAsync` | Process-level telemetry of the server process |

Each behavior runs once per call, in the process that runs the handler. A forwarded call does not run behaviors in the CLI.

## Transport and discovery

- **Transport**: a Unix domain socket (AF_UNIX). It works on Linux and macOS, and on Windows 10 version 1803 and later. No TCP port is opened. Each connection carries one request line and one response line of UTF-8 JSON, with protocol version 1.
- **Location**: `$XDG_RUNTIME_DIR/nuru/<app>.sock` on Linux when that directory exists. Otherwise `<temp>/nuru-<user>/<app>.sock`, created with mode 0700. On Windows it is `%LOCALAPPDATA%\TimeWarp.Nuru\daemon\<app>.sock`. `<app>` is the catalog name, and a long name is replaced by a hash to fit the socket path limit. `socketPath` or `NURU_DAEMON_SOCKET` overrides the location, for example to keep test servers apart.
- **One server per socket**: `serve` refuses to start when another server answers on the socket. A stale socket file with nothing listening on it is deleted.
- **Build check**: the server reports its build: name, version, entry file and that file's write time. A client from a different build stops that server and does not use it, so a rebuilt CLI never runs old server code.
- **Start**: the client starts its own executable (or `dotnet <entry.dll>`) with `serve`, with the standard streams redirected. On Linux it runs under `setsid`, so a Ctrl+C in the caller's terminal does not reach the server. A started server writes nothing to the inherited streams.
- **Lifetime**: the server stops after the idle timeout, which counts from the end of the last call and never expires while a call runs. It also stops on `serve --stop`, Ctrl+C, or SIGTERM, and then removes the socket file.
- **Cancellation**: when a client goes away before the answer (Ctrl+C), the server cancels that call's `CancellationToken`.

## Working with the MCP host

`TimeWarp.Nuru.McpHost` uses the same forwarder. In an app with both `AddServeRoute()` and `AddMcpRoute()`, `myapp mcp` sends each `tools/call` to the running server, so the MCP host and the CLI share one warm server. The server binds the JSON arguments with `TryBind`, runs `InvokeAsync` and `RenderAsync`, and returns the same texts. A bind error is still a tool result with `isError` and the `--json-args` message. Local-only routes run in the `mcp` process. See [MCP hosting](mcp-hosting.md).

A daemon can also host MCP itself with `WithNuruTools(app)`. Calls inside the served process always run there.

## Things to know

- **Keep expensive work out of singleton constructors.** Source-generated DI constructs singletons when services initialize, on every CLI call, forwarded or not. Load warm state on first use (for example with `Lazy<T>`), so a forwarded call does not pay for it in the CLI.
- **One call at a time.** The server runs calls one after another, because route execution uses process-wide state: services, `Environment.ExitCode`, the current directory, and console capture. `serve --status` answers while a call runs.
- **Environment.** A call runs in the caller's current directory, but with the server's environment variables, which are those of the client that started it.
- **Output.** The server captures what a route writes through the default terminal and the console. An app built with `UseTerminal(custom)` writes to that terminal in the server instead. A started server has no TTY, so forwarded output has no color.
- **Turning it off.** `--no-daemon` runs one call locally. Setting `app.RouteForwarder = null` after `Build()` runs every route locally.

## API

| Type | Purpose |
|------|---------|
| `AddServeRoute(socketPath, idleTimeoutSeconds, autoStart)` | Builder call: the `serve` routes and forwarding |
| `NuruDaemonServer.RunAsync / StatusAsync / StopAsync` | What the `serve` routes run; usable from a custom host |
| `NuruDaemonClient` | The forwarder; also `PingAsync`, `StopAsync`, `RunAsync(args)`, `InvokeAsync(route, json)` |
| `NuruDaemonOptions` | Socket path, idle timeout, auto-start, start timeout, serve arguments |
| `NuruDaemonPaths` | Default socket path and the build id |
| `NuruApp.RouteForwarder`, `INuruRouteForwarder` (core) | The forwarding contract generated code calls |
| `NuruApp.ExecuteAsync(args)` (core) | Run CLI arguments from code the generator does not intercept |
| `[LocalOnly]`, `.LocalOnly()` (core) | Keep a route in the CLI process |

## See also

- [MCP hosting](mcp-hosting.md)
- [Agent invocation](agent-invocation.md)
- [Built-in routes](built-in-routes.md)
