# MCP hosting

`TimeWarp.Nuru.McpHost` serves a Nuru app's routes as [Model Context Protocol](https://modelcontextprotocol.io) tools. You write no code per route. Each endpoint in the catalog becomes one tool, and every value the tool sends you comes from the same two generated pieces the CLI uses:

- `tools/list` is built from `NuruApp.Capabilities`, the catalog `--capabilities` prints.
- `tools/call` binds its arguments with `NuruApp.TryBind`, the binder `--json-args` uses. It then runs the route with `InvokeAsync` and returns the text `RenderAsync` writes.

An MCP tool and its CLI route therefore have the same name and take the same arguments. A request that `--json-args` rejects is rejected by `tools/call` with the same message. See [Agent invocation](agent-invocation.md) for the CLI side of the contract.

The direction is Nuru routes to MCP tools. `TimeWarp.Nuru.McpBridge` (task 507) maps the other way, from `[McpServerTool]` methods to Nuru routes, and is a separate package. `TimeWarp.Nuru.Mcp` is the frozen dev-assist server and is not related to either.

## Stdio: the built-in `mcp` route

Reference the package and call `AddMcpRoute()`:

```csharp
#:project path/to/timewarp-nuru-mcphost.csproj   // or: <PackageReference Include="TimeWarp.Nuru.McpHost" />

NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .AddMcpRoute(instructions: "Each tool takes the same arguments as the CLI route of the same name.")
  .Build();

return await app.RunAsync(args);
```

`myapp mcp` then runs an MCP server on stdin and stdout. Register it with any MCP client the same way you register other stdio servers:

```json
{ "mcpServers": { "calculator": { "command": "calculator", "args": ["mcp"] } } }
```

`AddMcpRoute` is interpreted by the source generator, the same way `AddCheckUpdatesRoute()` is. Its arguments must be compile-time constants:

