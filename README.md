# TimeWarp.Nuru

A route-based CLI framework for .NET that brings web-style routing to command-line applications.

> **Nuru** means "light" in Swahili - illuminating the path to your commands.

## Features

- 🚀 **Route-based command resolution** - Define commands using familiar route patterns
- 🎯 **Type-safe parameter binding** - Automatic type conversion for command parameters
- 🔌 **Mediator pattern support** - Use command objects for complex scenarios
- 🌟 **Minimal API style** - Clean, simple syntax inspired by ASP.NET Core
- 📦 **Zero external CLI framework dependencies** - Standalone implementation
- 🛠️ **Extensible type conversion** - Add custom type converters
- 🎨 **Catch-all parameters** - Support pass-through scenarios

## Quick Start

```csharp
using TimeWarp.Nuru;

var builder = new AppBuilder();

// Simple command
builder.AddRoute("greet {name}", (string name) => 
    Console.WriteLine($"Hello, {name}!"));

// Command with options
builder.AddRoute("calc {x:int} {y:int} --operation {op}", (int x, int y, string op) =>
{
    var result = op switch
    {
        "add" => x + y,
        "multiply" => x * y,
        _ => 0
    };
    Console.WriteLine($"Result: {result}");
});

// Catch-all for pass-through
builder.AddRoute("docker {*args}", (string[] args) =>
    Console.WriteLine($"Would run: docker {string.Join(" ", args)}"));

var app = builder.Build();
return await app.RunAsync(args);
```

## Route Patterns

Nuru supports several route pattern types:

- **Literal segments**: `git status`
- **Parameters**: `greet {name}`
- **Typed parameters**: `calc {x:int} {y:int}`
- **Options**: `--verbose`, `--output {file}`
- **Catch-all**: `{*args}`

## Installation

```bash
dotnet add package TimeWarp.Nuru
```

## License

This project is licensed under the Unlicense - see the [LICENSE](LICENSE) file for details.