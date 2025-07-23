# Codebase Structure

## Root Directory
- `TimeWarp.Nuru.slnx` - Solution file
- `Directory.Build.props` - Global MSBuild properties
- `Directory.Packages.props` - Central package version management
- `README.md` - Project documentation
- `LICENSE` - Unlicense
- `.gitignore` - Git ignore rules

## Source/TimeWarp.Nuru/ (Main Library)
Core library implementation with clear architectural separation:

### Key Components
- `AppBuilder.cs` - Fluent API for building CLI apps
- `NuruCli.cs` - Main CLI entry point
- `ServiceCollectionExtensions.cs` - DI registration
- `GlobalUsings.cs` - Global using statements

### Subdirectories
- **Endpoints/** - Route endpoint definitions
  - `RouteEndpoint.cs` - Core endpoint model
  - `EndpointCollection.cs` - Endpoint management
  - `IEndpointCollectionBuilder.cs` - Builder interface
  
- **Parsing/** - Route pattern parsing
  - `RoutePatternParser.cs` - Main parser
  - `ParsedRoute.cs` - Parsed route model
  - **Segments/** - Route segment types
    - `RouteSegment.cs` - Base segment
    - `LiteralSegment.cs` - Literal text
    - `ParameterSegment.cs` - Parameters
    - `OptionSegment.cs` - CLI options

- **TypeConversion/** - Type conversion system
  - `ITypeConverterRegistry.cs` - Registry interface
  - `TypeConverterRegistry.cs` - Registry implementation
  - **Converters/** - Built-in converters
    - Int, Long, Bool, Double, Decimal, Guid, DateTime, TimeSpan

- **ParameterBinding/** - Parameter binding logic
  - `DelegateParameterBinder.cs` - Binds values to delegate parameters

- **CommandResolver/** - Command resolution
  - `RouteBasedCommandResolver.cs` - Route matching
  - `CommandExecutor.cs` - Command execution
  - `ResolverResult.cs` - Resolution results

- **Help/** - Help system
  - `RouteHelpProvider.cs` - Help text generation

## Samples/
- **TimeWarp.Nuru.Sample/** - Example usage
- **TimeWarp.Nuru.IntegrationTests/** - Integration test suite
  - `test-all.sh` - Comprehensive test script

## Scripts/
- `Build.cs` - Executable C# build script
- `Directory.Build.props` - Script-specific MSBuild settings

## .github/workflows/
- `ci-cd.yml` - GitHub Actions for CI/CD and NuGet publishing

## Assets/
- Package assets (logo, etc.)