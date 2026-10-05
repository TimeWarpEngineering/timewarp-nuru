# Changelog

All notable changes to TimeWarp.Nuru will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Changes since `3.0.0-beta.76`, the last published beta. beta.77 was never published.

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

## [3.0.0-beta.77] - not published

The version was set to `3.0.0-beta.77` on 2026-08-27 and moved to `3.0.0-beta.78` on 2026-09-25. No tag, GitHub release, or NuGet package was made for beta.77. Its changes are listed under Unreleased.

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

## [3.0.0-beta.71] - 2026-06-15

### Fixed
- Per-command `--help` shows `[Parameter(Description)]` text (#215, #218)

## [3.0.0-beta.70] - not documented

A `3.0.0-beta.70` package is on NuGet. It has no git tag and no GitHub release, so its contents cannot be confirmed. Changes from beta.69 to beta.71 are listed under beta.71.

## [3.0.0-beta.69] - 2026-04-27

### Changed
- `dev check-version` uses the NuGet.org V3 flat-container API through `HttpClient` in place of NuGet.Protocol, so it works under AOT. It defaults to the `nuget-search` strategy.
- `TimeWarp.Nuru.DevCli` gets `IRepoConfigService` (#187) and a source-generated JSON context for `.timewarp/dev.jsonc`
- Updated dependencies: TimeWarp.Amuru 1.0.0-beta.32 (no Newtonsoft.Json), Microsoft.Extensions 10.0.7

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
- Generated code uses `global::TimeWarp.Nuru.Unit` in place of a bare `Unit`, so it does not clash with other packages' `Unit` types (#442)

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
- Routes with no positional arguments no longer catch the built-in flags, while explicit overrides of those flags still work (#403)

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
- `ILogger<T>` resolves transitively under `UseMicrosoftDependencyInjection()` (#396)

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
- `timewarp-nuru-build` is in the solution and CI build list, so the package includes `TimeWarp.Nuru.Build.dll` (#389)

## [3.0.0-beta.29] - 2026-01-21

### Fixed
- First attempt to restore the MSBuild task `.dll` that beta.28 lacked (`MSB4062`) (#389). The fix was completed in beta.30.

## [3.0.0-beta.28] - 2026-01-21

### Changed
- "Attributed routes" are renamed "endpoints" across code, the analyzer category, and the docs
- The MCP server no longer falls back to built-in documentation

## [3.0.0-beta.26] / [3.0.0-beta.27] - 2026-01-21

### Changed
- MCP server: examples come from `examples.json`, and a shared `GitHubCacheService` handles GitHub caching. Fixed `CacheManagementTool` and the `GenerateHandler` output.

## [3.0.0-beta.25] - 2026-01-21

### Changed
- **BREAKING**: `NuruCoreApp` and `NuruApp` are now one `NuruApp`. `NuruCoreAppBuilder` became `NuruAppBuilder`, and `CreateBuilder(args)` became `CreateBuilder()`. `NuruAppOptions` and `NuruHostEnvironment` are removed (#133)
- **BREAKING**: shell completion is source-generated. Static completion is removed, and dynamic completion is now `EnableCompletion`.
- `NURU_DEBUG` diagnostics are hidden by default (#385)

### Fixed
- Enum option parameter conversion (#387)

## [3.0.0-beta.24] - 2026-01-19

### Added
- **Source-generator runtime**: route matching, invokers, help, and REPL support are generated at compile time. The core and extension packages are now one `TimeWarp.Nuru` package (#360)
- `DiscoverEndpoints()` and `Map<T>()` for endpoints
- Per-command `--help` (#356) and the `--capabilities` flag (#157)
- `NURU_R003` reports unreachable routes (#351)
- Built-in conversion for `Uri`, `FileInfo`, and `DirectoryInfo` (#381)

### Fixed
- Custom type converters (#382), enum parameters (#372), typed catch-alls and repeated options (#349), and default values for typed options
- User routes override the built-in `--version` and `--help` (#357)
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
- Method group handlers for delegate routes
- Tables shrink to fit with `TruncateMode` for narrow terminals

### Changed
- **BREAKING**: removed the `Map(pattern, handler)` and `MapDefault` overloads. Use the fluent builder.
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