# TimeWarp.Nuru Fluent DSL Samples

This directory contains samples demonstrating the **Fluent DSL** pattern - the primary API for defining routes using method chaining.

## What is Fluent DSL?

Fluent DSL uses a chainable API to define routes:

```csharp
NuruApp app = NuruApp.CreateBuilder()
  .Map("greet {name}")
    .WithHandler((string name) => $"Hello, {name}!")
    .WithDescription("Greet someone by name")
    .AsCommand()
    .Done()
  .Build();
```

## When to Use Fluent DSL

✅ **Best for:**
- Quick prototypes and scripts
- Single-file runfiles
- Simple command-line tools
- Learning Nuru basics

❌ **Consider Endpoints when:**
- Building large applications
- Need testable handlers
- Require dependency injection
- Want auto-discovery

## Samples Index

### 01-hello-world
| File | Description |
|------|-------------|
| `fluent-hello-world-lambda.cs` | Minimal example with inline lambda handler |
| `fluent-hello-world-method.cs` | Using method reference instead of lambda |

### 02-calculator
| File | Description |
|------|-------------|
| `fluent-calculator-delegate.cs` | Calculator with typed parameters and options |

### 03-syntax
| File | Description |
|------|-------------|
| `fluent-syntax-examples.cs` | Comprehensive syntax reference (literals, parameters, types, optional, catch-all, options) |

### 04-async
| File | Description |
|------|-------------|
| `fluent-async-examples.cs` | Async/await patterns with Task handlers |

### 05-pipeline
| File | Description |
|------|-------------|
| `fluent-pipeline-basic.cs` | Logging and performance behaviors |
| `fluent-pipeline-exception.cs` | Exception handling middleware |
| `fluent-pipeline-telemetry.cs` | OpenTelemetry Activity tracing |
| `fluent-pipeline-filtered-auth.cs` | Filtered behaviors with authorization |
| `fluent-pipeline-retry.cs` | Retry with exponential backoff |
| `fluent-pipeline-combined.cs` | Complete pipeline with all behaviors |

### 06-testing
| File | Description |
|------|-------------|
| `fluent-testing-output-capture.cs` | Capturing stdout/stderr with TestTerminal |
| `fluent-testing-colored-output.cs` | Testing styled/ANSI output |
| `fluent-testing-terminal-injection.cs` | ITerminal injection in handlers |

### 07-configuration
| File | Description |
|------|-------------|
| `fluent-configuration-basics.cs` | IOptions<T> parameter injection |
| `fluent-configuration-overrides.cs` | Command-line configuration overrides |
| `fluent-configuration-validation.cs` | Fail-fast validation with IValidateOptions<T> |

### 08-type-converters
| File | Description |
|------|-------------|
| `fluent-type-converters-builtin.cs` | All 15 built-in type converters |
| `fluent-type-converters-custom.cs` | Creating custom IRouteTypeConverter implementations |

### 09-logging
| File | Description |
|------|-------------|
| `fluent-logging-console.cs` | Microsoft.Extensions.Logging integration |

### 10-repl
| File | Description |
|------|-------------|
| `fluent-repl-basic.cs` | Interactive REPL mode with dual CLI/REPL support |

### 11-completion
| File | Description |
|------|-------------|
| `fluent-completion.cs` | Shell tab completion and custom completion sources |

### 12-runtime-di
| File | Description |
|------|-------------|
| `fluent-runtime-di.cs` | UseMicrosoftDependencyInjection() for complex DI |

## Common Patterns

### Basic Route
```csharp
.Map("status")
  .WithHandler(() => "OK")
  .AsQuery()
  .Done()
```

### With Parameters
```csharp
.Map("greet {name}")
  .WithHandler((string name) => $"Hello, {name}!")
  .AsCommand()
  .Done()
```

### With Options
```csharp
.Map("deploy {env} --dry-run")
  .WithHandler((string env, bool dryRun) => { ... })
  .AsCommand()
  .Done()
```

### Async Handler
```csharp
.Map("fetch {url}")
  .WithHandler(async (string url) => 
  {
    await Task.Delay(100);
    Console.WriteLine($"Fetched {url}");
  })
  .AsQuery()
  .Done()
```

### With Pipeline Behaviors
```csharp
NuruApp app = NuruApp.CreateBuilder()
  .AddBehavior(typeof(LoggingBehavior))
  .AddBehavior(typeof(PerformanceBehavior))
  .Map("status")
    .WithHandler(() => "OK")
    .AsQuery()
    .Done()
  .Build();
```

## Running Samples

All samples are runfiles - execute them directly:

```bash
# Run a sample
dotnet run samples/fluent/01-hello-world/fluent-hello-world-lambda.cs

# With arguments
dotnet run samples/fluent/02-calculator/fluent-calculator-delegate.cs -- add 5 3
```

## Related

- `../endpoints/` - Endpoint DSL samples (attribute-based)
- `../hybrid/` - Combining Fluent and Endpoint DSL
