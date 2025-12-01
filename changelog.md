# Changelog

All notable changes to TimeWarp.Nuru will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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