# Fluent Route Builder Design

## Overview

This document describes the design for a fluent `CompiledRouteBuilder` API that provides an alternative to string-based route patterns. Both syntaxes ultimately compile down to the same runtime artifact: a `CompiledRoute` feeding into the unified Command/Handler pipeline.

## Design Goals

1. **Dual syntax support** - Consumers can use string patterns OR fluent builder
2. **Single runtime model** - Both compile to static `CompiledRoute` + Command/Handler
3. **Source generation** - No runtime parsing; everything resolved at compile time
4. **Unified pipeline** - All routes (delegate or command) flow through mediator pipeline

## The Full Architecture

```
                        CONSUMER WRITES (all equivalent)

  ┌────────────────────────┐  ┌─────────────────────────────┐  ┌────────────────────────┐
  │   String + Delegate    │  │     Fluent + Delegate       │  │   Command (Mediator)   │
  │                        │  │                             │  │                        │
  │ app.Map(               │  │ app.Map(r => r              │  │ app.Map<DeployCommand>(│
  │   "deploy {env}        │  │   .WithLiteral("deploy")    │  │   "deploy {env}        │
  │    --force",           │  │   .WithParameter("env")     │  │    --force");          │
  │   (env, force) =>      │  │   .WithOption("force"),     │  │                        │
  │   { ... });            │  │   (env, force) =>           │  │ // Command already     │
  │                        │  │   { ... });                 │  │ // exists              │
  └───────────┬────────────┘  └──────────────┬──────────────┘  └───────────┬────────────┘
              │                              │                             │
              ▼                              ▼                             │
  ┌───────────────────────────────────────────────────────────┐            │
  │                   SOURCE GENERATOR                        │            │
  │                                                           │            │
  │  1. Parse route (string → fluent builder calls)           │            │
  │  2. OR walk fluent builder calls directly                 │            │
  │  3. Generate Command class from delegate signature        │◄───────────┘
  │  4. Generate Handler class from delegate body             │  (already has command)
  │  5. Generate static CompiledRoute                         │
  │  6. Generate registration code                            │
  │                                                           │
  └─────────────────────────┬─────────────────────────────────┘
                            ▼
  ┌───────────────────────────────────────────────────────────────────────────────┐
  │                          GENERATED OUTPUT                                     │
  │                                                                               │
  │  // Command (generated from delegate signature)                               │
  │  public sealed record Deploy_Generated_Command(string Env, bool Force)        │
  │      : ICommand<int>;                                                         │
  │                                                                               │
  │  // Handler (wraps original delegate body)                                    │
  │  public sealed class Deploy_Generated_CommandHandler                          │
  │      : ICommandHandler<Deploy_Generated_Command, int>                         │
  │  {                                                                            │
  │      public Task<int> Handle(Deploy_Generated_Command cmd, CancellationToken) │
  │      {                                                                        │
  │          // Original delegate body here                                       │
  │          return ...;                                                          │
  │      }                                                                        │
  │  }                                                                            │
  │                                                                               │
  │  // Static CompiledRoute (built via fluent builder)                           │
  │  private static readonly CompiledRoute __Route_Deploy =                       │
  │      new CompiledRouteBuilder()                                               │
  │          .WithLiteral("deploy")                                               │
  │          .WithParameter("env")                                                │
  │          .WithOption("force")                                                 │
  │          .Build();                                                            │
  │                                                                               │
  │  // Registration                                                              │
  │  app.MapCommand<Deploy_Generated_Command>(__Route_Deploy);                    │
  │                                                                               │
  └───────────────────────────────────────────────────────────────────────────────┘
                            ▼
  ┌───────────────────────────────────────────────────────────────────────────────┐
  │                          RUNTIME (same for ALL)                               │
  │                                                                               │
  │  Route Match → Bind Args → Create Command → Pipeline → Handler                │
  │                                                                               │
  │       ┌─────────┐    ┌──────────┐    ┌───────────┐    ┌─────────┐             │
  │  ───► │ Logging │ ─► │ Validate │ ─► │ Telemetry │ ─► │ Handler │             │
  │       │Middleware│   │Middleware│    │Middleware │    │         │             │
  │       └─────────┘    └──────────┘    └───────────┘    └─────────┘             │
  │                                                                               │
  │  No delegates exist at runtime. Only commands through the pipeline.           │
  │                                                                               │
  └───────────────────────────────────────────────────────────────────────────────┘
```

## Generation Flow: String → Fluent → Static

