# TimeWarp.Nuru Project Overview

## Purpose
TimeWarp.Nuru is a route-based CLI framework for .NET that brings web-style routing to command-line applications. The name "Nuru" means "light" in Swahili, symbolizing illuminating the path to commands.

## Key Features
- Route-based command resolution using familiar patterns
- Type-safe parameter binding with automatic type conversion
- Mediator pattern support for complex scenarios
- Minimal API style inspired by ASP.NET Core
- Zero external CLI framework dependencies
- Extensible type conversion system
- Catch-all parameters for pass-through scenarios

## Tech Stack
- **Language**: C# with latest language features
- **Target Framework**: .NET 9.0
- **Dependencies**: 
  - Microsoft.Extensions.DependencyInjection (8.0.0)
  - TimeWarp.Mediator (13.0.0-beta.1)
  - TimeWarp.Cli (0.6.0-rc12) - used in Scripts
- **Build System**: MSBuild with Central Package Management
- **CI/CD**: GitHub Actions for automated builds and NuGet publishing

## Project Type
This is a NuGet library package designed for creating CLI applications with route-based command handling.