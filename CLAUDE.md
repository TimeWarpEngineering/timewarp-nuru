@Agents.md
# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Overview

TimeWarp.Nuru is a route-based CLI framework for .NET that brings web-style routing to command-line applications. It supports both direct delegate routing (for maximum performance) and mediator pattern routing (for enterprise patterns with DI).

## Common Development Tasks

### Build Commands

```bash
# Build the entire solution
dotnet build TimeWarp.Nuru.slnx -c Release

# Build with code formatting check
cd Scripts && ./Build.cs

# Clean and rebuild
cd Scripts && ./CleanAndBuild.cs

# Run code analysis
cd Scripts && ./Analyze.cs
```

### Test Commands

```bash
# Run integration tests comparing Delegate vs Mediator implementations (both JIT and AOT)
cd Tests && ./test-both-versions.sh

# Run benchmarks
cd Benchmarks/TimeWarp.Nuru.Benchmarks
dotnet run -c Release
```

### Single Test Execution

```bash
# Test specific delegate implementation
./Tests/TimeWarp.Nuru.TestApp.Delegates/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Delegates git status

# Test specific mediator implementation  
./Tests/TimeWarp.Nuru.TestApp.Mediator/bin/Release/net9.0/TimeWarp.Nuru.TestApp.Mediator git status
```

## Architecture

### Core Components

- **NuruApp**: Main application class that handles routing and execution
- **NuruAppBuilder**: Fluent builder for configuring routes and services
- **RouteSegment/ParameterSegment/LiteralSegment**: Route parsing components
- **TypeConverters**: Handle parameter type conversion (int, double, DateTime, etc.)
- **DelegateParameterBinder**: Binds parsed arguments to delegate parameters

### Route Pattern Support

- Literal routes: `status`, `version`
- Parameters: `greet {name}`, `delay {ms:int}`
- Optional parameters: `deploy {env} {tag?}`
- Options: `build --config {mode}`, `git commit -m {message}`
- Catch-all: `docker {*args}`

### Two Routing Approaches

1. **Direct Delegates** (Fast, minimal overhead)
   - No DI container required
   - ~4KB memory footprint
   - Best for simple commands

2. **Mediator Pattern** (Enterprise patterns)
   - Full DI support via Microsoft.Extensions.DependencyInjection
   - TimeWarp.Mediator integration
   - Best for complex business logic

## Project Structure

```
/Source/TimeWarp.Nuru/          # Main library
/Tests/
  TimeWarp.Nuru.TestApp.Delegates/   # Direct routing test app
  TimeWarp.Nuru.TestApp.Mediator/    # Mediator routing test app
/Benchmarks/                    # Performance benchmarks
/Samples/                       # Example implementations
/Scripts/                       # Build and utility scripts
/Kanban/                       # Task tracking
```

## Key Development Guidelines

### Code Style
- Enforce code style with `dotnet format` (runs automatically in build script)
- Follow Roslynator rules configured in Directory.Build.props
- All warnings are treated as errors

# ⚠️ IMPORTANT: NO TRAILING WHITESPACE ⚠️
### DO NOT ADD TRAILING WHITESPACE AT THE END OF LINES
### DO NOT ADD EXTRA BLANK LINES WITH SPACES
### CLEAN UP ANY WHITESPACE AFTER CODE CHANGES
### THE BUILD WILL FAIL WITH RCS1037 IF YOU LEAVE TRAILING SPACES

### Testing
- Integration tests validate both Direct and Mediator approaches
- Test script runs 37 complex CLI scenarios for each implementation
- AOT compilation is tested for both approaches

### Performance Considerations
- Direct approach: Optimized for minimal allocations (3,992 B)
- Mediator approach: Acceptable overhead for DI benefits
- AOT support requires TrimMode=partial for Mediator approach

### Current Work Focus
The Kanban board tracks recreation of Cocona samples to demonstrate feature parity and migration paths. Each sample includes an Overview.md contrasting Cocona vs Nuru implementations.