| Argument | Meaning |
|----------|---------|
| `instructions` | Text added to the server instructions after the app description |
| `groupFilter` | Publish only routes in this group (see [Filtering](#filtering)) |
| `contentFormat` | `NuruMcpContentFormat.Text` (default) or `NuruMcpContentFormat.Json` |

The `mcp` route is a built-in. It is matched after your own routes, so a route you map as `mcp`, or a catch-all, takes precedence over it. Root help lists it under Options. It is not an endpoint, so it does not appear in `--capabilities` and is not published as a tool.

Stdout carries the protocol while the server runs. Text a handler writes with `Console.WriteLine`, or through the default terminal, goes into that call's result rather than onto the protocol stream. Anything written to the console between calls goes to stderr.

## HTTP and other hosts: `WithNuruTools`

An app that already runs an MCP host adds the same tools to it. Streamable HTTP in ASP.NET Core uses the `ModelContextProtocol.AspNetCore` package:

```csharp
using TimeWarp.Nuru.McpHost;

NuruApp nuruApp = NuruApp.CreateBuilder().DiscoverEndpoints().Build();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddMcpServer()
  .WithHttpTransport()
  .WithNuruTools(nuruApp, options => options.GroupFilter = "admin");

WebApplication web = builder.Build();
web.MapMcp();
await web.RunAsync();
```

`WithNuruTools` registers one tool per route. If the host has not set the server instructions or server info, it sets them from the catalog. It works with any transport, including `WithStdioServerTransport()`. `NuruMcpServer.RunAsync(app, input, output)` serves a pair of streams without a generic host, and `NuruMcpServer.CreateServerOptions(app)` returns `McpServerOptions` you can pass to `McpServer.Create`.

## Tools

| Tool field | Source |
|------------|--------|
| `name` | The route's literal words, group words first, joined by `_`: `config set {key} {value}` is `config_set`. Characters outside `A-Z a-z 0-9 _ - .` become `_`. A route with no literal words is `default`. If two routes get the same name, the later one in catalog order gets `_2`, then `_3`, and so on. |
| `description` | The route description |
| `inputSchema` | JSON Schema from `parameters[]` and `options[]` (below) |
| `annotations` | From the endpoint kind and `[Destructive]` (below) |

Set `NuruMcpHostOptions.ToolNameSeparator` to use a different separator. `NuruMcpToolCatalog.GetToolName(endpoint)` returns the name for a catalog endpoint, so a client can map tools back to CLI routes.

### Input schema

The schema is an object. Its property names are the catalog `name` strings, which are also the `--json-args` keys. `additionalProperties` is `false`, because the binder rejects unknown keys.

| Catalog | Schema |
|---------|--------|
| `int`, `long`, `short`, `byte` and the unsigned types | `"type": "integer"` |
| `double`, `float`, `decimal` | `"type": "number"` |
| `bool`, or any flag option | `"type": "boolean"` |
| Anything else, including enums, `Guid`, dates, and custom converters | `"type": "string"`. `Guid` gets `format: uuid`, `DateTime` and `DateTimeOffset` get `date-time`, `DateOnly` gets `date`, `TimeOnly` gets `time`, and `Uri` gets `uri`. |
| A type ending in `?` | The type plus `"null"` |
| A catch-all parameter, a repeated option, or a type ending in `[]` | `"type": "array"`, with `items` holding the element schema |
| `allowedValues` | `enum` |
| `defaultValue` | `default`, converted to the JSON type. Quotes and numeric suffixes from the C# initializer are removed. |
| `description` | `description` |
| `required: true` | Listed in `required`, except for flags, which bind `false` when the key is absent |

`NuruMcpToolCatalog.CreateInputSchema(endpoint)` returns this schema for any catalog endpoint.

### Annotations

| Endpoint kind | `readOnlyHint` | `idempotentHint` |
|---------------|----------------|------------------|
| `query` | `true` | `true` |
| `idempotentCommand` | `false` | `true` |
| `command` | `false` | `false` |
| `unspecified` | not set | not set |

`destructiveHint` is never inferred. Mark an endpoint class with `[Destructive]` to set it to `true`:

```csharp
[NuruRoute("cache clear", Description = "Delete every cached entry")]
[Destructive]
public sealed class ClearCacheCommand : ICommand<Unit> { ... }
```

The attribute also adds `"destructive": true` to the endpoint in `--capabilities`, so a CLI agent sees the same hint. Routes without the attribute have no `destructive` member, and their MCP tools leave `destructiveHint` unset. The MCP specification tells clients to treat an unset `destructiveHint` on a tool that is not read-only as possibly destructive. The attribute is supported on endpoint classes. Fluent `Map()` routes cannot carry it.

## Calls

`tools/call` takes the tool's arguments object and binds it with `TryBind`.

- If binding fails (an unknown key, a missing value, a wrong type), the result has `isError: true` and its text is the message `--json-args` prints, for example `Error: Unknown key 'z' for 'add'.`. It is a tool result, not a JSON-RPC error, so the agent can correct the arguments and call again.
- If binding succeeds, the route runs through `InvokeAsync`: configuration, services, the command scope, behaviors, and the handler, the same as on the CLI. The result content is the text the CLI writes for that route. A handler exception, or a problem-details result (the CLI's non-zero exit code), returns `isError: true` with the error text.
- A route that writes nothing, such as one returning `Unit` without printing, returns empty content.
- Calls run one at a time, because route execution uses process-wide state, as the CLI does.

With `contentFormat: NuruMcpContentFormat.Json`, a successful result also carries `structuredContent`. A `json` route that returns an object passes that object through. Any other value is wrapped as `{"result": value}`, with numbers and booleans kept as JSON numbers and booleans. A `unit` route has no structured content. When a response renderer writes text that is not JSON, the result carries only the text.

## Instructions

The server instructions are the app description (`WithDescription`), then the `instructions` text, separated by a blank line. With neither, the server sends no instructions. The server name and version come from the catalog `name` and `version`.

## Filtering

`groupFilter` (or `NuruMcpHostOptions.GroupFilter`) publishes only the routes in one group. It uses the rule `--capabilities --group-filter` uses: the filter is split on `.`, and each segment is compared with the route's `groupPath` ignoring case. With `groupFilter: "admin"` the server lists only the `admin ...` routes, and calls to other routes fail as unknown tools.

## Related documentation

- **[Agent invocation](agent-invocation.md)** - `--capabilities`, `--json-args`, and the in-process catalog and binder this package is built on
- **[Built-in Routes](built-in-routes.md)** - `mcp` next to the other built-ins
