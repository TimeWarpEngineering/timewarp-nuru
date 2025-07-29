# Implement Automatic Per-Command Help Generation for Nuru

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
- [ ] Analyze Cocona's help generation implementation
- [ ] Design API surface for Nuru help generation
- [ ] Decide on source generator vs runtime approach
- [ ] Create proof of concept

### Implementation
- [ ] Extend RoutePatternParser to extract help metadata
- [ ] Implement help text generation logic
- [ ] Add EnableAutoHelp() extension method
- [ ] Create help route auto-registration
- [ ] Add parameter description attributes (optional)
- [ ] Implement IHelpFormatter interface

### Testing
- [ ] Unit tests for help text generation
- [ ] Integration tests with various route patterns
- [ ] Performance benchmarks
- [ ] Test with all sample applications

### Documentation
- [ ] Update API documentation
- [ ] Add help generation examples
- [ ] Migration guide from manual help
- [ ] Performance impact analysis

## Notes

- Consider compatibility with existing manual help routes
- Ensure help generation doesn't impact startup performance
- Support localization for help text in future iteration
- Could leverage similar patterns for generating shell completions
- Help format should be consistent with CLI conventions (similar to `--help` in Unix tools)