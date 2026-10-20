# Agent invocation

An agent discovers a Nuru CLI with `--capabilities`, then invokes a route with `--json-args`. Route literals stay on argv. Every parameter and option value may come from one JSON object. Keys are the `name` strings on `parameters[]` and `options[]`. There is no second schema.

```bash
myapp --capabilities
myapp apply_patch --json-args - <<'EOF'
{
  "solution": "/abs/path/to.sln",
  "patch": "--- a/Foo.cs\n+++ b/Foo.cs\n@@\n-old\n+new\n",
  "check-only": true
}
EOF
```

The same object can come from a file:

```bash
myapp apply_patch --json-args @./apply-patch.json
```

## Capabilities

`--capabilities` prints a `CapabilitiesResponse`. Routes are one flat `endpoints` array. Each entry has `pattern`, `groupPath`, `aliases`, `kind`, `parameters[]`, and `options[]`. A grouped route carries its group prefix segments in `groupPath`, and an ungrouped route has `groupPath: []`. There is no nested `groups` array. The optional `invocation` object names the transport. Every generated app emits it. An endpoint with no parameters accepts `--json-args '{}'` and rejects unknown keys.

```json
"invocation": {
  "jsonArgs": "--json-args",
  "stdin": "-",
  "filePrefix": "@",
  "merge": "argvOverridesJson",
  "unknownKeys": "error"
}
```

`invocation` is nullable. A document that omits it still deserializes, and `Invocation` is null. A reader that does not declare the property ignores the JSON field. `TimeWarp.Nuru.Search` indexes `endpoints` and leaves `invocation` in the stored capabilities document. It does not copy the object onto search results.

`parameters[]` and `options[]` remain the schema. `--capabilities` does not emit a JSON Schema.

Each endpoint ends with `returnType`: the CLR type name of the result (the awaited type for an async handler, without `global::`) and how the CLI writes it.

```json
"returnType": {
  "name": "double",
  "kind": "primitive"
}
```

| `kind` | Result types | CLI output |
|--------|--------------|------------|
| `unit` | `void`, `Task`, `TimeWarp.Mediator.Unit` (name `void` for the first two) | nothing |
| `primitive` | `string`, numeric types, `bool`, `char`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, `TimeSpan` | text |
| `json` | anything else, including nullable value types, enums, arrays, and records | JSON |

An endpoint that returns `OneOf<TSuccess, SharedProblemDetails>` is described by `TSuccess`, because the CLI writes the success value to stdout and the problem to stderr with an exit code. `IQuery<OneOf<string, SharedProblemDetails>>` is `{"name": "string", "kind": "primitive"}`. Any other `OneOf` is `json` under its own name.

`returnType` is nullable for the same reason as `invocation`: a document from a CLI built before it existed still deserializes, with `ReturnType` null.

An endpoint class marked `[Destructive]` also has `"destructive": true` after `returnType`. Nothing infers it. Other endpoints have no `destructive` member, so their documents are unchanged.

A route marked `[LocalOnly]` (endpoint class) or `.LocalOnly()` (fluent route) has `"localOnly": true` last. A served app never forwards it to its server (see [Served apps](served-apps.md)). Other routes have no `localOnly` member.

## MCP

`TimeWarp.Nuru.McpHost` serves the same routes as MCP tools: `AddMcpRoute()` adds a stdio `mcp` route, and `WithNuruTools(app)` adds the tools to an existing MCP host. A tool's name is the route's literal words joined by `_`. Its input schema is built from this catalog, and `tools/call` uses the binder described below. A tool therefore takes exactly the arguments `--json-args` takes for that route and returns the text the CLI prints. See [MCP hosting](mcp-hosting.md).

## In-process catalog

A host that runs in the same process as the app reads the catalog without spawning the CLI. `NuruApp.Capabilities` returns the `CapabilitiesResponse` that `--capabilities` prints, and `CapabilitiesResponse.FilterByGroup(group)` returns the document `--capabilities --group-filter {group}` prints. The source generator builds the catalog at compile time, so neither call uses reflection, and both work under AOT.

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .WithName("myapp")
  .DiscoverEndpoints()
  .Build();

CapabilitiesResponse catalog = app.Capabilities;            // same as: myapp --capabilities
CapabilitiesResponse admin = catalog.FilterByGroup("admin"); // same as: myapp --capabilities --group-filter admin

foreach (EndpointCapability endpoint in catalog.Endpoints)
{
  Console.WriteLine($"{endpoint.Pattern} -> {endpoint.ReturnType?.Kind}");
}
```

- The catalog is attached by the generated interceptor for the app's `Build()` call, so it is available before `RunAsync()`. An app whose `Build()` the generator did not see gets it at the start of `RunAsync()`. A `NuruApp` with no generated catalog throws `InvalidOperationException`.
- `Capabilities` is built on first access and cached. `--capabilities` serializes that same instance, so the two cannot drift. Treat it as read-only.
- `FilterByGroup` splits the filter on `.` and compares each segment ignoring case. It returns a new response with `filter.group` set.
- Each app built in one assembly has its own catalog with only its own routes.

## In-process bind and invoke

A host that gets a JSON object from somewhere other than argv (an MCP `tools/call`, a batch file, a daemon request) binds it to a route and runs that route in the same process. Binding uses the same generated code as `--json-args`, so the rules below apply unchanged.

```csharp
using JsonDocument doc = JsonDocument.Parse("""{"env":"prod","count":3}""");