The key insight is that **string patterns can be translated to fluent builder calls**, making the fluent builder the canonical construction mechanism:

```
  "deploy {env} --force"
           │
           ▼
  ┌─────────────────────┐
  │  Parser (compile)   │  ← Already exists, used in analyzer
  └──────────┬──────────┘
             ▼
  ┌─────────────────────┐
  │  Fluent Builder     │  ← Intermediate representation
  │  (generated code)   │
  │                     │
  │  new CompiledRoute- │
  │    Builder()        │
  │    .WithLiteral()   │
  │    .WithParameter() │
  │    .WithOption()    │
  │    .Build()         │
  └──────────┬──────────┘
             ▼
  ┌─────────────────────┐
  │  static Compiled-   │  ← Final runtime artifact
  │  Route              │
  └─────────────────────┘
```

### Benefits of This Approach

| Benefit | Explanation |
|---------|-------------|
| **Single code path** | Fluent builder is the canonical way to construct a `CompiledRoute` |
| **Debuggable** | Generated fluent code is readable, can set breakpoints |
| **Testable** | Builder logic tested once, used by both paths |
| **Parser stays clean** | Parser outputs builder calls, not raw `CompiledRoute` construction |

## Fluent Builder API

### `CompiledRouteBuilder` Class

```csharp
public class CompiledRouteBuilder
{
    private readonly List<RouteMatcher> _segments = [];
    private string? _catchAllParameterName;
    private int _specificity;

    // Specificity constants (same as Compiler)
    private const int SpecificityLiteralSegment = 100;
    private const int SpecificityRequiredOption = 50;
    private const int SpecificityOptionalOption = 25;
    private const int SpecificityTypedParameter = 20;
    private const int SpecificityUntypedParameter = 10;
    private const int SpecificityOptionalParameter = 5;
    private const int SpecificityCatchAll = 1;

    /// <summary>
    /// Adds a literal segment (e.g., "git", "commit").
    /// </summary>
    public CompiledRouteBuilder WithLiteral(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _segments.Add(new LiteralMatcher(value));
        _specificity += SpecificityLiteralSegment;
        return this;
    }

    /// <summary>
    /// Adds a required positional parameter.
    /// </summary>
    public CompiledRouteBuilder WithParameter(
        string name,
        string? type = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _segments.Add(new ParameterMatcher(name, isCatchAll: false, type, description, isOptional: false));
        _specificity += string.IsNullOrEmpty(type) ? SpecificityUntypedParameter : SpecificityTypedParameter;
        return this;
    }

    /// <summary>
    /// Adds an optional positional parameter.
    /// </summary>
    public CompiledRouteBuilder WithOptionalParameter(
        string name,
        string? type = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _segments.Add(new ParameterMatcher(name, isCatchAll: false, type, description, isOptional: true));
        _specificity += SpecificityOptionalParameter;
        return this;
    }

    /// <summary>
    /// Adds an option (flag or option with value).
    /// </summary>
    public CompiledRouteBuilder WithOption(
        string longForm,
        string? shortForm = null,
        string? parameterName = null,
        bool expectsValue = false,
        string? description = null,
        bool isOptional = true,
        bool isRepeated = false)
    {
        string matchPattern = $"--{longForm}";
        string? alternateForm = shortForm is not null ? $"-{shortForm}" : null;
        
        // For boolean flags, derive parameter name from long form
        string? resolvedParamName = parameterName ?? (expectsValue ? null : ToCamelCase(longForm));

        _segments.Add(new OptionMatcher(
            matchPattern: matchPattern,
            expectsValue: expectsValue,
            parameterName: resolvedParamName,
            alternateForm: alternateForm,
            description: description,
            isOptional: isOptional,
            isRepeated: isRepeated,
            parameterIsOptional: false
        ));
        
        _specificity += isOptional ? SpecificityOptionalOption : SpecificityRequiredOption;
        return this;
    }

    /// <summary>
    /// Adds a catch-all parameter (must be last).
    /// </summary>
    public CompiledRouteBuilder WithCatchAll(
        string name,
        string? type = null,
        string? description = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        
        if (_catchAllParameterName is not null)
            throw new InvalidOperationException("Only one catch-all parameter is allowed.");

        _catchAllParameterName = name;
        _segments.Add(new ParameterMatcher(name, isCatchAll: true, type, description, isOptional: false));
        _specificity += SpecificityCatchAll;
        return this;
    }

    /// <summary>
    /// Builds the CompiledRoute.
    /// </summary>
    public CompiledRoute Build()
    {
        return new CompiledRoute
        {
            Segments = _segments.ToArray(),
            CatchAllParameterName = _catchAllParameterName,
            Specificity = _specificity
        };
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        string cleaned = input.Replace("-", "").Replace("_", "");
        if (cleaned.Length == 0) return input;
        return char.ToLowerInvariant(cleaned[0]) + cleaned[1..];
    }
}
```

