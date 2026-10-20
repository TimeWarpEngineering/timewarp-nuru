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
| `socketPath` | null | Unix domain socket path. Null uses `NURU_DAEMON_SOCKET`, then the per-user default. A relative path is a file under the per-user socket directory, not the current directory |
| `idleTimeoutSeconds` | 600 | Seconds without a call before the server stops; 0 never stops |
| `autoStart` | false | When true, a call with no server starts one and forwards, as `--daemon` does |
| `exclusiveCalls` | false | When true, the server runs one call at a time (see [Concurrent calls](#concurrent-calls)) |

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
- **Location**: `$XDG_RUNTIME_DIR/nuru/<app>.sock` on Linux when that directory exists. Otherwise `<temp>/nuru-<user>/<app>.sock`. On Windows it is `%LOCALAPPDATA%\TimeWarp.Nuru\daemon\<app>.sock`. `<app>` is the catalog name, and a long name is replaced by a hash to fit the socket path limit. `socketPath` or `NURU_DAEMON_SOCKET` overrides the location, for example to keep test servers apart. A relative override is resolved against the per-user socket directory, so every call and the server it starts use the same socket whatever their current directory. A server started by `--daemon` or `autoStart` listens on exactly the path its client resolved.
- **Access**: on Unix the socket's directory must be owner-only. A missing directory is created with mode 0700. An existing directory that is a symlink, or that gives group or others any access, is refused by `serve` and by the client (a call runs locally; with `--daemon` it fails with the reason). The default directory is set back to 0700 when you own it. The socket file is set to mode 0600 after it is bound. So pick an owner-only directory for `socketPath`, not `/tmp` itself.
- **One server per socket**: a server holds an exclusive lock on `<socket>.lock` from before it checks the socket until after it removes it. Only the lock holder deletes or binds the socket path, so a second `serve` never removes a running server's socket: it reports the running server, or fails when the lock holder does not answer within the start timeout. A socket file left by a server that crashed (the OS releases its lock) is deleted. The lock file stays.
- **Build check**: the server reports its build: name, version, entry file and that file's write time, fixed when the server starts. A client from a different build stops that server and does not use it, so a rebuilt CLI never runs old server code. If that server still answers after the stop, the call runs locally, or fails with `--daemon` or `autoStart`. A started server is used only once it reports the client's build, and each forwarded call carries the build id, so the server refuses a call from another build.
- **Start**: the client starts its own executable (or `dotnet <entry.dll>`) with `serve`, with the standard streams redirected. On Linux it runs under `setsid`, so a Ctrl+C in the caller's terminal does not reach the server. A started server writes nothing to the inherited streams.
- **Lifetime**: the server stops after the idle timeout, which counts from the end of the last call and never expires while a call runs. It also stops on `serve --stop`, Ctrl+C, or SIGTERM, and then removes the socket file.
- **Cancellation**: when a client goes away before the answer (Ctrl+C), the server cancels that call's `CancellationToken`. Other calls keep running.
- **Backlog**: the socket listens with the system's maximum backlog, so many clients can connect at once.

## Concurrent calls

The server runs calls concurrently. Several CLI calls and MCP sessions can share one warm server, and a slow call (a first solution load, analyzers over a whole solution) does not hold up the others. In one run of `tests/scripts/benchmark-daemon-concurrency.cs`, eight forwarded calls to a route that waits 50 ms took 416 ms together when calls ran one at a time, and 52 ms concurrently. A no-op call takes about 0.35 ms in both cases.

Each call runs in its own `NuruCallContext` (core). Nothing on the call path changes process state:

| Per call | What it is | What a handler uses |
|----------|------------|---------------------|
| Output | A terminal of its own. `NuruApp.Terminal` and an injected `ITerminal` write to it, and so do `Console.Out` and `Console.Error` during the call. | `ITerminal` (or the console) as usual |
| Exit code | The call's own value, returned to its client | Return problem details, or set `NuruCallContext.CurrentExitCode` |
| Working directory | The client's directory. The server process's directory does not change. | `NuruCallContext.CurrentDirectory`, `NuruCallContext.GetFullPath(path)` |
| Services | Its own scope: `Scoped`, `AddCommandScoped` and `AddSessionScoped` services are created for the call and disposed when it returns. Singletons are shared by all calls. | Constructor or parameter injection, as usual |
| Cancellation | Its own token, cancelled when its client goes away | The handler's `CancellationToken` |

`NuruCallContext.CurrentExitCode`, `CurrentDirectory` and `GetFullPath` work outside a server too: in a plain CLI run they read and write `Environment.ExitCode` and `Environment.CurrentDirectory`. `NuruCallContext.Current` is the running call's context, or null in a plain CLI run.

### What a handler must not do in a served app

- **Set `Environment.ExitCode`.** It is process-wide, so the server ignores it for the call (another call could be changing it). Set `NuruCallContext.CurrentExitCode`, or return `OneOf<T, SharedProblemDetails>`. Both work in the CLI too, and `RunAsync` still returns the exit code and leaves it in `Environment.ExitCode` for a plain CLI run.
- **Read `Environment.CurrentDirectory` or pass relative paths to file APIs.** They resolve against the server's directory. Use `NuruCallContext.GetFullPath(path)`.
- **Keep per-call state in a singleton without locking.** Singletons are shared by concurrent calls. Make them thread-safe, use a scoped service, or mark the route exclusive.

### Exclusive routes

A handler that is not thread-safe can ask to run alone. While one exclusive call runs, other exclusive calls on the same app wait; other calls still run concurrently.

```csharp
[Exclusive]
[NuruRoute("reload", Description = "Reload the workspace")]
public sealed class ReloadCommand : ICommand<Unit> { /* ... */ }

builder.Map("compact").WithHandler(Compact).AsCommand().Exclusive().Done();
```

To run every call one at a time, pass `AddServeRoute(exclusiveCalls: true)`, or set `NuruDaemonOptions.ExclusiveCalls` on a custom host (`NuruDaemonServer.RunAsync(app, options)`). `ping` (`serve --status`) and `stop` never wait for calls.

## Working with the MCP host

`TimeWarp.Nuru.McpHost` uses the same forwarder. In an app with both `AddServeRoute()` and `AddMcpRoute()`, `myapp mcp` sends each `tools/call` to the running server, so the MCP host and the CLI share one warm server. The server binds the JSON arguments with `TryBind`, runs `InvokeAsync` and `RenderAsync`, and returns the same texts. A bind error is still a tool result with `isError` and the `--json-args` message. Local-only routes run in the `mcp` process. See [MCP hosting](mcp-hosting.md).

A daemon can also host MCP itself with `WithNuruTools(app)`. Calls inside the served process always run there.

## Things to know

- **Keep expensive work out of singleton constructors.** Source-generated DI constructs singletons when services initialize, on every CLI call, forwarded or not. Load warm state on first use (for example with `Lazy<T>`), so a forwarded call does not pay for it in the CLI.
- **Calls run concurrently.** See [Concurrent calls](#concurrent-calls) for what a handler gets per call and the exclusive opt-in.
- **Environment.** A call carries the caller's current directory (`NuruCallContext.CurrentDirectory`), but runs with the server's environment variables, which are those of the client that started it.
- **Output.** The server gives each call its own plain-text terminal and returns what the route wrote through any `ITerminal` or the console. A terminal set with `UseTerminal(custom)` is not used for forwarded calls. Forwarded output has no color.
- **Turning it off.** `--no-daemon` runs one call locally. Setting `app.RouteForwarder = null` after `Build()` runs every route locally.

## API

| Type | Purpose |
|------|---------|
| `AddServeRoute(socketPath, idleTimeoutSeconds, autoStart, exclusiveCalls)` | Builder call: the `serve` routes and forwarding |
| `NuruDaemonServer.RunAsync / StatusAsync / StopAsync` | What the `serve` routes run; usable from a custom host |
| `NuruDaemonClient` | The forwarder; also `PingAsync`, `StopAsync`, `RunAsync(args)`, `InvokeAsync(route, json)` |
| `NuruDaemonOptions` | Socket path, idle timeout, auto-start, start timeout, serve arguments, `ExclusiveCalls` |
| `NuruDaemonPaths` | Default socket path and the build id |
| `NuruApp.RouteForwarder`, `INuruRouteForwarder` (core) | The forwarding contract generated code calls |
| `NuruApp.ExecuteAsync(args)` (core) | Run CLI arguments from code the generator does not intercept |
| `NuruApp.ExecuteAsync(args, context)`, `InvokeAsync(request, context)` (core) | Run a call in its own `NuruCallContext`, concurrently with others |
| `NuruCallContext` (core) | The running call's terminal, working directory and exit code; `CurrentDirectory`, `GetFullPath`, `CurrentExitCode` |
| `[LocalOnly]`, `.LocalOnly()` (core) | Keep a route in the CLI process |
| `[Exclusive]`, `.Exclusive()` (core) | Run a route's calls one at a time against other exclusive calls |

## See also

- [MCP hosting](mcp-hosting.md)
- [Agent invocation](agent-invocation.md)
- [Built-in routes](built-in-routes.md)
