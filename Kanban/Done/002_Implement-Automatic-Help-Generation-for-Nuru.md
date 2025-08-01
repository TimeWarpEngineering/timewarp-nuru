# ✅ COMPLETED: Implement Automatic Per-Command Help Generation for Nuru

**Completed:** 2025-01-29

## Implementation Summary

We successfully implemented automatic help generation using a runtime approach with the following features:

1. **AddAutoHelp() Method**: Added to NuruAppBuilder to enable automatic help generation at build time
2. **Parameter/Option Descriptions**: Implemented inline descriptions using pipe syntax:
   - Parameters: `{name|description}`
   - Options: `--option,-s|description`
3. **Public Endpoints**: Made EndpointCollection public in both NuruApp and NuruAppBuilder
4. **Enhanced Parser**: Updated RoutePatternParser to handle descriptions with spaces
5. **Automatic Route Generation**: Help routes are automatically created for:
   - Base `--help` showing all commands
   - Per-command help like `deploy --help`

## Actual Implementation (Different from Original Plan)

## Description

Enhance Nuru's existing help functionality to automatically generate per-command help messages (e.g., `app hello --help`), similar to Cocona's automatic help generation. This would eliminate the need for manual help route definitions while preserving developer control.

## Current State

Nuru already has:
- `RouteHelpProvider` class that displays all registered routes
- `ShowAvailableCommands()` in DirectApp that shows commands when no match found
- Route descriptions via `AddRoute(pattern, handler, description)`

What's missing:
- Automatic `--help` routes for individual commands
- Detailed parameter information (types, descriptions, required/optional)
- Cocona-style per-command help output

## Requirements

- Automatically generate help text from route patterns
- Support both arguments and options with descriptions
- Integrate seamlessly with existing DirectAppBuilder and AppBuilder APIs
- Maintain Nuru's performance-first philosophy
- Support custom help text overrides when needed

## Implementation Suggestions

### Option 1: Source Generator Approach (Compile-Time)

**Pros:**
- Zero runtime overhead
- Full type information available
- Can generate optimized help strings

**Cons:**
- More complex implementation
- Requires analyzer/generator infrastructure

**Implementation:**
1. Create `Nuru.Generators` package with source generator
2. Analyze `AddRoute` calls during compilation
3. Parse route patterns to extract:
   - Command name
   - Arguments (positional parameters in `{}`)
   - Options (parameters with `--` prefix)
   - Parameter types
4. Generate partial class with help methods
5. Auto-inject help routes during build

Example generated code:
```csharp
public static partial class GeneratedHelp
{
    public static void AddGeneratedHelpRoutes(this DirectAppBuilder builder)
    {
        builder.AddRoute("hello --help", () => Console.WriteLine(HelloHelpText));
    }
    
    private const string HelloHelpText = @"Usage: app hello <name> [options]

Arguments:
  name (string)  Required positional argument

Options:
  --to-upper-case (bool)  Optional flag";
}
```

### Option 2: Runtime Reflection Approach

**Pros:**
- Simpler implementation
- Works with existing codebase
- Dynamic help generation

**Cons:**
- Small runtime overhead
- Limited compile-time validation

**Implementation:**
1. Extend `DirectAppBuilder` with `EnableAutoHelp()` method
2. Intercept route registration to build help metadata
3. Auto-register `--help` routes for each command
4. Use route pattern parser to extract parameter info

### Option 3: Hybrid Approach (Recommended)

Combine both approaches:
1. Use runtime parsing for basic help generation
2. Allow attribute-based descriptions for enhanced help
3. Optional source generator for zero-overhead scenarios

Example API:
```csharp
var app = new DirectAppBuilder()
    .EnableAutoHelp() // Enables automatic help generation
    .AddRoute("hello {name:string} --to-upper-case {toUpperCase:bool}", 
        (string name, bool toUpperCase) => { /* ... */ })
    .WithDescription("Greets a person by name") // Optional
    .WithParameter("name", "The name of the person to greet") // Optional
    .WithParameter("toUpperCase", "Convert name to uppercase") // Optional
    .Build();
```

## Technical Details

### Route Pattern Parser Enhancement
Extend existing `RoutePatternParser` to extract:
- Parameter names
- Parameter types
- Required vs optional status
- Default values

### Help Format Template
```
Usage: <app> <command> <required-args> [optional-args] [options]

<description>

Arguments:
  <name> (<type>)  <description>  [Required|Optional]

Options:
  --<name> (<type>)  <description>  [Default: <value>]
```

### Integration Points
1. `DirectAppBuilder.EnableAutoHelp()`
2. `AppBuilder.EnableAutoHelp()` for Mediator pattern
3. Optional `IHelpFormatter` interface for customization
4. `HelpAttribute` for additional metadata

## Checklist

### Research & Design
- [x] Analyze Cocona's help generation implementation
- [x] Design API surface for Nuru help generation
- [x] Decide on source generator vs runtime approach (chose runtime)
- [x] Create proof of concept

### Implementation
- [x] Extend RoutePatternParser to extract help metadata
- [x] Implement help text generation logic
- [x] Add EnableAutoHelp() extension method (as AddAutoHelp())
- [x] Create help route auto-registration
- [x] Add parameter description attributes (inline syntax instead)
- [x] Use existing RouteHelpProvider (instead of new IHelpFormatter)

### Testing
- [x] Integration tests with various route patterns (test-desc.cs, test-auto-help.cs)
- [x] Test with sample applications
- [ ] Unit tests for help text generation (could be added later)
- [ ] Performance benchmarks (minimal impact expected)

### Documentation
- [x] Update API documentation (RoutePatternSyntax.md)
- [x] Add help generation examples
- [x] Document inline description syntax
- [ ] Migration guide from manual help (could be added later)

## Notes

- Consider compatibility with existing manual help routes
- Ensure help generation doesn't impact startup performance
- Support localization for help text in future iteration
- Could leverage similar patterns for generating shell completions
- Help format should be consistent with CLI conventions (similar to `--help` in Unix tools)