# TimeWarp.Nuru Codebase Structure

## Source Directory (`/Source/TimeWarp.Nuru/`)

### Core Components
- `NuruCli.cs` - Main CLI entry point with command execution logic
- `AppBuilder.cs` - Application builder pattern implementation
- `ServiceCollectionExtensions.cs` - DI extension methods

### Routing System (`/Routing/`)
- Core routing functionality for CLI commands

### Parsing (`/Parsing/`)
- `RoutePatternParser.cs` - Main route pattern parser
- `ParsedRoute.cs` - Parsed route representation with parameters
- `/Segments/` - Route segment types:
  - `RouteSegment.cs` - Base abstract segment
  - `LiteralSegment.cs` - Literal text matching
  - `ParameterSegment.cs` - Parameter placeholders
  - `OptionSegment.cs` - Optional segments

### Endpoints (`/Endpoints/`)
- `IEndpointCollectionBuilder.cs` - Interface for building endpoints
- `DefaultEndpointCollectionBuilder.cs` - Default implementation
- `EndpointCollection.cs` - Collection of route endpoints
- `RouteEndpoint.cs` - Individual route endpoint

### Type Conversion (`/TypeConversion/`)
- `ITypeConverterRegistry.cs` & `TypeConverterRegistry.cs` - Registry pattern
- `IRouteTypeConverter.cs` - Converter interface
- `/Converters/` - Built-in type converters:
  - Basic: Int, Long, Bool, String
  - Numeric: Double, Decimal
  - Date/Time: DateTime, TimeSpan
  - Other: Guid

### Command Resolution (`/CommandResolver/`)
- `RouteBasedCommandResolver.cs` - Resolves commands from routes
- `CommandExecutor.cs` - Executes resolved commands
- `ResolverResult.cs` - Resolution result model

### Parameter Binding (`/ParameterBinding/`)
- `DelegateParameterBinder.cs` - Binds parameters to delegates

### Help System (`/Help/`)
- `RouteHelpProvider.cs` - Provides help for routes

## Scripts Directory
- `Build.cs`, `Clean.cs`, `CleanAndBuild.cs` - Build automation scripts
- `.github/workflows/` - GitHub Actions for syncing configurable files

## Samples Directory
- `TimeWarp.Nuru.Sample/` - Example usage project
- `TimeWarp.Nuru.IntegrationTests/` - Integration tests with test scripts

## Configuration Files
- `TimeWarp.Nuru.slnx` - Solution file
- `Directory.Build.props` - Common build properties
- `Directory.Packages.props` - Central package versions
- `.editorconfig` - Code style configuration