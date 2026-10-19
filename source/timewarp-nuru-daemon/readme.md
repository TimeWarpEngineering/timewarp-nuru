# TimeWarp.Nuru.Daemon

Run a [TimeWarp.Nuru](https://github.com/TimeWarpEngineering/timewarp-nuru) app as a long-lived local server and have its own CLI forward routes to it. State that is slow to load, such as a Roslyn workspace, is loaded once in the server. Later CLI calls and MCP `tools/call` requests use it while it is warm. You write no client, transport, or forwarding handler per route.

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddServeRoute(idleTimeoutSeconds: 600)
  .Build();

return await app.RunAsync(args);
```

```bash
myapp --daemon count hello   # start the server in the background and forward
myapp count hello            # forwarded while the server runs; local when it does not
myapp --no-daemon count x    # always local
myapp serve --status         # report the server
myapp serve --stop           # stop it
```

The server listens on a per-user Unix domain socket, never a TCP port. The forwarded call runs the same generated matching, binding, behaviors and handler, and the CLI prints the same output and returns the same exit code. Use `[LocalOnly]` or `.LocalOnly()` for routes that must run in the CLI process.

See [Served apps](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/features/served-apps.md).