## Building AOT Executables

```bash
# Direct approach (full AOT)
dotnet publish -c Release -r linux-x64 -p:PublishAot=true

# Mediator approach (partial trim for reflection)
dotnet publish -c Release -r linux-x64 -p:PublishAot=true -p:TrimMode=partial
```

## Kanban Task Guidelines

### ⚠️ CRITICAL: NEVER add these fields to Kanban tasks:
- **Status** - The folder location (ToDo/InProgress/Done/Backlog) indicates status
- **Priority** - We don't use priority rankings
- **Category** - Unnecessary classification
- **Priority Justification** - Not needed
- **Implementation Status** or any temporal status indicators

**WHY:** Status is determined by folder location. Adding status fields creates redundancy and confusion.

### Use ONLY the fields from Task-Template.md:
- Description
- Parent (optional)
- Requirements (optional)
- Checklist (optional)
- Notes (optional)
- Implementation Notes (optional)

## Important Notes
- The repository uses .NET 9.0 and C# latest features
- Central package management via Directory.Packages.props
- Local NuGet cache configured in Directory.Build.props
- Supports .NET 10 script mode (see README examples)
- Claude cannot run interactive REPL tests due to non-interactive shell environment
- All REPL functionality tests must be run by human user in interactive shell

## Testing Approach
- **IMPORTANT: This repository does NOT use xUnit, NUnit, MSTest or any traditional testing frameworks.**

Tests are implemented as single-file C# applications (new in .NET 10). You can find 50+ test files by searching for "test" in file names. The analyzer tests are located in `/Tests/TimeWarp.Nuru.Analyzers.Tests/TimeWarp.Nuru.Analyzers.Tests.csproj`.

## REPL Interactive Testing Limitations
- **IMPORTANT: Claude cannot run interactive REPL tests due to non-interactive shell environment.**
- All REPL functionality tests must be run by human user in interactive shell
- Claude can verify compilation and basic functionality, but cannot test interactive features like arrow key navigation, command execution, etc.
- When REPL testing is needed, ask the human user to run samples interactively and report results

## Cocona Comparison Documentation

When working with Cocona comparison documents in `/Samples/CoconaComparison/`:
- Follow the template structure defined in `CoconaComparisonTemplate.md`
- Check `CoconaComparisonUpdateTracking.md` for documents needing updates
- Ensure all comparison documents maintain consistent structure for better developer experience

## REPL Implementation Status: COMPLETE ✅

**Task 027: REPL Map Implementation - FINISHED**

### What Was Accomplished:
- ✅ **Split Architecture**: `AddReplOptions()` in core `TimeWarp.Nuru`, `AddReplRoutes()` in REPL `TimeWarp.Nuru.Repl`
- ✅ **Map Usage**: All REPL commands (`exit`, `quit`, `q`, `help`, `history`, `clear`, `cls`, `clear-history`) registered via `builder.Map()`
- ✅ **Clean Separation**: Core configuration stays private, REPL routes in separate project
- ✅ **Updated Sample**: Modified `Samples/ReplDemo/repl-basic-demo.cs` to demonstrate new `AddReplSupport()` API
- ✅ **All Builds Successful**: Both core and REPL projects compile without errors

### Testing Limitations:
- **IMPORTANT**: Claude cannot run interactive REPL tests due to non-interactive shell environment
- All REPL functionality tests must be run by human user in interactive shell
- Implementation verified to start correctly and display custom prompts/messages

### Files Modified:
- `Source/TimeWarp.Nuru/NuruAppBuilder.cs` - Added `AddReplOptions()` method
- `Source/TimeWarp.Nuru.Repl/NuruAppExtensions.cs` - Added `AddReplRoutes()` and updated `AddReplSupport()`
- `Samples/ReplDemo/repl-basic-demo.cs` - Updated to use new API
- `CLAUDE.md` - Added testing limitations note

The REPL Map implementation is **complete and functional**.