if (!app.TryBind("deploy {env} --count {count:int}", doc.RootElement, out NuruBoundRequest? request, out NuruBindError? error))
{
  // error.Kind, error.Key, and error.Message (the text --json-args prints)
  return Fail(error.Message);
}

object? result = await app.InvokeAsync(request, cancellationToken); // typed result, nothing written

using TestTerminal text = new();
int exitCode = await app.RenderAsync(request, result, text);        // the CLI's text, when a host wants it
```

- `TryBind(route, arguments, out request, out error)` names the route by its catalog `pattern` (ordinal). `arguments` is a JSON object; `default(JsonElement)` binds no arguments. No argv is parsed: the binder sees only the route's literal words, so every parameter and option comes from JSON. Keys, types, `null`, arrays, defaults, and the first failure reported are the same as `--json-args` without argv.
- `NuruBindError.Kind` is `UnknownRoute`, `NotAnObject`, `UnknownKey`, `MissingValue`, `WrongType`, `InvalidValue`, or `UnexpectedArgument`. `Key` is the JSON key the failure is about, or null. `Message` is the line `--json-args` writes to stderr.
- `InvokeAsync(request, cancellationToken)` runs the same configuration, services, command scope, `INuruBehavior` chain, and handler (or mediator dispatch) as the CLI. It returns the handler's result boxed, without writing it: for `OneOf<T, SharedProblemDetails>` the `OneOf` itself, and null for `void` or `Unit`. Handler exceptions propagate. It throws `InvalidOperationException` when route execution stops before the handler (for example, no handler is registered), and `ArgumentException` for a request another app bound.
- `RenderAsync(request, result, terminal)` writes the result as the CLI does for that route: a registered or declared `IResponseRenderer<T>`, problem details to stderr, otherwise the output strategy (`returnType.kind`). It returns the exit code the CLI would: the problem status, otherwise 0.
- The binder and invoker are attached with the catalog, by the generated `Build()` interceptor, so an app that is built but never run supports them. The code is generated: no reflection, AOT-safe.
- Calls may run concurrently. Each `InvokeAsync` and `RenderAsync` call runs in its own `NuruCallContext`: its own exit code and service scope, never `Environment.ExitCode`. `InvokeAsync(request, context, cancellationToken)` also gives the call its own terminal and working directory (`new NuruCallContext(terminal, workingDirectory) { CaptureConsole = true }`), which is how the daemon and MCP hosts run each call. `[Exclusive]` / `.Exclusive()` routes run one at a time. See [Served apps](served-apps.md#concurrent-calls).

## `--json-args`

`--json-args` takes one value. There is no short alias. The value is a separate argv token.

| Value | Meaning |
|-------|---------|
| `-` | Read stdin to EOF as UTF-8 JSON. A pipe and a heredoc both use this. |
| `@path` | Read that filesystem path as UTF-8 JSON. `~/` may be expanded. |
| `{...}` | Inline JSON object. Use this for tests and tiny calls. It still counts against `ARG_MAX`. |

Any other value exits 1. Attached forms (`--json-args=`, `--json-args:`, `/json-args=`, `/json-args:`) exit 1. The `@` prefix is special only as the value of `--json-args`.

Inside the REPL, `-` exits 1. `@path` and a quoted inline object work there.

## Exact names

Copy each key from the catalog `name`. Comparison is ordinal. An `alias` does not match a JSON key. Authors who want `check-only` or `checkOnly` declare that long form. Agents copy `name`.

## Merge

Argv overrides JSON for the same `name`. A key only in JSON is used. A key only in argv is used. When both set a repeated option, the argv list replaces the JSON array. Absent keys leave the argv value or the default. JSON `null` is a type error for a non-nullable value and clears a nullable reference.

`bool` values are JSON booleans. Numbers are JSON numbers. Enums and other strings are JSON strings. A repeated option is a JSON array. A catch-all is an array of strings.

## App-level fat field

An app that does not emit `invocation` can still take one large field in the handler. The handler interprets the option value. Document the sentinel in that option's description so `--capabilities` shows it. Only one such option can use `-` per process. Each app defines its own errors and merge rules.

```csharp
[Option("patch", Description = "Unified diff; '-' reads stdin, '@path' reads a file")]
public string Patch { get; set; } = "";
```

The handler reads stdin when `Patch` is `-`, the path after `@`, or the literal. Callers pipe or use a heredoc. This is an application convention. The Nuru contract is `--json-args`.

## Related documentation

- **[MCP hosting](mcp-hosting.md)** - the same routes as MCP tools, with the same names and arguments
- **[Built-in Routes](built-in-routes.md)** - `--capabilities` and `--json-args` beside the other built-ins
- **[Auto-Help](auto-help.md)** - root help lists `--json-args`; per-route help says values may come from it
