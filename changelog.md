# Changelog

All notable changes to TimeWarp.Nuru will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **Analyzer**: NURU_I001 (warning) reports a `NuruApp.RunAsync()` call that no app intercepts, such as a call on a `NuruApp` parameter, on another method's return value, or in a lambda or local function that captures a local assigned more than one app (an assignment in another lambda counts). Before, it compiled and threw `RunAsync was not intercepted` at runtime. A method group (`Func<string[], Task<int>> run = app.RunAsync;`) is not a call and is not reported, and a call in the same method or top-level statements as a generator error is not reported, because that error is why its app has no interceptor (547).
- **Analyzer**: NURU_A003 (warning) reports an unfiltered `DiscoverEndpoints()` when the compilation builds Nuru apps in more than one source file, such as the Jaribu `JARIBU_MULTI` test assembly. Discovery then adds the endpoints of every other file to the app, and the generator emits their matcher, help and capabilities code once per discovering app. The message names the endpoint count and suggests `Map<TEndpoint>()`. A one-app CLI, one file with many apps (a standalone test run), `DiscoverEndpoints(typeof(Group))`, and an app that also calls `Map<T>()` are not reported. A test of discovery itself opts out with `#pragma warning disable NURU_A003` or runs standalone. The CI test assembly suppresses it until its 49 call sites move to `Map<T>()` (522).
- **Referenced contracts**: Endpoints can live in a referenced assembly. `[assembly: NuruEndpointAssembly(typeof(X))]` adds every public `[Operation]` and `[NuruRoute]` type in X's assembly to discovery (`DiscoverEndpoints()`, `DiscoverEndpoints(typeof(...))`, `Map<T>()`), and `Map<T>()` on a referenced endpoint type adds that endpoint to its app without the attribute. A referenced assembly adds nothing on its own. Members come from metadata: an omitted option keeps the compiled initializer, `--capabilities` shows a default from `[DefaultValue]`, `required` and nullability still apply, and a nested `Handler` the CLI cannot see is ignored. The JSON context build task covers referenced endpoints too. A contracts assembly needs `[assembly: MediatorAssembly]` for the CLI's generated mediator to dispatch to its handlers. Sample: `samples/endpoints/17-referenced-contracts/` (508-013).
- **MCP schema fidelity**: A member that binds as one JSON value (an array of objects, a nested object) now publishes the real JSON Schema of its type instead of `"type": "string"`, which the binder rejected. The schema is exported at catalog build from the generated `NuruJsonMemberContext` the `--json-args` binder deserializes with, so property names, `required` members and constructor parameters, nullable reference members, string-enum names and nested `[Description]` text match what binds. `--capabilities` carries it as the option's `schema` (`OptionCapability.Schema`). The contracts calculator sample adds a nested object member and an `mcp` route (508-015).
- **Tool hints**: `[TimeWarp.Contracts.OpenWorld]` / `[OpenWorld(false)]` and a title add `openWorld` and `title` to an endpoint's `--capabilities` entry (`EndpointCapability.OpenWorld`, `EndpointCapability.Title`), and the MCP host maps them to `openWorldHint` and the tool and annotation `title`. `[Destructive(false)]` publishes `destructiveHint: false`. An `[Operation]` title is `[Operation(Title = "...")]`. A `[NuruRoute]` class that is not an operation uses `[DisplayName]`. `NuruMcpHostOptions.ToolAnnotations` overrides any hint per tool name for an app that does not own its contracts (508-015, 518).
- **MCP defaults**: A member's default is in both the JSON Schema `default` keyword and a last `Default: X.` sentence of its description, because the Codex CLI drops `default` before the model sees the schema (Claude Code and Grok keep it). Flags publish `default: false` (508-015).
- **Concurrent calls**: The daemon (`TimeWarp.Nuru.Daemon`) and the MCP host (`TimeWarp.Nuru.McpHost`) run calls concurrently instead of one at a time. Each call runs in its own `NuruCallContext` (core): its own terminal (what the route writes through `ITerminal`, and through `Console.Out`/`Console.Error` during the call), its own exit code, the client's working directory, its own service scope (`Scoped`, `AddCommandScoped` and `AddSessionScoped` instances per call, for source-generated DI and `UseMicrosoftDependencyInjection()`), and its own cancellation. Nothing on the call path swaps the console, changes the current directory or touches `Environment.ExitCode`. A client disconnect, an MCP `notifications/cancelled`, or Ctrl+C on the forwarding CLI cancels only that call. `ping` and `stop` never wait; the idle timer still waits for every call. Eight forwarded 50 ms calls took 416 ms together before and 52 ms after (508-016).
- **Call context (core)**: `NuruCallContext` with `Current`, `Terminal`, `WorkingDirectory`, `ExitCode` and `CaptureConsole`, plus `CurrentExitCode`, `CurrentDirectory` and `GetFullPath(path)`, which fall back to `Environment.ExitCode` and `Environment.CurrentDirectory` in a plain CLI run. `NuruApp.ExecuteAsync(args, context)` and `NuruApp.InvokeAsync(request, context)` run a call in a context; `NuruApp.RenderAsync(request, result, terminal, context)` renders its result as the rest of that call, keeping the exit code the handler set and the call's directory. The daemon and McpHost render this way, so a handler's `NuruCallContext.CurrentExitCode` is the bound call's result. The generated `ITerminal` injected into services and behaviors follows the running call (508-016).
- **Exclusive routes**: `[Exclusive]` on an endpoint class, or `.Exclusive()` on a fluent route, makes its calls run one at a time against other exclusive calls on the app; other calls stay concurrent. `AddServeRoute(exclusiveCalls: true)`, `NuruDaemonOptions.ExclusiveCalls` and `NuruMcpHostOptions.ExclusiveCalls` run every call one at a time (508-016).
- **Served apps**: New package `TimeWarp.Nuru.Daemon` lets an app run as a long-lived local server that its own CLI forwards routes to, with no code per route. `AddServeRoute(socketPath, idleTimeoutSeconds, autoStart)` is interpreted by the generator. It adds the built-in routes `serve`, `serve --status` and `serve --stop`, and attaches a forwarding client. The server listens on a per-user Unix domain socket (`$XDG_RUNTIME_DIR/nuru/<app>-<8 hex>.sock` on Linux, the hex from the build id since 508-017, otherwise a 0700 temp directory, `%LOCALAPPDATA%` on Windows), never a TCP port. The socket directory must be owner-only (a symlink or a directory others can use is refused), the socket file is 0600, a relative `socketPath` is resolved under the per-user socket directory, and a lock beside the socket keeps a second `serve` from removing a running server's socket. It runs calls one at a time in the caller's directory and stops after an idle timeout, on `serve --stop`, or on Ctrl+C or SIGTERM. A matched route is forwarded after argument parsing and before the behaviors, so parse errors stay local, and behaviors and the handler run once in the server. The client prints the server's stdout and stderr and returns its exit code. `--json-args` values from stdin and `@path` are read by the client and sent as the object. When no server answers, the call runs locally. With `--daemon` or `autoStart`, the client first starts `<app> serve` detached (under `setsid` on Linux). `--no-daemon` runs locally. A server from another build of the app is never used and never stopped by the client: a started server must report the client's build, and each call carries the build id for the server to check (508-017). `[LocalOnly]` on an endpoint class, or `.LocalOnly()` on a fluent route, keeps the route in the CLI. Built-ins never forward. The MCP host forwards `tools/call` to the same server through the public binder. Sample: `samples/endpoints/16-daemon/served-app.cs` (508-005).
- **Forwarding contract (core)**: `NuruApp.RouteForwarder` (`INuruRouteForwarder`, with `NuruForwardRequest` and `NuruForwardResult`) is the hook that generated route code calls before behaviors. `NuruApp.ExecuteAsync(args)` runs CLI arguments through generated routing from code the generator does not intercept, such as a daemon host or an app built in another method. The catalog gains `localOnly: true` (`EndpointCapability.LocalOnly`) for local-only routes. Other routes have no `localOnly` member (508-005).
- **MCP hosting**: New package `TimeWarp.Nuru.McpHost` serves an app's routes as MCP tools, with no code per route. Each catalog endpoint becomes one tool. Its name is the route's literal words joined by `_`, its input schema is JSON Schema from `parameters[]` and `options[]` (same keys and JSON types `--json-args` accepts, `additionalProperties: false`), and its annotations come from the endpoint kind: queries are read-only and idempotent, idempotent commands are idempotent. `tools/call` binds with `TryBind`. A bind failure is a tool result with `isError` and the `--json-args` message, not a protocol error. The call then runs `InvokeAsync` and returns the text the CLI writes, with JSON `structuredContent` as an option. Console output during a call is captured into its result. `AddMcpRoute(instructions, groupFilter, contentFormat)` adds a built-in stdio `mcp` route, emitted by the generator. `IMcpServerBuilder.WithNuruTools(app)` adds the tools to any MCP host, including ASP.NET Core streamable HTTP. A group filter publishes a subset of routes. The calculator sample has `calculator mcp` (508-004).
- **Capabilities**: `[Destructive]` on an endpoint class adds `"destructive": true` to that endpoint in `--capabilities` (`EndpointCapability.Destructive`) and sets the MCP tool's `destructiveHint`. Nothing infers it. Endpoints without the attribute have no `destructive` member, so their documents are unchanged (508-004).
- **Bind and invoke**: `NuruApp.TryBind(route, JsonElement, out request, out error)` binds a JSON object to a route named by its catalog pattern, with the `--json-args` rules and no argv. `NuruBindError` carries `Kind`, `Key`, and the message `--json-args` prints. `NuruApp.InvokeAsync(request)` runs services, the command scope, Nuru behaviors, and the handler, and returns the typed result instead of writing it; `NuruApp.RenderAsync(request, result, terminal)` writes it as the CLI does and returns the CLI exit code. `--json-args` and `TryBind` call the same generated bind method per route. An app with only `Map<T>()` endpoints that is built but never run now gets route execution too, so these calls work on it (508-003).
- **Capabilities**: `NuruApp.Capabilities` returns the generated `CapabilitiesResponse` in process, with no reflection and no subprocess, and `CapabilitiesResponse.FilterByGroup(group)` returns the `--group-filter` view. The generator intercepts each app's `Build()` to attach the catalog, so it is available before `RunAsync()`; an app built but never run is now generated too. `--capabilities` prints the same cached instance. Each endpoint gains a last `returnType` member (`name`, and `kind` of `unit`, `primitive`, or `json`, from the CLI output strategy). It is the only change to the printed document, and it is nullable so older documents still load (508-002).
- **Neutral contracts**: `UseDirectInvoke()` calls a nested `Handler` directly. `[NuruCliBinding(Group)]` sets the group path when no `[NuruRouteGroup]` is present. NURU_H008 warns when an `[Operation]` endpoint has no visible mediator handler. NURU_H009 is an error when an endpoint type is abstract, static, or has no constructor Nuru can call; the type is skipped. A visible or registered `IResponseRenderer<T>` renders the response before the output strategy. `OneOf<TSuccess, SharedProblemDetails>` writes the problem to stderr and sets the exit code (508-001).
- **Neutral contracts**: `[Operation(Group = "symbols")]` from `TimeWarp.Contracts` 2.0.0-beta.23 sets the endpoint's `groupPath`, split on `.` or space. The group is metadata: the route, the command, and the MCP tool name stay the operation name. A non-blank operation group replaces any `[NuruRouteGroup]` prefix and `[NuruCliBinding(Group)]`. A blank one keeps the earlier rules. `--group-filter` and the McpHost `groupFilter` read the same path, and `FilterByGroup` now splits the filter on `.` or space (508-014).