### Usage Examples

```csharp
// Equivalent to: "git commit {message} --amend"
var route = new CompiledRouteBuilder()
    .WithLiteral("git")
    .WithLiteral("commit")
    .WithParameter("message")
    .WithOption("amend", shortForm: "a")
    .Build();

// Equivalent to: "deploy {env} --force --config {file}"
var route2 = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env", description: "Target environment")
    .WithOption("force", shortForm: "f")
    .WithOption("config", shortForm: "c", parameterName: "file", expectsValue: true)
    .Build();

// Equivalent to: "exec {*args}"
var route3 = new CompiledRouteBuilder()
    .WithLiteral("exec")
    .WithCatchAll("args")
    .Build();
```

## Consumer API

### `IEndpointCollectionBuilder`

```csharp
public interface IEndpointCollectionBuilder
{
    // === Delegate-based (source generator creates Command/Handler) ===
    
    // String pattern + delegate
    void Map(string routePattern, Delegate handler, string? description = null);
    
    // Fluent builder + delegate
    void Map(Action<CompiledRouteBuilder> configure, Delegate handler, string? description = null);
    
    // Pre-built route + delegate
    void Map(CompiledRoute compiledRoute, Delegate handler, string? description = null);
    
    // === Command-based (user provides Command, Handler already exists) ===
    
    // String pattern + command type
    void Map<TCommand>(string routePattern, string? description = null) 
        where TCommand : ICommand;
    
    // Fluent builder + command type
    void Map<TCommand>(Action<CompiledRouteBuilder> configure, string? description = null) 
        where TCommand : ICommand;
    
    // Pre-built route + command type
    void Map<TCommand>(CompiledRoute compiledRoute, string? description = null) 
        where TCommand : ICommand;
}
```

### Consumer Choice

```csharp
// === Delegate-based (generates Command/Handler) ===

// Option 1: String pattern + delegate (concise, familiar)
app.Map("deploy {env} --force", (string env, bool force) => { ... });

// Option 2: Fluent builder + delegate (IDE autocomplete, refactor-friendly)
app.Map(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"),
    (string env, bool force) => { ... });

// Option 3: Pre-built route + delegate (reusable, dynamic scenarios)
CompiledRoute route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force")
    .Build();
app.Map(route, (string env, bool force) => { ... });


// === Command-based (user provides Command/Handler) ===

// Option 4: String pattern + command
app.Map<DeployCommand>("deploy {env} --force");

// Option 5: Fluent builder + command
app.Map<DeployCommand>(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"));

// Option 6: Pre-built route + command
app.Map<DeployCommand>(route);
```

### Default Routes

Default routes (fallback when no other route matches) use an empty string pattern. No separate `MapDefault` method is needed - this keeps the API surface minimal and simplifies the source generator.

```csharp
// Default route with delegate
app.Map("", () => Console.WriteLine("Usage: mycli <command>"));

// Default route with options
app.Map("--verbose,-v", (bool verbose) => ShowHelp(verbose));

// Default route with command
app.Map<HelpCommand>("");

// Default route with command + options
app.Map<HelpCommand>("--verbose,-v --format {fmt?}");
```

### Why Support Both Syntaxes?

| Use Case | String Pattern | Fluent Builder |
|----------|---------------|----------------|
| Quick prototyping | Faster to type | |
| Complex patterns | Harder to read | Self-documenting |
| IDE support | No autocomplete | Full IntelliSense |
| Refactoring | Find/replace | Rename symbol |
| Dynamic route generation | Parse at runtime | Build programmatically |
| Code generation | Emit strings | Emit builder calls |
| Validation timing | Runtime parse errors | Compile-time (with analyzers) |

## What Gets Generated

### From String Syntax

```csharp
// User writes:
app.Map("deploy {env} --force", (string env, bool force) => 
{
    Console.WriteLine($"Deploying to {env}, force={force}");
    return 0;
});

// Generator emits:
private static readonly CompiledRoute __Route_Deploy = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force")
    .Build();

// Plus Command, Handler, and registration (see below)
```

### From Fluent Syntax

