# TypicalSimpleApp: Cocona vs Nuru Comparison

This document compares the implementation of a typical simple CLI application between Cocona and Nuru frameworks.

## Overview

The TypicalSimpleApp represents the most common CLI pattern:
- Single command with description
- Basic argument handling
- Boolean option support with short alias
- Professional help system

This is essentially the MinimalApp with added command description, representing what most simple CLI tools look like.

Nuru provides two approaches:
1. **Delegate-based** (`typical-simple-app`) - Direct lambda expressions for simplicity
2. **Class-based with DI** (`typical-simple-app-di`) - Enterprise patterns with dependency injection

## Side-by-Side Comparison

### Cocona Implementation

```csharp
class Program
{
    static void Main(string[] args)
    {
        CoconaApp.Run<Program>(args);
    }

    [Command(Description = "This is a sample application")]
    public void Hello([Option('u', Description = "Print a name converted to upper-case.")]bool toUpperCase, 
                      [Argument(Description = "Your name")]string name)
    {
        Console.WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}!");
    }
}
```

### Nuru Implementation (Delegate)

```csharp
var app = new NuruAppBuilder()
    .AddRoute("hello {name|Your name} --to-upper-case,-u|Print a name converted to upper-case {toUpperCase:bool}", 
        (string name, bool toUpperCase) => 
        {
            WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}!");
        },
        description: "This is a sample application")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);
```

### Nuru Implementation (Class-based with DI)

```csharp
var app = new NuruAppBuilder()
    .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(HelloCommand).Assembly))
    .AddRoute<HelloCommand>("hello {name|Your name} --to-upper-case,-u|Print a name converted to upper-case {toUpperCase:bool}")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);

public class HelloCommand : IRequest
{
    public string Name { get; set; } = string.Empty;
    public bool ToUpperCase { get; set; }
    
    public class Handler : IRequestHandler<HelloCommand>
    {
        public async Task Handle(HelloCommand request, CancellationToken cancellationToken)
        {
            WriteLine($"Hello {(request.ToUpperCase ? request.Name.ToUpper() : request.Name)}!");
        }
    }
}
```

## Key Differences

### 1. Command Description
- **Cocona**: `[Command(Description = "...")]` attribute on method
- **Nuru (Both)**: `description` parameter in route registration

### 2. Code Structure
- **Cocona**: Traditional class with attributed method
- **Nuru (Delegate)**: Functional approach with inline handler
- **Nuru (Class-based)**: Mediator pattern with separate command and handler

### 3. Professional Features
- **Cocona**: Help generation automatic
- **Nuru (Both)**: Explicit `.AddAutoHelp()` for professional CLI experience

## Usage Examples

All implementations provide identical CLI interface:

```bash
# Show help
./app --help

# Basic usage
./app hello Alice --to-upper-case true
./app hello Alice -u true

# Help output shows:
Usage: app hello <name> [options]

This is a sample application

Arguments:
  name     Your name

Options:
  -u, --to-upper-case    Print a name converted to upper-case
  --help                 Show help
```

## Typical Simple App Patterns

### What Makes It "Typical"

1. **Single focused command** - Most CLI tools do one thing well
2. **Clear descriptions** - Professional tools always have help text
3. **Short option aliases** - Common options get short forms (-u)
4. **Standard argument/option pattern** - Positional args + named options

### Best Practices Demonstrated

- Command has a clear description
- Arguments and options are documented
- Short aliases for common options
- Consistent naming conventions
- Professional help output

## Architecture Insights

### Cocona's Typical Pattern
- Class represents the application
- Method represents the command
- Attributes provide metadata
- Reflection handles execution

### Nuru's Typical Patterns
- **Delegate**: Perfect for simple tools, minimal overhead
- **Class-based**: Better for tools that might grow, need testing, or use DI

## When to Use Each Approach

### Use Cocona's Pattern When:
- Team is familiar with ASP.NET-style attributes
- You prefer convention over configuration
- Reflection overhead is acceptable

### Use Nuru Delegate Pattern When:
- Building simple, focused CLI tools
- Performance is important
- You prefer explicit, functional code

### Use Nuru Class-based Pattern When:
- Building tools that may grow in complexity
- Need dependency injection
- Want better testability
- Following enterprise patterns

## Migration Guide

To migrate a typical Cocona app to Nuru:

1. Choose your approach (delegate vs class-based)
2. Convert method to route pattern
3. Move descriptions to route registration
4. Add `.AddAutoHelp()` for help generation
5. Test to ensure identical behavior

The "typical" pattern is the sweet spot for most CLI applications - simple enough to build quickly, but with all the professional features users expect.