### Fixed
- **DevCli**: `dev release` no longer refuses when the commits after the last green CI run change only `kanban/`. Those board commits get no CI run, so each release raced board traffic. It now tags the newest first-parent commit with a successful `workflow.yml` run (at most 50 commits back) and prints `✓ HEAD is N kanban-only commit(s) past <sha> (CI run <id>)`. Any later commit outside `kanban/` still refuses, naming the commit and path. `--dry-run` shows the target commit (543).
- **DevCli**: `check-version`, `dev release` and the release pipeline now ask both org feeds, nuget.org and the org GitHub Packages feed, whether a version is published. Before, they asked only nuget.org, so a package published to GitHub Packages always looked new and could be released twice. GitHub Packages uses `GITHUB_TOKEN`, `GH_TOKEN` or `gh auth token`. When a version is not on nuget.org and GitHub Packages cannot be read, the gates refuse with the feed, the HTTP status and the fix, not "already released". Workflow jobs that run them need `packages: read` (536).
- **Telemetry**: `UseTelemetry()` now emits one span per command. It registers the built-in `TelemetryBehavior` as the outermost behavior, so a command span (named by the route pattern, with `command.name` and `command.type`, status Ok or Error plus `error.type`) reaches `/v1/traces` without `.AddBehavior(typeof(TelemetryBehavior))`. Adding the behavior anyway does not create a second span, and the span is a child of `Activity.Current`. Before, only metrics and logs exported: the generator's span helpers were never called, and they are removed. A fluent route whose handler is a method group (`WithHandler(Handlers.Run)`) with any behavior also failed to compile (`__command` does not exist); it now gets a generated command class like a lambda route (552).
- **RunAsync interception**: `RunAsync()` and `RunReplAsync()` calls are now intercepted in any expression, not only as `await app.RunAsync(...);`, `var x = await app.RunAsync(...);` or `return await app.RunAsync(...);`. `(await app.RunAsync(args)).ShouldBe(0)`, a method argument, a ternary branch, `(await app.RunAsync(args).ConfigureAwait(false))`, a lambda in the method, `app!.RunAsync(...)`, `app?.RunAsync(...)` and `NuruApp.CreateBuilder()...Build().RunAsync(args)` in an expression-bodied member compiled and then threw `RunAsync was not intercepted`. The generator finds the calls by method symbol and resolves the receiver to the app built in that method. A local reassigned to a second app binds each call to the app it holds at that point in the source (547).
- **Service loggers**: A registered service (`AddSingleton<MyService>()` and the other lifetimes) that injects `ILogger<T>` or `ILoggerFactory` under source-generated DI now gets the app's `AddLogging` factory, or the telemetry factory with `UseTelemetry()`. Before, it always got a `NullLogger`, even with logging configured, so its messages were dropped (545).
- **Search**: `nuru search` now writes its warnings and errors to stderr, such as why a CLI did not index (`CLI ... returned exit code 2`) or why a search failed. It never configured logging, so every message went to a `NullLogger`. Stdout stays results-only (545).
- **Delegate handler services**: A `.WithHandler((ILogger<X> logger) => ...)` parameter now gets a logger from the app's `AddLogging` factory (or telemetry logging), as endpoint handler and behavior constructors do. Before, it compiled to `default!` with no diagnostic, so the handler threw `NullReferenceException` on its first log call. Without logging it gets a `NullLogger` and `NURU_H007` warns. Delegate parameters, endpoint handler constructors and behaviors now resolve services through one path, so `ILoggerFactory` and typed `AddHttpClient<TClient, TImpl>()` clients work as delegate parameters too. An `IOptionsSnapshot<T>` or `IOptionsMonitor<T>` delegate parameter, which is not bound from configuration, now reports `NURU050` instead of compiling to null, and a user interface whose name contains `ILogger` is resolved as a normal service. A typed HttpClient whose service type is in the global namespace no longer generates an invalid field name. `NURU_H007` no longer warns when `UseTelemetry()` configures logging (541).
- **Telemetry**: An app that calls `UseTelemetry()` now builds without `using Microsoft.Extensions.Logging;` in its own code. Before, the generated logging setup called `SetMinimumLevel` and `AddOpenTelemetry` as `ILoggingBuilder` extension methods, which only the app's usings could resolve, so the build failed with CS1061 in `NuruGenerated.g.cs`. The generated tracing, metrics, resource and exporter setup now calls every extension method in static form with `global::` types, and the generated file no longer adds `OpenTelemetry` usings (521).
- **Unknown options fail closed**: An option-shaped token that the route does not declare now fails the call with exit code 1 and `Unknown option '--name'`, plus `Did you mean '--form'?` when a declared option of the matching command is close. Before, a route with options took the token as an extra positional and ran the handler with exit 0, so `kanban create "Title" --repo x` ran without `--repo`. Negative numbers and a lone `-` stay values, and everything after `--` stays positional. Catch-all routes take option-shaped args only after `--` (`exec ls -- -la`), a route with options rejects an extra positional, and a repeated boolean flag is accepted. A typo of a required option or of a flag the route needs (`commit --ammend` for `commit --amend`) gets the same suggestion, including on a route with no command word (`--check-update` for `--check-updates`, `readme --outpt out` for `{file} --output {output}`). REPL lines follow the same rules (515).
- **Aliases**: A route alias whose option check failed (an option given with no value, or a required option or flag missing) no longer loops forever. The alias jumped back to the primary route's skip point, which comes before the alias, so it ran again with the same arguments. It now skips to its own end (515).
- **Completion**: The generated zsh script completes on the first Tab. `compinit` autoloads the file as the completion function, and zsh does not run the function the file defines, so a trailing `compdef` registered the command and that first Tab did nothing. The script runs the function when zsh is loading it, and still calls `compdef` when the script is sourced or eval'd (509).
- **Served apps**: A client from another build of a served app no longer stops the running server, which cancelled its calls in flight (exit 130) and dropped its warm state on every alternate call when two builds shared one socket, such as a `dnx` package-cache copy beside a globally installed tool. The default socket is now one per build, `<app>-<8 hex>.sock` with 8 hex digits of SHA-256 of the build id, so builds run side by side, each warm. When two builds still share a socket (`socketPath` or `NURU_DAEMON_SOCKET`), the other build's server is left running: an Auto call runs locally, and `--daemon` or `autoStart` fails with one line naming both builds and `serve --stop`. Ping carries the build's entry path and write time, and `serve --status` prints them and lists other builds' servers of the app in the default socket directory. Stopping stays explicit (`serve --stop`), and the idle timeout still stops a server nobody calls, so an old build's server goes away after a rebuild (508-017).
- **Search**: `nuru search` results no longer print the group path in front of the pattern. The catalog pattern already holds any route-group prefix, so grouped routes showed it twice (508-014).
- **Neutral contracts**: An `[Operation]` or `[NuruRoute]` request whose members are declared in camelCase (`solutionId`, `checkOnly`) now compiles. The generated request construction wrote each member re-spelled in PascalCase (`SolutionId = ...`), which failed with CS0117 and CS9035 on argv, on `--json-args` and in `TryBind`. Members are now written as declared, with `@` for a keyword name (508-006).
- **JSON output**: The generated `NuruUserTypesJsonContext` no longer reports SYSLIB1031 when two operations nest types with the same short name, such as two `Response` classes. A `OneOf<TSuccess, SharedProblemDetails>` endpoint registers only `TSuccess`, and registers nothing when a renderer is found. Any types whose generated JSON names collide, including types reached through members such as two `Item` classes or `List<Item>`, each get a `TypeInfoPropertyName` built from the full type name (508-006).
- **JSON members**: Names inside a `--json-args` JSON value now match the declared members ignoring case, so an MCP client's camelCase `{"tool":...}` binds `Tool` instead of leaving it at its default. The top-level `--json-args` keys still match exactly (508-006).
- **Help and capabilities**: An app that maps fluent routes and also `Map<T>()` or `DiscoverEndpoints()` endpoints now lists the endpoints in root help and `--capabilities`, as well as the fluent routes. Before this fix only the fluent routes were listed, although all of them could be invoked (508-004).
- **Neutral contracts**: Omitting an option whose initializer cannot be copied (`new()`, a method call) keeps that initializer on argv and on `--json-args`. `--capabilities` omits it, shows plain numbers, and shows an enum member name. `init` members and positional records are derived. A JSON member whose name is a C# keyword still generates a valid rejection flag. A derived type that redeclares a base property does not derive the base property again. NURU_H008 says invoking the route exits 1. A build with no generated mediator calls a nested handler directly and exits 1 for a visible external handler, including when an `INuruBehavior` applies. A `required` member whose initializer cannot be copied is always set, and several such `init` members no longer multiply the generated code. A positional record takes its constructor parameter defaults. A nullable enum keeps its initializer when omitted, including `= null`. `--capabilities` shows a `[Flags]` combination as member names (508-001).

