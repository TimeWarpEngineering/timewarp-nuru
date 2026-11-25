# Changelog: 3.0.0-beta.24 to 3.0.0-beta.79 (2026)

Archived from [changelog.md](../../changelog.md), which keeps the unreleased changes and the current release. Newest first.

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