```csharp
// User writes:
app.Map(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"),
    (string env, bool force) => 
    {
        Console.WriteLine($"Deploying to {env}, force={force}");
        return 0;
    });

// Generator emits exactly the same:
private static readonly CompiledRoute __Route_Deploy = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force")
    .Build();

// Plus Command, Handler, and registration (see below)
```

### Generated Command

```csharp
[GeneratedCode("TimeWarp.Nuru.Generator", "1.0.0")]
public sealed record Deploy_Generated_Command(
    string Env,
    bool Force
) : ICommand<int>;
```

### Generated Handler

```csharp
[GeneratedCode("TimeWarp.Nuru.Generator", "1.0.0")]
public sealed class Deploy_Generated_CommandHandler 
    : ICommandHandler<Deploy_Generated_Command, int>
{
    public Task<int> Handle(Deploy_Generated_Command command, CancellationToken cancellationToken)
    {
        // Extracted from delegate body
        Console.WriteLine($"Deploying to {command.Env}, force={command.Force}");
        return Task.FromResult(0);
    }
}
```

### Generated Registration

```csharp
// Replaces the original Map call
app.MapCommand<Deploy_Generated_Command>(
    __Route_Deploy,
    "deploy {env} --force");  // Original pattern preserved for help display

// Handler registered in DI
services.AddTransient<ICommandHandler<Deploy_Generated_Command, int>, Deploy_Generated_CommandHandler>();
```

## DI Integration

Parameters not in the route pattern are resolved from DI:

```csharp
// User writes (ILogger not in route, must be injected):
app.Map("deploy {env}", (string env, ILogger logger) => 
{
    logger.LogInformation("Deploying to {Env}", env);
    return 0;
});

// Generated Command (only route parameters):
public sealed record Deploy_Command(string Env) : ICommand<int>;

// Generated Handler (DI parameters injected via constructor):
public sealed class Deploy_CommandHandler : ICommandHandler<Deploy_Command, int>
{
    private readonly ILogger _logger;
    
    public Deploy_CommandHandler(ILogger logger)
    {
        _logger = logger;
    }
    
    public Task<int> Handle(Deploy_Command command, CancellationToken ct)
    {
        _logger.LogInformation("Deploying to {Env}", command.Env);
        return Task.FromResult(0);
    }
}
```

## Unified Runtime Model

All three syntaxes become the same thing at runtime:

```csharp
// 1. Delegate syntax
app.Map("greet {name}", (string name) => Console.WriteLine($"Hello {name}"));

// 2. Fluent syntax  
app.Map(r => r.WithLiteral("greet").WithParameter("name"), 
    (string name) => Console.WriteLine($"Hello {name}"));

// 3. Explicit command
app.Map<GreetCommand>("greet {name}");

// ─────────────────────────────────────────────────────────────────────
// At runtime, ALL become:
//
//   args[] → Route Match → Command Instance → Pipeline → Handler
//
// No delegates exist at runtime. Only commands through the pipeline.
```

## Benefits of Unified Model

| Benefit | Explanation |
|---------|-------------|
| **Unified middleware** | Logging, validation, telemetry work on ALL routes |
| **Consistent behavior** | No "delegate routes skip middleware" surprises |
| **AOT friendly** | No reflection for delegate invocation |
| **Testable** | Generated commands can be unit tested directly |
| **Debuggable** | Step through generated handler code |
| **Pipeline power** | Retry, circuit breaker, caching - all work everywhere |

## Key Insight

**Delegates are developer convenience; commands are the runtime reality.**

The string pattern and fluent builder are both syntactic sugar. The source generator transforms everything into the unified Command/Handler model, ensuring consistent behavior regardless of how the route was originally declared.

## Relationship to Existing Parser

The existing `PatternParser` (already used in the analyzer for compile-time validation) can be leveraged:

1. **For string patterns:** Parser extracts segments → Generator emits equivalent builder calls
2. **For fluent patterns:** Generator walks the builder expression tree directly

This means the parser remains the single source of truth for pattern syntax, while the builder becomes the single mechanism for constructing `CompiledRoute` instances.

## RoutePattern String Reconstruction

Since `Endpoint.RoutePattern` is required for help display, builder-created routes need a way to generate the pattern string. Options:

1. **Add `ToPatternString()` to `CompiledRoute`** - Reconstruct from segments
2. **Require explicit pattern** - Consumer provides display string
3. **Generator emits both** - Builder calls AND original pattern string

Option 3 (generator emits both) is cleanest since the generator has access to the original source.
