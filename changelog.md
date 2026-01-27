# Changelog

All notable changes to TimeWarp.Nuru will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Changed
- **BREAKING: `--capabilities` JSON structure**: The capabilities endpoint now outputs hierarchical JSON reflecting route groups. Grouped commands appear only within their respective `groups` array (not duplicated at top level). Ungrouped commands remain in the top-level `commands` array.

### Added
- **GroupCapability class**: New `groups` array in capabilities JSON output containing nested groups with their commands
- **GroupHierarchyBuilder**: Internal utility for building hierarchical group structures from routes

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