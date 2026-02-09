# Endpoint DSL Samples ⭐ RECOMMENDED

> **Class-based, attribute-driven CLI development**

The **Endpoint DSL** is the recommended approach for most applications. It provides:
- ✅ **Testability** - Handlers are testable classes
- ✅ **Dependency Injection** - Constructor injection support
- ✅ **Clean Architecture** - Separation of concerns
- ✅ **Discoverability** - Auto-discovery of [NuruRoute] classes
- ✅ **Type Safety** - Compile-time route validation

## Quick Example

```csharp
[NuruRoute("greet {name}", Description = "Greet someone")]
public sealed class GreetCommand : IQuery<Unit>
{
  [Parameter(Description = "Name to greet")]
  public string Name { get; set; } = "";

  public sealed class Handler(ITerminal Terminal) : IQueryHandler<GreetCommand, Unit>
  {
    public ValueTask<Unit> Handle(GreetCommand c, CancellationToken ct)
    {
      Terminal.WriteLine($"Hello, {c.Name}!");
      return default;
    }
  }
}

// Main program
NuruApp app = NuruApp.CreateBuilder()
  .DiscoverEndpoints()
  .Build();

await app.RunAsync(args);
```

## Learning Path

### Beginner

| Sample | Description | Run |
|--------|-------------|-----|
| `endpoint-hello-world.cs` | Simplest Endpoint example | `dotnet run endpoint-hello-world.cs` |
| `endpoint-calculator.cs` | Full calculator with DI | `dotnet run endpoint-calculator.cs` |

### Intermediate

| Sample | Description | Topics |
|--------|-------------|--------|
| `endpoint-syntax-examples.cs` | All route patterns | parameters, options, catch-all |
| `endpoint-async-examples.cs` | Async handlers | ValueTask, CancellationToken |
| `endpoint-pipeline-basic.cs` | Pipeline behaviors | INuruBehavior |
| `endpoint-testing-output-capture.cs` | Testing patterns | TestTerminal, assertions |

### Advanced

| Sample | Description | Topics |
|--------|-------------|--------|
| `endpoint-pipeline-combined.cs` | Complete pipeline | telemetry, auth, retry |
| `endpoint-configuration-validation.cs` | Config validation | DataAnnotations |
| `endpoint-runtime-di-advanced.cs` | Complex DI | decorators, factories |
| `endpoint-repl-options.cs` | REPL customization | prompts, history |

## Sample Index

### Core Concepts

| Folder | Files | Description |
|--------|-------|-------------|
| `01-hello-world/` | 1 | Getting started |
| `02-calculator/` | 1 + messages/ | Full-featured calculator |
| `03-syntax/` | 1 | Route pattern reference |
| `04-async/` | 1 | Async/await patterns |

### Pipeline & Middleware

| Folder | Files | Description |
|--------|-------|-------------|
| `05-pipeline/` | 6 files | All pipeline behavior patterns |
| | `endpoint-pipeline-basic.cs` | Logging + Performance |
| | `endpoint-pipeline-exception.cs` | Error handling |
| | `endpoint-pipeline-telemetry.cs` | OpenTelemetry |
| | `endpoint-pipeline-filtered-auth.cs` | Authorization |
| | `endpoint-pipeline-retry.cs` | Resilience |
| | `endpoint-pipeline-combined.cs` | Complete reference |

### Testing

| Folder | Files | Description |
|--------|-------|-------------|
| `06-testing/` | 3 files | Test patterns |
| | `endpoint-testing-output-capture.cs` | stdout/stderr capture |
| | `endpoint-testing-colored-output.cs` | ANSI code testing |
| | `endpoint-testing-terminal-injection.cs` | ITerminal DI |

### Configuration

| Folder | Files | Description |
|--------|-------|-------------|
| `07-configuration/` | 4 files | Config patterns |
| | `endpoint-configuration-basics.cs` | IOptions<T> |
| | `endpoint-configuration-overrides.cs` | CLI overrides |
| | `endpoint-configuration-validation.cs` | Validation |
| | `endpoint-configuration-advanced.cs` | Collections/dictionaries |

### Type Converters

| Folder | Files | Description |
|--------|-------|-------------|
| `08-type-converters/` | 2 files | Parameter conversion |
| | `endpoint-type-converters-builtin.cs` | Built-in types |
| | `endpoint-type-converters-custom.cs` | Custom converters |

### REPL

| Folder | Files | Description |
|--------|-------|-------------|
| `09-repl/` | 4 files | Interactive mode |
| | `endpoint-repl-basic.cs` | Getting started |
| | `endpoint-repl-custom-keys.cs` | Key bindings |
| | `endpoint-repl-options.cs` | Full customization |
| | `endpoint-repl-dual-mode.cs` | CLI + REPL |

### Logging

| Folder | Files | Description |
|--------|-------|-------------|
| `10-logging/` | 2 files | Logging integration |
| | `endpoint-logging-console.cs` | MS.Extensions.Logging |
| | `endpoint-logging-serilog.cs` | Serilog structured |

### Advanced

| Folder | Files | Description |
|--------|-------|-------------|
| `11-discovery/` | 1 + messages/ | Auto-discovery |
| `12-completion/` | 1 | Shell completion |
| `13-runtime-di/` | 2 files | Runtime DI |

## When to Use Endpoint DSL

Use Endpoint DSL when you need:

1. **Unit Testing** - Handlers are easily testable
2. **Dependency Injection** - Full DI container support
3. **Complex Logic** - Separation of concerns with handlers
4. **Team Development** - Clear, consistent structure
5. **Long-term Maintenance** - Self-documenting code

## Key Attributes

### Route Definition
- `[NuruRoute("pattern")]` - Define route pattern
- `[NuruRouteGroup("prefix")]` - Group prefix (on base class)
- `[NuruRouteAlias("alias")]` - Alternative route name

### Parameters
- `[Parameter]` - Route parameter
- `[Option("name", "alias")]` - Command-line option
- `[ConfigurationKey("section")]` - Config section override

### Route Type Converters
- `[RouteTypeConverter(typeof(MyConverter))]` - Custom converter

## Running Samples

```bash
# Run a sample
dotnet run samples/endpoints/01-hello-world/endpoint-hello-world.cs

# With arguments
dotnet run samples/endpoints/02-calculator/endpoint-calculator.cs -- add 5 3

# Show help
dotnet run samples/endpoints/02-calculator/endpoint-calculator.cs -- --help
```

## Comparison with Fluent DSL

| Aspect | Endpoint DSL | Fluent DSL |
|--------|--------------|------------|
| Testability | ⭐⭐⭐ Excellent | ⭐⭐ Limited |
| DI Support | ⭐⭐⭐ Full | ⭐⭐ Basic |
| Boilerplate | ⭐⭐ More | ⭐⭐⭐ Minimal |
| Performance | ⭐⭐⭐ Fast | ⭐⭐⭐ Fastest |
| Learning Curve | ⭐⭐ Moderate | ⭐⭐⭐ Easy |
| Team Scale | ⭐⭐⭐ Best | ⭐⭐ Good |

## See Also

- [Fluent DSL](../fluent/) - Alternative paradigm
- [Hybrid Patterns](../hybrid/) - When to mix (rarely)
- [Root Samples README](../) - Overview
