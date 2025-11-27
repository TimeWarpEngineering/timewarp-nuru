# MinimalApp: Cocona vs Nuru Comparison

This document compares the implementation of a minimal CLI application between Cocona and Nuru frameworks.

## Overview

Both frameworks aim to create simple CLI applications with minimal boilerplate. The MinimalApp demonstrates:
- Basic command execution
- Argument handling
- Boolean option support

Nuru provides two approaches:
1. **Delegate-based** (`minimal-app`) - Direct lambda expressions for maximum performance
2. **Class-based with DI** (`minimal-app-di`) - Mediator pattern with dependency injection, closer to Cocona's model

## Side-by-Side Comparison

### Cocona Implementation

```csharp
CoconaApp.Run<Program>(args);

public void Hello(bool toUpperCase, [Argument]string name)
{
    Console.WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}");
}
```

### Nuru Implementation (Delegate)

```csharp
var app = new NuruAppBuilder()
    .Map("hello {name:string} --to-upper-case {toUpperCase:bool}", 
        (string name, bool toUpperCase) => 
        {
            WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}");
        },
        description: "Greets a person by name with optional uppercase")
    .AddAutoHelp()  // This automatically generates --help and hello --help routes
    .Build();

return await app.RunAsync(args);
```

### Nuru Implementation (Class-based with DI)

```csharp
var app = new NuruAppBuilder()
    .AddDependencyInjection(config => config.RegisterServicesFromAssembly(typeof(HelloCommand).Assembly))
    .Map<HelloCommand>("hello {name:string} --to-upper-case {toUpperCase:bool}")
    .AddAutoHelp()
    .Build();

return await app.RunAsync(args);

// Command class with nested handler
public sealed class HelloCommand : IRequest
{
    public string Name { get; set; } = string.Empty;
    public bool ToUpperCase { get; set; }
    
    internal sealed class Handler : IRequestHandler<HelloCommand>
    {
        public async Task Handle(HelloCommand request, CancellationToken cancellationToken)
        {
            WriteLine($"Hello {(request.ToUpperCase ? request.Name.ToUpper() : request.Name)}");
            await Task.CompletedTask;
        }
    }
}
```

## Key Differences

### 1. Programming Model
- **Cocona**: Class-based with method-to-command mapping
- **Nuru (Delegate)**: Route-based with lambda expressions
- **Nuru (Class-based)**: Mediator pattern with command classes and handlers

### 2. Entry Point
- **Cocona**: Uses `CoconaApp.Run<T>()` pattern requiring a class type
- **Nuru (Both)**: Uses fluent builder pattern with `NuruAppBuilder`
  - Delegate version: Direct route registration
  - Class-based: `.AddDependencyInjection()` with command registration

### 3. Command Definition
- **Cocona**: Commands are methods with attributes
  - `[Argument]` marks positional arguments
  - Method parameters automatically become options
- **Nuru (Delegate)**: Commands are routes with inline parameter definitions
  - Route pattern defines both arguments and options
  - Type information included in the route string
- **Nuru (Class-based)**: Commands are classes implementing `IRequest`
  - Properties map to parameters
  - Nested `Handler` class contains logic
  - Similar structure to Cocona's class-based approach

### 4. Option Syntax
- **Cocona**: Boolean parameter becomes `--to-upper-case` automatically through naming convention
- **Nuru**: Option syntax explicitly defined in route pattern

### 5. Default Behavior
- **Cocona**: Method signature determines required/optional parameters
- **Nuru**: Explicit routes for different parameter combinations

### 6. Help Generation
- **Cocona**: Automatically generates help for `--help` and `command --help` without any code
  - Framework automatically intercepts help flags
  - Generates help from method signatures and attributes
  - No opt-out without workarounds
- **Nuru**: Opt-in help with `.AddAutoHelp()` - a feature, not a limitation
  - Single method call generates all help routes when needed
  - Automatically creates `--help` and `command --help` patterns
  - **Key advantage**: Choose when to include help
    - Omit for maximum performance (no help routes generated)
    - Include with one line for full help support
    - Perfect for performance-critical or embedded scenarios
  - For any real-world CLI (git, docker, kubectl), one line is negligible

## Usage Examples

Both implementations support the same command-line interface:

```bash
# With arguments and options
app hello Alice --to-upper-case true
# Output: Hello ALICE

# Without uppercase option
app hello Bob --to-upper-case false
# Output: Hello Bob

# Help (automatically generated in Cocona, explicit route in Nuru)
app hello --help
# Output: Usage information...
```

## Architecture Insights

### Cocona Approach
- Object-oriented with method-to-command mapping
- Convention over configuration
- Reflection-based command discovery
- Implicit parameter binding
- Automatic help generation from method signatures

### Nuru Approach
- Functional with route-based commands
- Explicit route definitions
- Direct parameter mapping
- Lambda expressions for handlers
- Performance-focused with minimal overhead

## Performance Considerations

- **Cocona**: Class instantiation and reflection overhead
- **Nuru (Delegate)**: Direct routing with minimal overhead (~4KB memory footprint)
- **Nuru (Class-based)**: Mediator pattern overhead but still optimized

## Developer Experience

### Cocona
- Familiar to ASP.NET developers
- Less typing for simple cases (4 lines of actual code)
- Strong IDE support for method signatures
- Automatic parameter validation
- Help generation requires zero code

### Nuru
- More explicit and predictable
- Clear routing patterns
- Two approaches for different needs:
  - Delegate: Maximum performance, minimal code
  - Class-based: Better testability, DI support, familiar to enterprise developers
- Help generation with single `.AddAutoHelp()` call