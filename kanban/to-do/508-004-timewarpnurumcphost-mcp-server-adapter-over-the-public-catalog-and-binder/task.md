# TimeWarp.Nuru.McpHost: MCP server adapter over the public catalog and binder

## Description

An opt-in package that gives any Nuru app an MCP server with no per-app code: one MCP tool per route, built from the public capabilities catalog (508-002) and dispatched through the public binder and invoke (508-003). Adds a built-in `mcp` route that serves over stdio, and a hosting extension for apps that already run an HTTP host (streamable HTTP transport).

Direction: Nuru routes are the contract, MCP tools are derived. Task 507 (`TimeWarp.Nuru.McpBridge`) is the reverse mapping and stays a separate package; the two can coexist, and 507's go/no-go gate records which direction Roslynk takes.

## Depends on

- 508-002
- 508-003

## Requirements

- Tool name = route literal (group path joined by a documented separator, or the route's declared name); description = route description; input schema = JSON Schema built from `parameters[]` and `options[]` (name, type, required, default, enum allowed values, arrays for repeated and catch-all), so the schema the agent sees is the same contract `--json-args` accepts.
- Tool annotations from `EndpointKind`: Query is readOnly and idempotent; IdempotentCommand is idempotent; Command is neither. `destructiveHint` left to an optional per-route attribute, not inferred.
- Server instructions from the app description plus an optional app-provided text.
- `tools/call` binds the arguments with the public binder (unknown keys are an error reported as an MCP tool error, not a protocol error), invokes, and returns the typed result rendered as the CLI would render it (text), with JSON content as an option.
- Filtering: expose the same group filter the catalog has, so an app can publish a subset of routes as tools.
- Package name: `TimeWarp.Nuru.Mcp` is taken by the frozen dev-assist server. Working name `TimeWarp.Nuru.McpHost`; confirm against 507's `McpBridge` so the pair reads as two directions.
- Tests: schema parity between `--capabilities` and `tools/list` for the calculator sample; a `tools/call` round trip; annotation mapping; filter.
- Docs: a page beside `agent-invocation.md`; note in the Roslynk-facing guidance that MCP and CLI share names and arguments exactly.

## Goal (implementer restatement)

Ship `TimeWarp.Nuru.McpHost` (`source/timewarp-nuru-mcphost/`), an opt-in package that serves any Nuru app's routes as MCP tools with no per-route code. Everything comes from the public catalog (`NuruApp.Capabilities`, 508-002) and the public binder (`TryBind` / `InvokeAsync` / `RenderAsync`, 508-003). No reflection over app types.

Acceptance criteria:

1. One tool per catalog endpoint. Tool name = the route's literal words (group path included) joined by `_`, sanitized to MCP tool-name characters, with a numeric suffix on a collision. Description = route description. Input schema = JSON Schema generated from `parameters[]` and `options[]`: same key names `--json-args` accepts, JSON types matching what the binder accepts (integer, number, boolean, string, arrays for repeated options and catch-alls), `required`, `default`, `enum` from allowed values, `additionalProperties: false`.
2. Annotations from `EndpointKind`: Query is `readOnlyHint` + `idempotentHint`, IdempotentCommand is `idempotentHint`, Command is neither. `destructiveHint` only from an explicit per-route attribute (`[Destructive]` on an endpoint class, carried in the catalog as `destructive: true`), never inferred.
3. Server instructions = app description plus optional app text.
4. `tools/call` binds with `TryBind`. A bind error (unknown key and the rest) is a tool result with `isError: true`, not a JSON-RPC error. It then invokes and returns the text the CLI would print (`RenderAsync` plus console output). A content-format option also returns JSON structured content. Calls are serialized.
5. Group filter option, using the catalog's `FilterByGroup`.
6. Hosting: `AddMcpRoute()` on the builder gives the app a built-in `mcp` route that serves stdio (generator-emitted like `--check-updates`). `IMcpServerBuilder.WithNuruTools(app)` serves the same tools from any MCP host, including ASP.NET Core streamable HTTP (`WithHttpTransport()` + `MapMcp()`).
7. Package name `TimeWarp.Nuru.McpHost`, which pairs with 507's `TimeWarp.Nuru.McpBridge` (McpHost hosts Nuru routes as MCP, McpBridge bridges MCP tools into Nuru).
8. Tests: `--capabilities` vs `tools/list` schema parity for the calculator sample, a `tools/call` round trip over the real protocol, annotation mapping, filter. Docs: a page beside `agent-invocation.md` and a note that MCP and CLI share names and arguments exactly.

## Checklist

- [x] Package scaffold, `ModelContextProtocol` reference (already pinned in `Directory.Packages.props`)
- [x] Catalog to tool definitions, including annotations and schema
- [x] `tools/call` through binder and invoke, text and JSON content
- [x] `mcp` built-in route (stdio) and HTTP hosting extension
- [x] Group filter
- [x] Tests and docs

## Review

- Round 1: Blocked — open M1
- Fix pass for round 2: M1 fixed (structured content no longer reads captured console output).

## Notes

- Parent: 508.
- Consumer: Roslynk's daemon would host this in-process so its MCP surface is derived from the same contracts its Nuru CLI is generated from; until then Roslynk reflects over its contracts assembly, and deletes that code when this lands.

## Session

- Created: 1479989 (2026-10-17)
- Implemented: implementer-claude under `ganda task work` (2026-10-19). Package, generator `AddMcpRoute()` and `[Destructive]`, mixed-app catalog fix, tests, docs. Full CI and `ganda repo audit` green.
- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-18T20:01:27Z
- Reviewer: grok session `01a1509a-db8a-7711-a1d9-777aacf447b5` (2026-10-19). Round 1, effort 3, roster general. M1 open. Disposition not written.
- Review oracle: review by reviewer-grok (grok, model grok-4.7), session not reported, max-turns 200 — 2026-10-18T20:23:31Z
- Fix oracle (implement-fix-2): implementer-claude (claude-opus-5-5), 2026-10-19. Fixed M1, added a regression test, updated the doc. Full CI and `ganda repo audit` green.

## Results

### Package `TimeWarp.Nuru.McpHost` (`source/timewarp-nuru-mcphost/`)

- `NuruMcpToolCatalog` (pure functions over `CapabilitiesResponse`):
  - `CreateTools(catalog, options)` returns `NuruMcpToolDefinition(Tool, EndpointCapability)` per endpoint, after `GroupFilter` (via `FilterByGroup`).
  - `GetToolName`: the pattern's literal words (group words first) joined by `ToolNameSeparator` (default `_`). Characters outside `A-Za-z0-9_-.` become `_`. A route with no literal words is `default`. Collisions get `_2`, `_3`.
  - `CreateInputSchema`: object schema keyed by the catalog names. Integer/number/boolean/string from the catalog type, `?` adds `"null"`, catch-all, repeated options and `[]` types are arrays, `enum` from `allowedValues`, typed `default` (C# quotes and numeric suffixes removed, `Env.Dev` becomes `Dev`), `description`, `required`, and `additionalProperties: false`.
  - `CreateAnnotations`: Query is readOnly + idempotent, IdempotentCommand is idempotent, Command is neither, Unspecified has no hints. `destructiveHint` is true only when the catalog has `destructive: true`, otherwise unset.
  - `CreateInstructions`: the app description, then the options text, separated by a blank line.
- `NuruMcpToolInvoker.CallAsync(endpoint, arguments)`:
  - Calls `TryBind`, `InvokeAsync`, and `RenderAsync` (to a `TestTerminal`). Calls go through one process-wide gate.
  - Console output from handlers (`Console.*` and the default terminal) is captured into the result.
  - A bind error, a handler exception, or a non-zero CLI exit code (problem details) is `isError: true` with the message. A bind error's message is the exact `--json-args` message.
  - `ContentFormat.Json` adds `structuredContent`: an object result as is, other values as `{"result": v}`, none for `unit`.
  - `structuredContent` is built from the CLR result and the text `RenderAsync` wrote, never from captured console output. Console lines stay in the text content only (M1 fix). Numbers, booleans, and strings wrap the CLR value. Other scalars (char, Guid, dates) wrap the rendered text.
- `NuruMcpTool : McpServerTool`. `NuruMcpServer.CreateTools`, `CreateServerOptions` (server info and instructions from the catalog), `RunStdioAsync` (points `Console.Out` at stderr for the session), and `RunAsync(app, input, output)`.
- `IMcpServerBuilder.WithNuruTools(app, configure)` is the hosting extension for any MCP host, including ASP.NET Core `WithHttpTransport()` + `MapMcp()`. It does not reference AspNetCore.
- `AddMcpRoute(instructions, groupFilter, contentFormat)` is a builder stub that the generator interprets.
- Console access is confined to `console-redirect.cs` (scoped `RS0030` pragma with the reason).
- `IsAotCompatible`, with no reflection. Release build packs `TimeWarp.Nuru.McpHost.4.0.0-beta.1.nupkg` with 0 warnings.
- Name check against 507: `McpHost` (host Nuru routes as MCP tools) and `McpBridge` (bridge MCP tool methods into Nuru routes) read as the two directions, so the working name stays.

### Generator and core

- `AddMcpRoute()`:
  - `DslInterpreter` reads its arguments as compile-time constants (non-constant is an error) into `McpRouteModel`, stored on `AppModel.McpRoute`.
  - `InterceptorEmitter` emits a built-in `mcp` route after user routes (like `--check-updates`) that calls `NuruMcpServer.RunStdioAsync(app, options, cancellationToken)`.
  - Help lists the row and the json-args built-in predicate knows it. `BuiltInFlags.McpRouteForm`.
- `[Destructive]` (`source/timewarp-nuru/attributes/destructive-attribute.cs`):
  - `EndpointExtractor` sets `RouteDefinition.IsDestructive`.
  - `CapabilitiesEmitter` emits `Destructive = true`, and only then. `EndpointCapability.Destructive` is `bool?` and omitted when null, so documents for other routes are byte-identical.
  - Endpoint classes only. Fluent routes cannot carry it.
- Fixed a pre-existing bug. `InterceptorEmitter.EmitClassEnd` built help and the catalog from `app.Routes` when the app had any fluent route. So an app with fluent routes plus `Map<T>()` / `DiscoverEndpoints()` omitted the endpoints from root help, `--capabilities`, and `app.Capabilities`, although the matcher and `TryBind` dispatched them. It now uses `RoutesInIndexOrder(app, model)`, the same list as the matcher and binder. Without the fix those endpoints would be missing from `tools/list`.

### Tests

- `tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs` (14 tests):
  - Tool names, separator, collisions, and the default route.
  - Schema: string, integer, boolean flag (not required), repeated, catch-all, enum, nullable, endpoint option description.
  - Annotation mapping and `[Destructive]` in the catalog and annotations.
  - Group filter and instructions.
  - `initialize`, `tools/list`, and `tools/call` over real newline-delimited JSON-RPC (in-memory pipes into `NuruMcpServer.RunAsync`).
  - Unknown, missing, and wrong-type keys and problem details come back as `isError` results, not JSON-RPC errors. The bind message equals `TryBind`'s.
  - Console and default-terminal capture, JSON structured content, and the help row from `AddMcpRoute`.
  - `Should_keep_console_output_out_of_structured_content` (M1 regression): `mh-log-report` logs a line and returns an object, and `mh-log-text` logs a line and returns a string. Under `ContentFormat.Json` both keep clean structured content, and the text block still has the log line.
- `tests/timewarp-nuru-mcphost-tests/mcphost-02-calculator-parity.cs` (2 tests):
  - Runs `calculator --capabilities`, then a real MCP client (`StdioClientTransport`) on `calculator mcp`.
  - Checks one tool per endpoint with the same name, description, schema (deep-equal to the projection, and member by member: names, required, JSON types, enum), and annotations.
  - Round trips `add`, `factorial` (`Console.WriteLine` captured), an unknown key (`isError`), and `stats`.
- CI wiring: `tests/ci-tests/Directory.Build.props` glob and McpHost ProjectReference. `timewarp-nuru.slnx` and `dev build` (`tools/dev-cli/endpoints/build-command.cs`) include the project, after `timewarp-nuru`. The packable set is discovered from `source/*.csproj`.
- Full CI after `ganda runfile cache --clear` (fix pass): multi-mode 1965 total, 1959 passed, 6 skipped, 0 failed. Standalone 0 failed.
- `ganda repo audit`: all checks pass. Nothing to fix in either pass.

### Docs

- New `documentation/user/features/mcp-hosting.md`, beside `agent-invocation.md`.
- `agent-invocation.md` gains an MCP section and the `destructive` member. `built-in-routes.md` gains the `mcp` row and section. `overview.md` gains an entry.
- `skills/tw-nuru/SKILL.md` gains the Roslynk-facing note that MCP and the CLI share names and arguments exactly.
- Changelog: Added McpHost and `[Destructive]`. Fixed the mixed-app help and catalog bug.
- The calculator sample has `.AddMcpRoute(...)`, so `calculator mcp` works.

### Notes (pre-existing, not changed)

- Catalog `required` describes argv, which is stricter than the JSON binder in two cases:
  - A flag option is `required: true` but binds `false` when absent. The schema drops flags from `required`.
  - An option with an optional value (`--name {n?}`, `--level {level:int?}`) is `required: true`, and the binder accepts the key absent. The schema keeps the catalog's `required`. That is stricter than the binder, so every schema-valid call still binds.
- Enum `allowedValues` are missing from the catalog when the enum is a nested type (an enum declared inside a class). The test uses a namespace-level enum. Worth its own task.
- The next release publishes a new package ID, `TimeWarp.Nuru.McpHost`. Trusted publishing is per repository and workflow, but confirm the NuGet.org policy covers new IDs before the release.

### How to validate

Smoke:

```bash
ganda runfile cache --clear
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-01-tools.cs
dotnet run tests/timewarp-nuru-mcphost-tests/mcphost-02-calculator-parity.cs
(printf '%s\n' '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"t","version":"1"}}}' '{"jsonrpc":"2.0","method":"notifications/initialized"}' '{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"add","arguments":{"x":2,"y":3.5}}}' '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"add","arguments":{"x":2}}}'; sleep 15) | dotnet run samples/endpoints/02-calculator/calculator.cs -- mcp
dotnet run tests/ci-tests/run-ci-tests.cs
```

Expect:

- `mcphost-01-tools.cs`: Total 14, Passed 14, including `Should_keep_console_output_out_of_structured_content`.
- `mcphost-02-calculator-parity.cs`: Total 2, Passed 2 (about 20 s each, because each test launches the sample).
- The stdio pipe prints three JSON-RPC responses:
  - id 1 has `serverInfo.name` `calculator` and the instructions text.
  - id 2 is `{"content":[{"type":"text","text":"5.5"}],"isError":false}`.
  - id 3 is `isError: true` with `Error: Missing required value 'y' for 'add'.`.
  - Nothing else appears on stdout.
- CI runner: multi-mode 1965 total, 1959 passed, 6 skipped, 0 failed. Standalone 0 failed.

### Review

Round 1 (effort 3, general) is in `review/round-1/merged.md`. One open bug (M1): `NuruMcpContentFormat.Json` builds `structuredContent` from captured console text plus the rendered body, so a handler that logs and returns a value drops or corrupts the structured payload. Fix pass: `CreateStructuredContent` now takes the `RenderAsync` terminal text, not `capturedOut + terminal.Output`, and `WriteScalar` writes a `string` result from the CLR value. The regression test is above. `mcp-hosting.md` now says console output stays in the text content.

## Learnings

Compared `## Goal (implementer restatement)` with `## Requirements`. No misread. The original's "route literal (group path joined by a documented separator, or the route's declared name)" has no separate declared-name field on `EndpointCapability`. Aliases are alternate patterns, not one name. The restatement's rule — literal words, group words first, joined by `_` — is that clause made concrete, and the separator is documented. `[Destructive]` on an endpoint class matches the optional per-route attribute. Fluent `Map()` routes have no attribute target, and the docs say so.