### Changed
- **NURU_H007 (breaking for warnings-as-errors builds)**: An endpoint handler constructor or a registered service constructor that injects `ILogger<T>` now gets the `NURU_H007` warning when the app configures no logging (no `AddLogging(...)` and no `UseTelemetry()` logging), as behaviors and delegate handlers already did. The logger is a `NullLogger` that discards every message, so the warning was missing for the most common logger sites. A build that treats warnings as errors fails until it adds `ConfigureServices(s => s.AddLogging(...))` (545).
- **Source-generated DI**: Services are constructed the first time a handler or behavior reads them, not when services initialize at the start of every call. A forwarded call, a built-in (`--help`, `--version`, `--capabilities`) and a parse error construct no user service, and a local route constructs only the services its handler and behaviors reach. Lifetimes are unchanged: one singleton per app, built once under a lock when concurrent calls race, and session and command instances per scope, disposed as before. The served-app sample loads its warm state in the singleton constructor instead of a `Lazy<T>`. An `AddScoped` service that injects a session- or command-scoped service lives in the run's command scope, so each call, including concurrent served calls, gets its own. A singleton that injects such a Scoped service now gets NURU056, like a singleton that injects the session or command service directly, and receives its own copy instead of an instance the run disposes (508-007).
- **Served calls**: A handler in a daemon or MCP host call must set `NuruCallContext.CurrentExitCode` (or return problem details) instead of `Environment.ExitCode`, which is process-wide and is now ignored for the call, and must resolve relative paths with `NuruCallContext.GetFullPath`, because the server no longer changes its current directory per call. A plain `RunAsync` still returns the exit code and leaves it in `Environment.ExitCode`. `NuruApp.InvokeAsync(request)` and `RenderAsync` now run in their own call context: they no longer read or set `Environment.ExitCode`, and each call gets its own service scope. The McpHost puts what a handler writes through `ITerminal` into the call's result even with `CaptureConsoleOutput = false`, and a forwarded call no longer uses a terminal set with `UseTerminal`. The daemon socket listens with the system's maximum backlog (508-016).
- **Neutral contracts**: Nuru derives endpoints from `TimeWarp.Contracts` `[Operation]` plus a TimeWarp.Mediator kind (508-001). The reference is `TimeWarp.Contracts` 2.0.0-beta.24 (2.0.0-beta.23 before 518, 2.0.0-beta.21 before 508-014), still a prerelease, so the package version is a `4.0.0-beta.*` prerelease until that dependency is stable. A stable Nuru package does not take a prerelease dependency.
- **Tool hints**: `[Destructive]` and `[OpenWorld]` are `TimeWarp.Contracts` attributes, matched by metadata name, on contract and route endpoints. An operation's title is `OperationAttribute.Title`; blank or whitespace is no title, and `[DisplayName]` does not set it. A `[NuruRoute]` class with no `[Operation]` still uses `[DisplayName]` for the title. A host override still wins (518).
- **Dispatch**: With a generated mediator, endpoints go through the mediator unless `UseDirectInvoke()` selects a nested handler. A nested handler still uses the 3.0 direct call when no generated mediator exists. An endpoint with no nested handler still uses the mediator. Each command resets `Environment.ExitCode` before it runs. A `[NuruRoute]` class whose handler is registered elsewhere is an endpoint (508-001).
- **Dispatch overhead**: `benchmarks/operation-dispatch/operation-dispatch-overhead.cs` warms shared Nuru code with a delegate route, then times each path's first call in microseconds and each later call in nanoseconds. One run (50 warm-up calls, 2000 measured calls, `Unit` response so the loop does not write the handler result) reported `mediator-first-us=22708`, `direct-first-us=2548`, `mediator-per-call-ns=3296`, and `direct-per-call-ns=1788`. The mediator first call still includes building its service provider. Startup is compared with `--aot`, which publishes both paths and times three alternating cold starts: mediator `4,3,3` ms and direct `3,2,2` ms. Direct is lower per call, so `UseDirectInvoke()` stays (508-001).

