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

Stdout carries the protocol while the server runs. Text a handler writes through `ITerminal`, or with `Console.WriteLine`, goes into that call's result rather than onto the protocol stream, even while other calls run. Anything written to the console outside a call goes to stderr.

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
| `name` | The route's literal words, including any route-group prefix, joined by `_`: `config set {key} {value}` is `config_set`. An `[Operation(Group)]` path is metadata and adds no words. Characters outside `A-Z a-z 0-9 _ - .` become `_`. A route with no literal words is `default`. If two routes get the same name, the later one in catalog order gets `_2`, then `_3`, and so on. |
| `description` | The route description |
| `inputSchema` | JSON Schema from `parameters[]` and `options[]` (below) |
| `title` | `[Operation(Title)]` on a contract, `[DisplayName]` on a `[NuruRoute]` class that is not an operation, or a host override (below) |
| `annotations` | From the endpoint kind, `[Destructive]`, `[OpenWorld]`, and the title source above, with host overrides (below) |

Set `NuruMcpHostOptions.ToolNameSeparator` to use a different separator. `NuruMcpToolCatalog.GetToolName(endpoint)` returns the name for a catalog endpoint, so a client can map tools back to CLI routes.

### Input schema

The schema is an object. Its property names are the catalog `name` strings, which are also the `--json-args` keys. `additionalProperties` is `false`, because the binder rejects unknown keys.

