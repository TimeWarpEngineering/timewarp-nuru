# TimeWarp.Nuru Project Overview

TimeWarp.Nuru is a .NET 9.0 CLI routing framework that brings web-style routing patterns to command-line applications.

## Key Features
- Pattern-based routing similar to ASP.NET Core but for CLI applications
- Strong typing with comprehensive type conversion system
- Dependency injection support via Microsoft.Extensions.DependencyInjection
- Mediator pattern integration via TimeWarp.Mediator
- Extensible architecture with clear separation of concerns

## Technology Stack
- **Framework**: .NET 9.0
- **Language**: C# with latest features, nullable reference types enabled
- **Build System**: MSBuild with central package management
- **Code Quality**: Roslynator analyzers, analysis level "latest-all"

## Main Dependencies
- Microsoft.Extensions.DependencyInjection (8.0.0)
- TimeWarp.Mediator (13.0.0-beta.1)
- TimeWarp.Cli (0.6.0-rc12)

## Project Type
Console application library for building CLI tools with advanced routing capabilities.