### Removed
- **`TimeWarp.Nuru.DestructiveAttribute` and `TimeWarp.Nuru.OpenWorldAttribute`**: removed. The same short names mean `TimeWarp.Contracts` only, so a file that imports `TimeWarp.Nuru` and `TimeWarp.Contracts` compiles. Call sites take `using TimeWarp.Contracts`. An `[Operation]` that used `[DisplayName]` for its title uses `[Operation(Title = "...")]` (518).
- **BREAKING: `UseLogging(ILoggerFactory)`, `UseConsoleLogging(...)`, `UseDebugLogging()` and `ConfigureLogging(...)`**: removed, with the `NuruLoggingExtensions` class. The source generator never supported them, so every app that called one failed to build with `NURU_S999 Unrecognized DSL method`. Configure logging with `ConfigureServices(s => s.AddLogging(b => b.AddConsole().SetMinimumLevel(...)))` and inject `ILogger<T>` into endpoint handlers, behaviors or services. `UseConsoleLogging(level)` becomes `AddLogging(b => b.AddConsole().SetMinimumLevel(level))`, `UseDebugLogging()` becomes the same with `LogLevel.Trace`, and a `UseConsoleLogging`/`ConfigureLogging` lambda moves into `AddLogging` unchanged. `Microsoft.Extensions.Logging.Console` still comes with the package (527).

## [3.0.0] - 2026-10-14

Changes since `3.0.0-beta.79`; beta.80 has no separate section. First stable 3.0 release. It supersedes the `3.0.0-beta.*` line (last published beta is `3.0.0-beta.80`, 2026-10-12). This is the first 3.0 package built in Release; the betas were Debug (see the first Packaging entry under Fixed, task 503).

### Changed
- **DevCli**: `dev release` passes `--prerelease` to `gh release create` when `<Version>` has a SemVer prerelease suffix, so the GitHub Release flag matches NuGet. `--dry-run` prints the same command it runs (486).
- **TimeWarp.Mediator 14.0.0**: Contracts and Generators moved from 14.0.0-beta.5 to stable 14.0.0 (489), after the beta.5 dogfood (498). Generated host types stay internal to the compilation that emits them. A stable Nuru package does not depend on a prerelease of Mediator, so consumers do not need a prerelease opt-in for it.
- **TimeWarp.Amuru 2.0.0**: `TimeWarp.Amuru` and `TimeWarp.Amuru.Tools` moved from 2.0.0-beta.2 to stable 2.0.0 (502). A stable Nuru package does not depend on a prerelease of Amuru, so consumers do not need a prerelease opt-in for it. 3.0.0-beta.80 was the one beta that depended on the 2.0.0-beta.2 prerelease.
- **Dependencies**: Roslynator analyzers 5.0.1 and Spectre.Console.Cli 0.57.2 (benchmarks only); neither ships to consumers. `Microsoft.CodeAnalysis.CSharp` stays at 5.6.0, so the minimum consumer SDK is unchanged (10.0.3xx); 5.9.0 would require SDK 10.0.400. `Microsoft.Build.Utilities.Core` stays at 18.9.6 because 18.10.1 has no `net10.0` build.

