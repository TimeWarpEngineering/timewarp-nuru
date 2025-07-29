# MinimalApp: Cocona vs Nuru Comparison

This document compares the implementation of a minimal CLI application between Cocona and Nuru frameworks.

## Overview

Both frameworks aim to create simple CLI applications with minimal boilerplate. The MinimalApp demonstrates:
- Basic command execution
- Argument handling
- Boolean option support

## Side-by-Side Comparison

### Cocona Implementation

```csharp
CoconaApp.Run<Program>(args);

public void Hello(bool toUpperCase, [Argument]string name)
{
    Console.WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}");
}
```

### Nuru Implementation

```csharp
var app = new NuruAppBuilder()
    .AddRoute("hello {name:string} --to-upper-case {toUpperCase:bool}", 
        (string name, bool toUpperCase) => 
        {
            Console.WriteLine($"Hello {(toUpperCase ? name.ToUpper() : name)}");
        })
    
    // Help command
    .AddRoute("hello --help", 
        () => Console.WriteLine(
            "Usage: minimal-app hello <name> [options]\n" +
            "\n" +
            "Arguments:\n" +
            "  name  The name to greet\n" +
            "\n" +
            "Options:\n" +
            "  --to-upper-case  Convert name to uppercase"))
    
    .Build();

return await app.RunAsync(args);
```

## Key Differences

### 1. Programming Model
- **Cocona**: Class-based with method-to-command mapping
- **Nuru**: Route-based with lambda expressions

### 2. Entry Point
- **Cocona**: Uses `CoconaApp.Run<T>()` pattern requiring a class type
- **Nuru**: Uses fluent builder pattern with `NuruAppBuilder`

### 3. Command Definition
- **Cocona**: Commands are methods with attributes
  - `[Argument]` marks positional arguments
  - Method parameters automatically become options
- **Nuru**: Commands are routes with inline parameter definitions
  - Route pattern defines both arguments and options
  - Type information included in the route string

### 4. Option Syntax
- **Cocona**: Boolean parameter becomes `--to-upper-case` automatically through naming convention
- **Nuru**: Option syntax explicitly defined in route pattern

### 5. Default Behavior
- **Cocona**: Method signature determines required/optional parameters
- **Nuru**: Explicit routes for different parameter combinations

### 6. Help Generation
- **Cocona**: Automatically generates help from method signatures and attributes
- **Nuru**: Requires explicit help route definition

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
- **Nuru**: Direct routing with minimal overhead

## Developer Experience

### Cocona
- Familiar to ASP.NET developers
- Less typing for simple cases
- Strong IDE support for method signatures
- Automatic parameter validation

### Nuru
- More explicit and predictable
- Clear routing patterns
- Better for performance-critical scenarios
- Flexible handler implementation