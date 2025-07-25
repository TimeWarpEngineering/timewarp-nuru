# Code Style and Conventions

## General C# Style
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled for cleaner code
- **Language Version**: Latest C# features
- **File Organization**: One type per file, named after the primary type

## Naming Conventions
- **Classes/Interfaces**: PascalCase (e.g., `RouteEndpoint`, `ITypeConverterRegistry`)
- **Methods**: PascalCase (e.g., `AddRoute`, `Build`)
- **Properties**: PascalCase with required/init modifiers where appropriate
- **Private Fields**: Underscore prefix with camelCase (e.g., `_services`, `_endpoints`)
- **Parameters/Variables**: camelCase

## Code Patterns
- **Builder Pattern**: Used for fluent API (e.g., `AppBuilder`)
- **Dependency Injection**: Services registered via `IServiceCollection`
- **Required Properties**: Using C# 11 `required` modifier for mandatory properties
- **XML Documentation**: Triple-slash comments for public APIs
- **Expression-bodied Members**: Used where appropriate for conciseness

## Architecture Patterns
- **Separation of Concerns**: Clear namespace/folder structure
  - Endpoints: Route endpoint definitions
  - Parsing: Route pattern parsing logic
  - TypeConversion: Type converter system
  - ParameterBinding: Parameter binding logic
  - CommandResolver: Command resolution and execution
- **Interface-based Design**: Key services have interfaces (e.g., `ITypeConverterRegistry`)

## Analyzer Settings
- **Warning Level**: 5 (highest)
- **Analysis Mode**: All
- **Analysis Level**: latest-all
- **.NET Analyzers**: Enabled
- **Treat Warnings as Errors**: Currently disabled
- **Enforce Code Style in Build**: Enabled