### Fixed
- **Packaging**: Published 3.0 betas up to beta.80 were packed from Debug builds, because the CI sample and test steps repacked over the Release package. Packing is now Release-only, and CI verifies the artifact's configuration and hashes before upload (503).
- **Packaging**: The package now ships `build/TimeWarp.Nuru.props` and `buildTransitive/TimeWarp.Nuru.props`, which append `TimeWarp.Nuru.Generated` to `InterceptorsNamespaces`. A fresh console app or a `#:package TimeWarp.Nuru` runfile builds with no extra properties; previously both failed with CS9137 (488).
- **Completion**: `--generate-completion bash` works in plain bash. The script uses the bash-completion package (`_init_completion`, `_filedir`) when it is loaded and otherwise reads `COMP_WORDS`/`COMP_CWORD` and calls `compgen` itself; previously Tab printed `_init_completion: command not found`. The zsh script runs `compinit` when `compdef` is not defined yet (492).
- **Analyzers**: `NURU_H002` no longer reports a nested lambda's own parameter as a closure, so `(int[] ids) => ids.Sum(i => (long)i)` builds. The validator now checks whether a local or parameter is declared inside the handler instead of matching names, which also stops false reports on pattern variables declared in the handler (491).
- **REPL**: `--interactive` no longer throws `InvalidOperationException` when stdin is redirected. With piped input the REPL reads whole lines, runs them in order, and exits cleanly at EOF; an unusable console prints one line to stderr and returns a non-zero exit code instead of crashing (493).
- **REPL**: `NuruApp.RunReplAsync()` now returns `Task<int>` (the last command's exit code, or non-zero when the console is unusable) and `--interactive` propagates it as the process exit code instead of always returning 0 (493).

## [3.0.0-beta.79] - 2026-10-06

Changes since `3.0.0-beta.76`, the last published beta. beta.77 and beta.78 were never published.
Dogfood release: this beta is meant to be consumed across the TimeWarp repos before `3.0.0` is cut.

### Fixed
- **Generator**: typed catch-all and repeated options use non-throwing `TryParse` for every built-in conversion, including `DateTimeOffset` and `Version` (482-005). Invalid route patterns report a diagnostic instead of emitting a fallback literal route (482-006). `Build()` on an endpoint registers the open route (482-001). Optional parameters render as `{name?}` in diagnostics (482-007). The emit cache key no longer holds a Roslyn `Location` (482-008).
- **Completion**: later parameters bind, command prefixes match on token boundaries, user types resolve (482-002); `File` and `Directory` candidates and directive flags are honored (482-003).
- **REPL**: the `RunReplAsync` stub tells the caller to enable the source generator (482-004).
- **Search**: no JSON array after the listing (482-009); `--group` matches dotted group paths (482-010); index rebuild remembers the executable path (482-011).
- **Docs**: guides, parser reference, error-handling guide, and public XML examples match the 3.0 API (482-013, 482 R-7).

### Removed
- **`TimeWarp.Nuru.Mcp` is not part of the 3.0 package set**: the project is `IsPackable=false`, so the build and release pipeline no longer produce or publish `TimeWarp.Nuru.Mcp.*.nupkg`. The MCP server is frozen and its `GetSyntax` / `GenerateHandler` answers do not match 3.0. Use the Nuru skill instead. The source stays in the repo and still builds; `dotnet pack -p:NuruMcpPack=true` packs it locally.

### Changed
- **TimeWarp.Mediator 14.0.0-beta.4**: Contracts and Generators moved from 14.0.0-beta.3. Generated host types (`Mediator`, `MediatorManifest`, `GeneratedMediatorServiceCollectionExtensions`) are `internal` to the compilation that emits them. TimeWarp.Nuru no longer strips the mediator generator from its own compile; `TimeWarpMediatorAssembly` stays `false`, so the library still does not call `AddGeneratedMediator()`. Apps keep a single call in their own compilation.
- **BREAKING: message and handler contracts moved to `TimeWarp.Mediator`**: TimeWarp.Nuru no longer defines `IMessage`, `IQuery<T>`, `ICommand<T>`, `IIdempotentCommand<T>`, `IIdempotent`, `IQueryHandler<,>`, `ICommandHandler<,>`, `IIdempotentCommandHandler<,>`, or `Unit`. Endpoints use the `TimeWarp.Mediator` 14.0.0-beta types instead. Migration:
  - Add `using TimeWarp.Mediator;` next to `using TimeWarp.Nuru;` (or a global `<Using Include="TimeWarp.Mediator" />`).
  - Handlers return `Task<T>` instead of `ValueTask<T>` (`public Task<Unit> Handle(...)`). Replace `return default;` in non-async `Task<Unit>` handlers with `return Unit.Task;`, and `new ValueTask<T>(value)` with `Task.FromResult(value)`.
  - Drop `using static TimeWarp.Nuru.Unit;`. `TimeWarp.Mediator.Unit` has a static `Task` property, so a static using of it hides `System.Threading.Tasks.Task`. Use `Unit.Value` / `Unit.Task`.
  - `[NuruRoute]` endpoint classes must be `public` for now: the Mediator generator emits public `Send(TRequest)` overloads, so `internal` requests fail with CS0051 ([timewarp-mediator#64](https://github.com/TimeWarpEngineering/timewarp-mediator/issues/64)).
- **Endpoint kind classification**: `[NuruRoute]` endpoints are classified from `TimeWarp.Mediator` interfaces with explicit precedence: Query, then IdempotentCommand (`IIdempotentCommand<T>`, or `ICommand<T>` plus `IIdempotent`), then Command. Previously the first matching interface won, so an idempotent command could be reported as a plain Command.

### Added
- **2.x → 3.0 migration guide**: `documentation/user/guides/migrating-to-3.0.md`.
- **`key-bindings`**: REPL command and `tools/nuru-key-bindings` list built-in key bindings (Default, Emacs, Vi, VSCode) grouped by category, with Key, Function, and Description. Filter with `--key`, `--function`, and `--profile`, or pass `--detailed` for one block per binding. Custom and JSON profiles are not expanded. In the REPL, `key-bindings` is a built-in and takes precedence over an app route with the same first word.
- **TimeWarp.Mediator 14.0.0-beta.4**: TimeWarp.Nuru now depends on `TimeWarp.Mediator.Contracts` and `TimeWarp.Mediator.Generators`. The generator flows into consuming apps, so each app compilation emits its own source-generated `IMediator` / `ISender` / `IPublisher`. Apps using `.UseMicrosoftDependencyInjection()` get `AddGeneratedMediator()` called automatically, so handlers can inject `ISender` / `IPublisher`.
- **`ISender` / `IPublisher` / `IMediator` under source-generated DI**: handlers, services, and behaviors in apps without `.UseMicrosoftDependencyInjection()` can inject the generated mediator. Nuru builds a small container on first use holding `AddGeneratedMediator()`, Nuru's `ITerminal` / `NuruApp` / `IConfiguration` / logging, and the services registered in `ConfigureServices`, so mediator handlers resolve their dependencies. Apps that never inject a mediator type get no container code.

## [3.0.0-beta.78] - not published

The version moved to `3.0.0-beta.78` on 2026-09-25 and to `3.0.0-beta.79` on 2026-10-06. No tag, GitHub release, or NuGet package was made for beta.78. Its changes are listed under 3.0.0-beta.79.

## [3.0.0-beta.77] - not published

The version was set to `3.0.0-beta.77` on 2026-08-27 and moved to `3.0.0-beta.78` on 2026-09-25. No tag, GitHub release, or NuGet package was made for beta.77. Its changes are listed under 3.0.0-beta.79.

## [3.0.0-beta.76] - 2026-08-14

### Fixed
- Generated string literals escape U+0085, U+2028, and U+2029, so descriptions that contain those characters compile (#226)

## [3.0.0-beta.75] - 2026-08-14

### Added
- **Route examples**: routes can declare examples. They appear in `--help` and in `--capabilities` output (#223)

### Fixed
- The generator reports an error when a `WithExample` command is not a literal, and no longer drops diagnostics from the builder chain
- Packages include `PackageProjectUrl` metadata (#225)

## [3.0.0-beta.74] - 2026-08-10

### Added
- Repeated options accept enum arrays (task 440, #222)

### Fixed
- The generator escapes C# keyword identifiers (for example a parameter named `class`) at every emit site (task 460, #222)

## [3.0.0-beta.73] - 2026-08-10

### Fixed
- The `TimeWarp.Nuru` package ships the full build-task payload. Explicit pack includes and a package layout check stop the build `.dll` going missing again (task 461, #221)

## [3.0.0-beta.72] - 2026-08-08

### Changed
- **Release process**: one release workflow, `dev release` cuts the tag and GitHub release from the props version, a promotion pipeline, a three-state `check-version` gate, and a CI check of ganda audit attestations (task 458, #219). The packable set comes from `IsPackable`.

### Fixed
- Review sweep from task 454 (July–August 2026). It covers:
  - Generator caching: `EquatableArray<T>` models, no live `Location` in the models, and no `CompilationProvider` in the emit path
  - SemanticModel checks in place of string type heuristics
  - A DSL interpreter hardened against invalid user code
  - A false `NURU_R003` unreachable-route warning
  - Lexer whitespace and end-of-options handling
  - Enum completion overflow for wide underlying types
  - Ctrl+C cancellation of REPL commands
  - REPL reader and screen desync bugs
  - Windows clipboard set
  - PowerShell 5.1 completion quoting
  - MCP example-cache fallback and thread safety
  - Search FTS query sanitizing
- Single-dash options may be longer than one character. Matching is exact, so a short option is no longer treated as a character inside a longer token (`-e` no longer matches `-help`)
- Generated help escapes `{`, quotes, and backslashes in descriptions, so text such as `greet {name}` compiles

## [3.0.0-beta.71] - 2026-06-15

### Fixed
- Per-command `--help` shows `[Parameter(Description)]` text (#215, #218)

## [3.0.0-beta.70] - 2026-04-27

No git tag. The GitHub release is tagged `v3.0.0-beta.69`, and that commit's package version is `3.0.0-beta.70`. NuGet published `3.0.0-beta.70` on this date. There is no `3.0.0-beta.69` package.

### Changed
- `dev check-version` uses the NuGet.org V3 flat-container API through `HttpClient` in place of NuGet.Protocol, so it works under AOT. It defaults to the `nuget-search` strategy.
- `TimeWarp.Nuru.DevCli` gets `IRepoConfigService` (#187) and a source-generated JSON context for `.timewarp/dev.jsonc`
- Updated dependencies: TimeWarp.Amuru 1.0.0-beta.32 (no Newtonsoft.Json), Microsoft.Extensions 10.0.7

## [3.0.0-beta.69] - not published

No NuGet package. The GitHub tag `v3.0.0-beta.69` points at the `3.0.0-beta.70` tree. See beta.70.

## [3.0.0-beta.68] - 2026-03-24

### Changed
- The release workflow no longer runs `check-version`, which had made releases exit with code 1
- `check-version` output names the strategy, the version in source, and the latest release

## [3.0.0-beta.66] / [3.0.0-beta.67] - 2026-03-24

These two betas have the same release notes.

### Changed
- **Source-gen DI**: framework types are registered as services (#208), and constructor dependencies are resolved

### Fixed
- Two regressions from the AOT DI refactor that broke most generated apps: `EnsureServicesInitialized` is always emitted (CS0103), and `Lazy<T>` behaviors are called through `.Value` (CS1061)
- Dev CLI workflow handlers call the handler directly instead of a non-intercepted `App.RunAsync()`, and `DevCli.Commands` is renamed `DevCli.Endpoints`

## [3.0.0-beta.65] - 2026-03-23

### Fixed
- CI packs and pushes `TimeWarp.Nuru.DevCli`, so the package is published from this beta on

## [3.0.0-beta.64] - 2026-03-23

### Added
- **`TimeWarp.Nuru.DevCli`**: a source-only package with reusable dev-CLI endpoints (`clean`, `self-install`, `check-version`)

### Fixed
- REPL session uses `ITerminal` for Ctrl+C and error output in place of `Console`

## [3.0.0-beta.63] - 2026-03-17

### Added
- `nuru` search indexes CLIs on demand through `PathResolver`

### Changed
- Updated 29 NuGet packages

## [3.0.0-beta.62] - 2026-03-15

### Fixed
- `--capabilities --search` calls `nuru` in place of `nuru-search`, and its error messages name `TimeWarp.Nuru.Search`

## [3.0.0-beta.61] - 2026-03-15

### Added
- A `cli-search` agent skill for the `nuru` search tool

## [3.0.0-beta.60] - 2026-03-15

### Fixed
- When `.WithName()` is not called, `--capabilities` uses the entry assembly name in place of `"app"`

## [3.0.0-beta.59] - 2026-03-15

### Changed
- **BREAKING**: the search tool's command is now `nuru`, not `nuru-search`

## [3.0.0-beta.58] - 2026-03-15

### Added
- `--capabilities --search <query>` and `--capabilities --group-filter <group>`
- **`TimeWarp.Nuru.Search`**: a new dotnet tool that searches across Nuru CLIs with SQLite FTS5

## [3.0.0-beta.57] - 2026-03-07

### Fixed
- Generated code uses `global::TimeWarp.Nuru.Unit` in place of a bare `Unit`, so it does not clash with other packages' `Unit` types (task 442, #191)

## [3.0.0-beta.56] - 2026-03-04

### Changed
- **BREAKING: `--capabilities` contract**: output is a flat `Endpoints` list of `EndpointCapability` items, with an `EndpointKind` enum and `GroupPath`, `Aliases`, `DefaultValue`, `AllowedValues`, and `IsFlag` fields. `EndpointCapability` replaces `CommandCapability` and `GroupCapability`. JSON is serialized from typed DTOs.

## [3.0.0-beta.55] - 2026-03-03

### Changed
- The capabilities DTOs (`CapabilitiesResponse`, `GroupCapability`, `CommandCapability`, `ParameterCapability`, `OptionCapability`, `CapabilitiesJsonSerializerContext`) are public, so callers can deserialize `--capabilities` output

## [3.0.0-beta.53] / [3.0.0-beta.54] - 2026-02-20

### Fixed
- `.DiscoverEndpoints(typeof(GroupBase))` strips the root group prefix when it publishes a subset (#184). The fix landed after the beta.53 package was built, so beta.54 is the first package that has it.

## [3.0.0-beta.52] - 2026-02-18

### Added
- Handlers can set `Environment.ExitCode`. Generated code returns it in place of a fixed `0` (#182)

### Fixed
- `[NuruRoute("")]` endpoints with required parameters no longer intercept `--help` (#179, #181)

## [3.0.0-beta.51] - 2026-02-17

### Changed
- Route matching uses one code path (`EmitMatch` / `EmitAliasMatch`). This ends a class of bugs where the simple and complex paths behaved differently (task 431)

### Fixed
- Empty-pattern endpoint routes no longer intercept `--help` (#179)
- `[NuruRouteAlias]` on group base classes no longer emits literal segments twice (#178)

## [3.0.0-beta.50] - 2026-02-16

### Fixed
- REPL and shell completions (bash, zsh, fish, PowerShell) no longer show duplicate suggestions (#177)

## [3.0.0-beta.49] - 2026-02-15

### Fixed
- Endpoint DSL (`DiscoverEndpoints()`) services with constructor dependencies use `Lazy<T>` fields with resolved arguments, as the Fluent DSL already did (#175)

## [3.0.0-beta.47] / [3.0.0-beta.48] - 2026-02-14

beta.48 is a version bump of beta.47 with no further changes.

### Added
- The source generator supports `AddHttpClient` (task 423). Added an HttpClient sample (task 422)
- `DiscoverEndpoints(typeof(GroupBase))` filters endpoints by group type

### Fixed
- Source-gen DI resolves services with parameterized constructors at compile time in place of falling back to MS DI without a message (#172)

## [3.0.0-beta.46] - 2026-02-11

### Added
- `[GroupOption]` on `[NuruRouteGroup]` base classes: all routes in the group get the shared options (#168)
- `ReplOptions.AutoStartWhenEmpty` starts the REPL when no arguments are given (#166)

### Changed
- Help output: OPTIONS come before PARAMETERS, listings use `WriteTable`, and the app name falls back to the assembly name (#167)

### Fixed
- `[GroupOption]` string defaults are kept; kebab-case options bind; `AddLogging` lambda parameter names are kept (#167, #168)

## [3.0.0-beta.45] - 2026-02-04

### Fixed
- Options declared in another file of a partial endpoint class are recognized (#164, task 406)

## [3.0.0-beta.44] - 2026-01-30

### Added
- `{group} --help` and `{group} -h` list every command in the group, including multi-word groups (task 405)

## [3.0.0-beta.43] - 2026-01-29

### Fixed
- Nested `[NuruRouteGroup]` inheritance joins all prefixes (#160)
- Routes with no positional arguments no longer catch the built-in flags, while explicit overrides of those flags still work (task 403)

## [3.0.0-beta.42] - 2026-01-27

### Changed
- **BREAKING: `--capabilities` JSON structure**: output is hierarchical by route group. Grouped commands appear only in their group's `groups` array, and ungrouped commands stay in the top-level `commands` array. beta.56 replaced this structure.

### Added
- `GroupCapability` and the `groups` array in capabilities output
- An internal `GroupHierarchyBuilder` that builds the group tree from routes

### Fixed
- Route segment extraction no longer repeats the group prefix

## [3.0.0-beta.39] / [3.0.0-beta.40] / [3.0.0-beta.41] - 2026-01-26

### Fixed
- A missing optional typed option (int, Uri, enum, custom converter) skips the route in place of failing with `Invalid value '(missing)'` (#152). beta.39 and beta.40 were incomplete attempts. beta.41 has the full fix.

## [3.0.0-beta.38] - 2026-01-25

### Fixed
- Nullable value-type `[Option]` properties on `[NuruRoute]` endpoint classes (#149, #150). This beta was tagged `3.0.0-beta.38`, without the `v` prefix.

## [3.0.0-beta.37] - 2026-01-25

### Fixed
- A nullable value-type option (`int?`, `long?`) is `null` when omitted, not `0` (#146, #147)

## [3.0.0-beta.36] - 2026-01-24

### Added
- **`NURU_A001`**: the `[NuruRoute]` pattern must be a single literal or `""`

### Changed
- **BREAKING**: multi-word, parameter, or option patterns in `[NuruRoute]` are errors. Use `[NuruRouteGroup]`, `[Parameter]`, and `[Option]` instead.

## [3.0.0-beta.35] - 2026-01-22

### Added
- Handlers and `ConfigureServices` accept anonymous methods (`delegate { }`). `ConfigureServices` also accepts local function method groups.

## [3.0.0-beta.34] - 2026-01-22

### Fixed
- `ConfigureServices` accepts method group references
- `ILogger<T>` resolves transitively under `UseMicrosoftDependencyInjection()` (task 396)

## [3.0.0-beta.33] - 2026-01-22

### Fixed
- Each `NuruApp` in a multi-app assembly gets its own runtime DI container
- Under MS DI, the user's `ConfigureServices` delegate runs at runtime

## [3.0.0-beta.32] - 2026-01-22

### Added
- Source-gen DI diagnostics `NURU050`–`NURU054` for unsupported registration patterns. They are skipped under `.UseMicrosoftDependencyInjection()`.

## [3.0.0-beta.31] - 2026-01-22

### Added
- `UseMicrosoftDependencyInjection()` turns on the runtime MS DI container

### Fixed
- `--help` and `--capabilities` show discovered endpoints
- Source-gen DI and runtime DI work together in the same assembly

## [3.0.0-beta.30] - 2026-01-21

### Fixed
- `timewarp-nuru-build` is in the solution and CI build list, so the package includes `TimeWarp.Nuru.Build.dll` (task 389)

## [3.0.0-beta.29] - 2026-01-21

### Fixed
- First attempt to restore the MSBuild task `.dll` that beta.28 lacked (`MSB4062`) (task 389). The fix was completed in beta.30.

## [3.0.0-beta.28] - 2026-01-21

### Changed
- "Attributed routes" are renamed "endpoints" across code, the analyzer category, and the docs
- The MCP server no longer falls back to built-in documentation

## [3.0.0-beta.27] - 2026-01-21

### Changed
- MCP server: examples come from `examples.json`, and a shared `GitHubCacheService` handles GitHub caching. Fixed `CacheManagementTool` and the `GenerateHandler` output.

## [3.0.0-beta.26] - 2026-01-21

Same commit as beta.25 (`ac2ede19`). No code changes and no NuGet package. The GitHub release notes repeat the beta.27 MCP cleanup, which is not in this tag.

## [3.0.0-beta.25] - 2026-01-21

### Changed
- **BREAKING**: `NuruCoreApp` and `NuruApp` are now one `NuruApp`. `NuruCoreAppBuilder` became `NuruAppBuilder`, and `CreateBuilder(args)` became `CreateBuilder()`. `NuruAppOptions` and `NuruHostEnvironment` are removed (#133)
- **BREAKING**: shell completion is source-generated. Static completion is removed, and dynamic completion is now `EnableCompletion`.
- `NURU_DEBUG` diagnostics are hidden by default (task 385)

### Fixed
- Enum option parameter conversion (task 387)

## [3.0.0-beta.24] - 2026-01-19

### Added
- **Source-generator runtime**: route matching, invokers, help, and REPL support are generated at compile time. The core and extension packages are now one `TimeWarp.Nuru` package (task 360)
- `DiscoverEndpoints()` and `Map<T>()` for endpoints
- Per-command `--help` (task 356). The source generator emits per-app `--capabilities` output. The flag itself shipped in beta.22.
- `NURU_R003` reports unreachable routes (task 351)
- Built-in conversion for `Uri`, `FileInfo`, and `DirectoryInfo` (task 381)

### Fixed
- Custom type converters (task 382), enum parameters (task 372), typed catch-alls and repeated options (task 349), and default values for typed options
- User routes override the built-in `--version` and `--help` (task 357)
- `--interactive` is no longer captured by catch-all routes
- `ILogger<T>` uses the app `LoggerFactory` for OTLP export

## [3.0.0-beta.23] - 2025-12-23

### Added
- `ITerminal` is registered in DI automatically

### Changed
- `TimeWarp.Terminal` and `TimeWarp.Builder` moved to their own repositories and packages. `NuruTerminal` is renamed `TimeWarpTerminal`.
- NuGet packages are published with OIDC trusted publishing
- Updated TimeWarp.Jaribu 1.0.0-beta.8 and Microsoft.Extensions.Options.ConfigurationExtensions 10.0.1

## [3.0.0-beta.22] - 2025-12-22

beta.21 was not published. Changes from beta.20 to beta.22 are listed here.

### Added
- **`[NuruRoute]` endpoints**: a source generator for attribute-based routes, with `[Parameter]` and `[Option]` binding and message-type detection (`AsQuery`, `AsCommand`, `AsIdempotentCommand`)
- **Fluent route builder**: `Map(pattern).WithHandler(...).WithDescription(...)` built on `IBuilder<TParent>`. The same pattern is used by `TableBuilder`, `PanelBuilder`, `RuleBuilder`, and `KeyBindingBuilder`.
- **`--capabilities`**: prints machine-readable CLI capabilities for tool discovery (#157)
- Method group handlers for delegate routes
- Tables shrink to fit with `TruncateMode` for narrow terminals

### Changed
- **BREAKING**: removed the `Map(pattern, handler)` and `MapDefault` overloads. Use the fluent builder.
- **BREAKING**: removed `MapMultiple`. Register each pattern with `Map`.
- **BREAKING**: `RunReplAsync` returns `Task`, not `Task<int>`
- `TimeWarp.Builder` and `TimeWarp.Terminal` are versioned separately (1.0.0-beta.1)

## [3.0.0-beta.20] - 2025-12-12

### Added
- Colored `--help` output: commands, parameters, options, descriptions, and headers. Output is plain text when the terminal has no color support (task 144)
- Invoker support for `Task<int>` handlers

### Fixed
- Default routes no longer show blank entries or leading commas in help
- Help shows optional parameters as `[name]` and optional options as `[--flag]` (task 141)

## [3.0.0-beta.19] - 2025-12-10

### Added
- **--check-updates command**: Query GitHub releases to check for newer versions
- **Panel WordWrap**: New `WordWrap` property with ANSI-aware `WrapText` method in `AnsiStringUtils`

### Fixed
- Strip build metadata from version display to avoid duplicate hash
- Correct project reference casing in samples and documentation

## [3.0.0-beta.18] - 2025-12-09

### Added
- **Automatic `--version` route**: `CreateBuilder` now automatically registers a `--version` route displaying the assembly's informational version
- Consumers can override by registering their own `--version` route

## [3.0.0-beta.17] - 2025-12-09

### Fixed
- **NuruInvokerGenerator MapDefault detection**: Source generator now detects `MapDefault()` invocations when generating typed invokers, fixing runtime errors for delegate signatures only used by `MapDefault()`

## [3.0.0-beta.15] - 2025-12-08

### Added
- **TestTerminalContext**: AsyncLocal-based context enabling zero-config test isolation for parallel tests
- **NuruTestContext**: Ambient delegate pattern for integration testing unmodified runfiles via `NURU_TEST_MODE=true`

### Changed
- Simplified `RunAsync` execution flow with consolidated terminal resolution
- Enhanced testing documentation with manual and automated execution patterns

## [3.0.0-beta.14] - 2025-12-08

### Fixed
- **Default app name detection**: Help output now shows actual executable name instead of hardcoded 'nuru-app' using `AppNameDetector.GetEffectiveAppName()`

## [3.0.0-beta.13] - 2025-12-07

### Added
- **HelpOptions configuration**: Configure help output filtering to hide per-command help routes, REPL commands, and completion infrastructure
- **Clipboard improvements (REPL)**: PowerShell Core (`pwsh`) support on Linux, WSL clipboard integration, kill ring fallback for `Ctrl+V`

### Fixed
- HelpProvider correctly classifies single-dash options (`-i`) in Options section
- AddInteractiveRoute uses alias syntax (`--interactive,-i`) for single endpoint
- Register pre-built invokers for library routes (`help`, `exit`, `clear`, etc.)

## [3.0.0-beta.12] - 2025-12-02

### Fixed
- **Help route priority**: Auto-generated help routes now use `--help?` (optional), preventing them from outranking user routes with optional flags

## [3.0.0-beta.11] - 2025-12-02

### Added
- **OSC 8 Hyperlinks**: Clickable terminal URLs for supported terminals (Windows Terminal, VS Code, iTerm2, Konsole, GNOME Terminal 3.26+)
- String extension `Link()` chainable with colors: `"text".Link("https://...").Cyan().Bold()`
- Terminal methods: `WriteLink()`, `WriteLinkLine()`
- Terminal detection via `SupportsHyperlinks` property

## [3.0.0-beta.10] - 2025-12-02

### Changed
- Flatten namespaces across the codebase to `TimeWarp.Nuru`

### Fixed
- NURU_D001 analyzer properly detects missing `Mediator.SourceGenerator` by checking for generated `AddMediator` method
- InternalsVisibleTo script uses AssemblyName from csproj and includes benchmarks

## [3.0.0-beta.9] - 2025-12-01

### Changed
- Skip redundant test execution on release events
- Split CI/CD workflow into separate Build and Test steps with conditional execution

## [3.0.0-beta.8] - 2025-11-30

### Changed
- CI/CD pipeline now publishes all 8 NuGet packages in dependency order
- Standardized repository naming to kebab-case convention

## [3.0.0-beta.7] - 2025-11-30

### Added
- **Aspire Dashboard integration**: `IHostApplicationBuilder` implementation on `NuruAppBuilder`
- **TimeWarp.Nuru.Telemetry package**: `TelemetryBehavior` for OpenTelemetry support with OTLP exporter
- **New samples**: AspireHostOtel, AspireTelemetry, PipelineMiddleware, UnifiedMiddleware

### Changed
- Auto-flush telemetry in `NuruApp.RunAsync`

## [3.0.0-beta.6] - 2025-11-29

### Added
- **TelemetryBehavior**: Pipeline middleware creating Activity spans for all Mediator commands
- **UseTelemetry() extension**: Auto-configures OpenTelemetry with OTLP exporter

### Changed
- Split `NuruAppBuilder` into `NuruCoreAppBuilder` + `NuruAppBuilder` with covariant return types
- Extract `TimeWarp.Nuru.Core` package for shared functionality

### Breaking Changes
- `NuruAppBuilder` moved from `TimeWarp.Nuru.Core` to `TimeWarp.Nuru`
- `NuruCoreAppBuilder` is the new lightweight builder in `TimeWarp.Nuru.Core`
- `CreateSlimBuilder()` and `CreateEmptyBuilder()` now return `NuruCoreAppBuilder`

## [3.0.0-beta.5] - 2025-11-27

### Added
- **MCP testing examples**: `test-output-capture`, `test-colored-output`, `test-terminal-injection` for `ITerminal`/`IConsole` usage

## [3.0.0-beta.4] - 2025-11-27

### Changed
- Modernized `AnsiColorExtensions` to use C# 14 `extension(string text)` block syntax
- Added `WithStyle()` helper method for consistent color application

### Added
- Comprehensive documentation for `IConsole` and `ITerminal` interfaces
- Testing samples demonstrating output capture, colored output testing, and DI injection

### Breaking Changes
- Extension methods now use C# 14 syntax (requires .NET 10 preview SDK)

## [3.0.0-beta.3] - 2025-11-27

### Added
- **REPL Mode**: Interactive REPL with syntax highlighting and tab completion
- **PSReadLine-compatible key bindings**: Profiles for Default, Emacs, Vi, VSCode
- **Custom key binding support**: `ConfigureKeyBindings()` builder API
- **History persistence**: Security patterns to exclude sensitive commands
- **Shell Completion**: Full support for Bash, Zsh, Fish, and PowerShell
- **Static completion**: `--generate-completion <shell>`
- **Dynamic completion**: `EnableDynamicCompletion()` for real-time suggestions
- **Auto-install completion**: `--install-completion`
- **New packages**: TimeWarp.Nuru.Completion, TimeWarp.Nuru.Repl
- **IConsole/ITerminal abstractions**: Testable console operations
- **Custom type converter support**
- **6 new built-in type converters**: Guid, Uri, TimeSpan, DateOnly, TimeOnly, Version

### Changed
- ASP.NET Core-style builder API: `NuruApp.CreateBuilder()`
- `Map()` replaces `AddRoute()` for familiar ASP.NET Core patterns
- `MapDefault()` for default route handling

### Breaking Changes
- `AddRoute()` renamed to `Map()` - update your route registrations

## [3.0.0-beta.2] - 2025-11-26

### Added
- **ASP.NET Core-Style Builder API**: `NuruApp.CreateBuilder(args)` factory method
- **Three builder options**: `CreateBuilder()` (full-featured), `CreateSlimBuilder()` (lightweight), `CreateEmptyBuilder()` (total control)
- **Map() aliases**: `Map(pattern, handler)` alias for `AddRoute()`, `MapDefault(handler)` alias for `AddDefaultRoute()`

## [2.0.0] - 2025-08-04

### Added
- **Optional Parameter Support**: Route patterns now support optional parameters using `{param?}` syntax
  - Example: `deploy {env} {tag?}` - tag parameter is optional
  - Works with both sync and async route handlers
- **Nullable Type Support**: Full support for nullable value types in route parameters
  - Example: `sleep {seconds:int?}` - accepts nullable int
  - Automatic conversion handles both value presence and absence
- **Automatic Help Generation**: New `.AddAutoHelp()` method for automatic help route creation
  - Generates `--help` routes for all commands
  - Supports inline parameter descriptions: `{name|description}`
  - Supports option descriptions: `--option,-o|description`
- **Enhanced Route Pattern Syntax**: Improved parser with better description support
  - Descriptions can contain spaces when using pipe syntax
  - Short aliases for options using comma syntax

### Changed
- **Culture-Invariant Formatting**: All string formatting now uses `CultureInfo.InvariantCulture`
  - Ensures consistent behavior across different locales
  - Fixes CA1305 warnings
- **Public API Enhancements**: Made `EndpointCollection` accessible for advanced scenarios
  - Available in both `NuruApp` and `NuruAppBuilder`
  - Enables custom help implementations

### Fixed
- Fixed parameter binding for optional parameters
- Fixed route matching when optional parameters are omitted
- Fixed type conversion for nullable value types
- Fixed culture-sensitive string operations in RouteHelpProvider

### Internal Improvements
- Updated TypeConverterRegistry to handle `Nullable<T>` types
- Enhanced RoutePatternParser regex for better parameter parsing
- Improved error messages for type conversion failures
- Added comprehensive test coverage (44 integration tests)

## [2.0.0-beta.3] - Previous Beta
- Various beta improvements and bug fixes

## [1.0.0] - 2025-07-28
- Initial release
- Route-based CLI framework bringing web-style routing to command-line applications
- Support for both direct delegate and mediator pattern approaches
- Basic route pattern matching with parameters and options
- Type conversion for common types (int, double, bool, DateTime, etc.)
- Initial mediator support for DI scenarios (later migrated to martinothamar/Mediator)