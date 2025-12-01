# Fix Misleading BuilderMode.Full Comment

## Description

The `BuilderMode.Full` enum comment incorrectly states "all features enabled including DI, Mediator". This is misleading because `CreateBuilder()` does NOT automatically register Mediator. Users must always install Mediator packages and call `services.AddMediator()`.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Update the XML doc comment for `BuilderMode.Full` to accurately describe what is included
- Remove mention of "Mediator" from the description
- Clarify that Mediator requires explicit setup

## Checklist

### Implementation
- [x] Locate `BuilderMode.Full` enum in `source/timewarp-nuru-core/nuru-core-app-builder.factory.cs` (lines 21-24)
- [x] Update XML doc comment to remove incorrect "Mediator" mention
- [x] Add clarification about Mediator requiring explicit setup
- [x] Verify documentation builds correctly
- [x] Verify IntelliSense shows correct comment

### Additional changes
- [x] Also fixed `BuilderMode.Slim` comment which incorrectly mentioned Mediator

## Notes

**Current (WRONG):**
```csharp
/// <summary>
/// Full builder - all features enabled including DI, Mediator, REPL, Completion.
/// Best for complex applications that need enterprise patterns.
/// </summary>
Full
```

**Target (CORRECT):**
```csharp
/// <summary>
/// Full builder - DI, Configuration, and auto-help enabled.
/// For Mediator support, install Mediator packages and call services.AddMediator().
/// Best for complex applications that need enterprise patterns.
/// </summary>
Full
```

What `BuilderMode.Full` actually enables:
- `AddDependencyInjection()` - Sets up DI container
- `AddConfiguration(args)` - Sets up configuration
- `AddAutoHelp()` - Enables auto-help

Then `UseAllExtensions()` adds:
- Telemetry
- REPL support
- Dynamic completion
- Interactive route

Mediator is NOT included - users must install packages and call `services.AddMediator()`.

File to update:
- `source/timewarp-nuru-core/nuru-core-app-builder.factory.cs`

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
