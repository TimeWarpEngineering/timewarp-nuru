# TimeWarp.Nuru.McpHost

Serve a [TimeWarp.Nuru](https://github.com/TimeWarpEngineering/timewarp-nuru) app's routes as MCP tools without writing any per-route code. Each route becomes one tool. The tool's input schema is built from the same catalog `--capabilities` prints, and `tools/call` runs through the generated binder that `--json-args` uses. MCP and the CLI therefore share tool names and arguments.

```csharp
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddMcpRoute(instructions: "Use add and multiply for arithmetic.")
  .Build();

return await app.RunAsync(args);
```

```bash
myapp mcp   # MCP server over stdio
```

To serve the same tools from an ASP.NET Core app over streamable HTTP, use the ModelContextProtocol.AspNetCore package:

```csharp
builder.Services.AddMcpServer()
  .WithHttpTransport()
  .WithNuruTools(nuruApp);
app.MapMcp();
```

See [MCP hosting](https://github.com/TimeWarpEngineering/timewarp-nuru/blob/master/documentation/user/features/mcp-hosting.md).

Not to be confused with `TimeWarp.Nuru.Mcp`, the frozen dev-assist server, or `TimeWarp.Nuru.McpBridge` (task 507), which goes the other way and maps MCP tool methods to Nuru routes.