| Catalog | Schema |
|---------|--------|
| `int`, `long`, `short`, `byte` and the unsigned types | `"type": "integer"` |
| `double`, `float`, `decimal` | `"type": "number"` |
| `bool`, or any flag option | `"type": "boolean"` |
| `json` plus a CLR type name (a member that binds as one JSON value) | The member's catalog `schema` (below) |
| Anything else, including enums, `Guid`, dates, and custom converters | `"type": "string"`. `Guid` gets `format: uuid`, `DateTime` and `DateTimeOffset` get `date-time`, `DateOnly` gets `date`, `TimeOnly` gets `time`, and `Uri` gets `uri`. |
| A type ending in `?` | The type plus `"null"` |
| A catch-all parameter, a repeated option, or a type ending in `[]` | `"type": "array"`, with `items` holding the element schema |
| `allowedValues` | `enum` |
| `defaultValue` | `default`, converted to the JSON type, and `Default: X.` at the end of `description` (see [Defaults](#defaults)). Quotes and numeric suffixes from the C# initializer are removed. A flag's default is `false`. |
| `description` | `description` |
| `required: true` | Listed in `required`, except for flags, which bind `false` when the key is absent |

`NuruMcpToolCatalog.CreateInputSchema(endpoint)` returns this schema for any catalog endpoint.

### Object and array members

A member that is not a scalar or a repeated scalar, such as an array of objects or a nested object, binds as one JSON value: `tools/call` passes the object or array itself, and the binder deserializes it into the member's type. Its property schema is the catalog `schema`, exported from the source-generated JSON metadata that same binder deserializes with, so it is derived from the member's type rather than written by hand:

- `type` is `object` or `array`, with `properties` and `items` nested as deep as the type goes.
- `required` lists C# `required` members and constructor parameters.
- A nullable reference member inside the type has `"null"` in its `type`. The member itself has `"null"` only when it is nullable (`TotalFormat? Format`); otherwise the binder rejects `null`.
- An enum with `[JsonConverter(typeof(JsonStringEnumConverter<TEnum>))]` is an `enum` of its names. Without the converter the deserializer takes numbers, and the schema says `integer`.
- A nested property's `[Description]` becomes its `description`.
- An initializer that is itself JSON, such as `= []`, is the `default`.

```json
"Notes": {
  "type": "array",
  "items": {
    "type": "object",
    "properties": { "Label": { "type": "string" }, "Amount": { "type": "number" } },
    "required": ["Label"]
  },
  "default": [],
  "description": "Labeled amounts added to the total. Default: []."
}
```

Property names inside the value are the declared C# names. The binder matches them ignoring case. A JSON value sent as a string (`"Notes": "[{...}]"`) is a bind error, because the binder takes only an object or an array. A catalog built before task 508-015 has no `schema`. Such a member is published as `"type": ["object", "array"]`.

The contracts calculator sample (`samples/endpoints/02-calculator-contracts`) has an object-array member (`Notes`) and a nested object member (`Format`). `calculator-contracts mcp` publishes both schemas, and the call below binds and runs:

```json
{"name": "calc_total", "arguments": {"Values": [1, 2], "Notes": [{"Label": "tax", "Amount": 1.5}], "Format": {"Decimals": 1, "Prefix": "$"}}}
```

### Defaults

A member with a default carries it twice: in the JSON Schema `default` keyword, and as a last sentence of its `description`, `Default: X.`, where X is the default as JSON (`Default: 3.`, `Default: "main".`). The description is the only place every harness shows the model:

| Harness | `default` reaches the model | Evidence (checked 2026-10-20) |
|---------|-----------------------------|-------------------------------|
| Claude Code 2.1.304 | Yes | The MCP `inputSchema` is sent as the tool's `input_schema` unchanged |
| OpenAI Codex CLI (`openai/codex` `main` at `b2aa07db`) | No | `codex-rs/tools/src/mcp_tool.rs` parses the schema into `JsonSchema` (`codex-rs/tools/src/json_schema/types.rs`), which keeps `type`, `description`, `enum`, `items`, `properties`, `required`, `additionalProperties` and the combinators, and drops `default` and `format` |
| Grok Build 1.0.64 | Yes | `search_tool` returns each MCP tool's `input_schema` as written |

Repeating the default in the description adds a few words to `tools/list` and needs no per-app option. `NuruMcpToolCatalog.DescribeDefault(description, defaultJson)` builds the text.

### Nullable members

A member whose catalog type ends in `?` (`int?`, a nullable enum) has `"null"` in its `type`, because the binder accepts an explicit `null` for it and binds `null`. Omitting the key also leaves the member at its default, and that is the usual way to skip a member. A non-nullable member has no `"null"`, and the binder rejects one. A derived `string?` option is published as `string` because the catalog type carries no `?`. The binder still accepts `null` for it.

### Annotations

Harnesses use these hints to decide which calls need a permission prompt and which they may approve automatically.

| Endpoint kind | `readOnlyHint` | `idempotentHint` |
|---------------|----------------|------------------|
| `query` | `true` | `true` |
| `idempotentCommand` | `false` | `true` |
| `command` | `false` | `false` |
| `unspecified` | not set | not set |

The other hints are never inferred. The endpoint class (an `[Operation]` contract or a `[NuruRoute]` class) declares them:

| Attribute on the endpoint class | Annotation | Catalog member |
|---------------------------------|------------|----------------|
| `[Destructive]` / `[Destructive(false)]` (`TimeWarp.Contracts`) | `destructiveHint: true` / `false` | `destructive` |
| `[OpenWorld]` / `[OpenWorld(false)]` (`TimeWarp.Contracts`) | `openWorldHint: true` / `false` | `openWorld` |
| `[Operation(Title = "...")]` on a contract; `[DisplayName("...")]` on a `[NuruRoute]` class with no operation | `title`, and the tool's `title` | `title` |

```csharp
using TimeWarp.Contracts;

[Operation("find_references", Description = "Find every reference to a symbol", Title = "Find references")]
[OpenWorld(false)]
public sealed class FindReferencesQuery : IQuery<string> { ... }

[NuruRoute("cache clear", Description = "Delete every cached entry")]
[System.ComponentModel.DisplayName("Clear the cache")]
[Destructive]
public sealed class ClearCacheCommand : ICommand<Unit> { ... }
```

`--capabilities` carries the same members, so a CLI agent sees the same hints. A route without an attribute has no catalog member, and its tool leaves the hint unset. The MCP specification tells clients to treat an unset `destructiveHint` on a tool that is not read-only as possibly destructive, and an unset `openWorldHint` as open-world. `[Destructive(false)]` says a command only adds data. `[OpenWorld(false)]` says a route works only on local state. The attributes are supported on endpoint classes, contract and route alike. A blank or whitespace operation `Title` publishes no title, and `[DisplayName]` does not set an operation's title. Fluent `Map()` routes cannot carry them. See [Neutral contracts](neutral-contracts.md#tool-hints) for how a contract carries them.

An app that serves contracts it does not own sets hints per tool with `NuruMcpHostOptions.ToolAnnotations`, keyed by tool name. Every hint an entry sets replaces the derived one, and the others stay derived:

```csharp
builder.Services.AddMcpServer()
  .WithStdioServerTransport()
  .WithNuruTools(app, options =>
  {
    options.ToolAnnotations["find_references"] = new ToolAnnotations { Title = "Find references", OpenWorldHint = false };
    options.ToolAnnotations["cache_clear"] = new ToolAnnotations { DestructiveHint = true };
  });
```

`NuruMcpToolCatalog.CreateAnnotations(endpoint)` returns the derived annotations, and `NuruMcpToolCatalog.MergeAnnotations(derived, overrides)` applies an override.

## Calls

`tools/call` takes the tool's arguments object and binds it with `TryBind`.

- If binding fails (an unknown key, a missing value, a wrong type), the result has `isError: true` and its text is the message `--json-args` prints, for example `Error: Unknown key 'z' for 'add'.`. It is a tool result, not a JSON-RPC error, so the agent can correct the arguments and call again.
- If binding succeeds, the route runs through `InvokeAsync`: configuration, services, the command scope, behaviors, and the handler, the same as on the CLI. The result content is the text the CLI writes for that route. A handler exception, a problem-details result, or a non-zero `NuruCallContext.CurrentExitCode` set by the handler (the CLI's non-zero exit code) returns `isError: true` with the error text. Rendering continues the call, so a response renderer that resolves a path with `NuruCallContext.GetFullPath` sees the call's directory.
- A route that writes nothing, such as one returning `Unit` without printing, returns empty content.
- If the app has an `IResponseRenderer<SharedProblemDetails>`, a bind failure and a handler exception that escapes are rendered with it instead, so an agent sees the same format as for the app's problem-details results. See [Errors through the problem renderer](#errors-through-the-problem-renderer).
- Calls run concurrently. Each `tools/call` runs in its own `NuruCallContext`, with its own terminal, exit code and service scope, so a slow tool does not hold up the others and two calls never see each other's output. Text a handler writes through `ITerminal`, or to the console (with `CaptureConsoleOutput`, the default), goes into that call's result. A handler that is not thread-safe can be marked `[Exclusive]` (or `.Exclusive()`): exclusive calls run one at a time. `NuruMcpHostOptions.ExclusiveCalls = true` runs every in-process call one at a time. See [Served apps](served-apps.md#concurrent-calls) for what a handler gets per call.
- `notifications/cancelled` cancels that request's `CancellationToken` only; other calls keep running. A forwarded call is cancelled in the server too: the host closes that call's connection.
- A handler must not set `Environment.ExitCode` or read `Environment.CurrentDirectory` for its own call. Return problem details or set `NuruCallContext.CurrentExitCode`, and resolve paths with `NuruCallContext.GetFullPath`.
- In a served app (`AddServeRoute()`, package `TimeWarp.Nuru.Daemon`), a call to a route that is not local-only goes to the app's running server, so the MCP host and the CLI share one warm server. The server binds the same arguments with `TryBind` and returns the same texts and errors. With `NuruMcpContentFormat.Json`, structured content then comes from the rendered text: numbers and booleans are parsed back using the catalog return type. See [Served apps](served-apps.md).

### Errors through the problem renderer

An app with an `IResponseRenderer<SharedProblemDetails>` (see [Response rendering](neutral-contracts.md#dispatch-and-rendering)) renders its handlers' problem details with it. The MCP host uses the same renderer for the two failures that happen outside a handler's result. Each becomes a `SharedProblemDetails` from `NuruProblems`:

| Failure | `Type` | `Status` | `Detail` | Extension members |
|---------|--------|----------|----------|-------------------|
| Bind failure (`NuruProblems.FromBindError`) | `Invalid` | 400 | The bind message without its `Error: ` prefix | `kind` (the `NuruBindErrorKind`), `key` (the argument name, when there is one) |
| Handler exception (`NuruProblems.FromException`) | `Faulted` | 500 | The exception message | none |

The result is still `isError: true`, and its text is the renderer's output. For example, a renderer that writes `error={Type}` and `errorMessage={Detail}` returns `error=Invalid` and `errorMessage=Unknown key 'z' for 'add'.` for an unknown key. A forwarded call gets the same text: the served app's server renders its bind failures and escaped exceptions the same way. An app with no problem renderer keeps the plain `Error: ...` text. If the renderer throws, the host returns the plain text.

`NuruApp.TryRenderProblem(problem, out text)` renders any problem with the app's problem renderer. It returns false when there is none. The CLI uses the renderer for its own binding errors only after `UseProblemRendererForErrors()` (see [Neutral contracts](neutral-contracts.md#dispatch-and-rendering)).

With `contentFormat: NuruMcpContentFormat.Json`, a successful result also carries `structuredContent`. A `json` route that returns an object passes that object through. Any other value is wrapped as `{"result": value}`, with numbers and booleans kept as JSON numbers and booleans. A `unit` route has no structured content. Structured content comes from the route's value only: console output the handler writes stays in the text content. When a response renderer writes text that is not JSON, the result carries only the text.

## Instructions

The server instructions are the app description (`WithDescription`), then the `instructions` text, separated by a blank line. With neither, the server sends no instructions. The server name and version come from the catalog `name` and `version`.

## Filtering

`groupFilter` (or `NuruMcpHostOptions.GroupFilter`) publishes only the routes in one group. It uses the rule `--capabilities --group-filter` uses: the filter is split on `.` or space, and each segment is compared with the route's `groupPath` ignoring case. With `groupFilter: "admin"` the server lists only the `admin ...` routes, and calls to other routes fail as unknown tools.

## Related documentation

- **[Agent invocation](agent-invocation.md)** - `--capabilities`, `--json-args`, and the in-process catalog and binder this package is built on
- **[Built-in Routes](built-in-routes.md)** - `mcp` next to the other built-ins
- **[Served apps](served-apps.md)** - forward tool calls to the app's warm local server
