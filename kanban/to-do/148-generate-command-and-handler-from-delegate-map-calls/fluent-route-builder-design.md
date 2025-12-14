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

  ┌────────────────────────┐  ┌─────────────────────────────┐  ┌────────────────────────┐  ┌────────────────────────┐
  │   String + Delegate    │  │     Fluent + Delegate       │  │   Command + Pattern    │  │  Attributed Command    │
  │                        │  │                             │  │                        │  │                        │
  │ app.Map(               │  │ app.Map(r => r              │  │ app.Map<DeployCommand>(│  │ [Route("deploy")]      │
  │   "deploy {env}        │  │   .WithLiteral("deploy")    │  │   "deploy {env}        │  │ record DeployCommand(  │
  │    --force",           │  │   .WithParameter("env")     │  │    --force");          │  │   [Parameter] string   │
  │   (env, force) =>      │  │   .WithOption("force"),     │  │                        │  │     Env,               │
  │   { ... });            │  │   (env, force) =>           │  │                        │  │   [Option("--force")]  │
  │                        │  │   { ... });                 │  │                        │  │     bool Force         │
  │                        │  │                             │  │                        │  │ ) : ICommand<int>;     │
  └───────────┬────────────┘  └──────────────┬──────────────┘  └───────────┬────────────┘  └───────────┬────────────┘
              │                              │                             │                          │
              ▼                              ▼                             ▼                          ▼
  ┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
  │                                        SOURCE GENERATOR                                                        │
  │                                                                                                                │
  │  1. Parse route (string → fluent builder calls)                                                                │
  │  2. OR walk fluent builder calls directly                                                                      │
  │  3. OR read attributes from Command class                                                                      │
  │  4. Generate Command class from delegate signature (if delegate-based)                                         │
  │  5. Generate Handler class from delegate body (if delegate-based)                                              │
  │  6. Generate static CompiledRoute                                                                              │
  │  7. Generate registration code                                                                                 │
  │                                                                                                                │
  └─────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┘
                                                    ▼
  ┌───────────────────────────────────────────────────────────────────────────────┐
  │                          GENERATED OUTPUT                                     │
  │                                                                               │
  │  // Command (generated from delegate signature, or user-provided)             │
  │  public sealed record Deploy_Generated_Command(string Env, bool Force)        │
  │      : ICommand<int>;                                                         │
  │                                                                               │
  │  // Handler (wraps original delegate body, or user-provided)                  │
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

### Why Support Multiple Syntaxes?

| Use Case | String Pattern | Fluent Builder | Attributed Command |
|----------|---------------|----------------|-------------------|
| Quick prototyping | Faster to type | | |
| Complex patterns | Harder to read | Self-documenting | Self-documenting |
| IDE support | No autocomplete | Full IntelliSense | Full IntelliSense |
| Refactoring | Find/replace | Rename symbol | Rename symbol |
| Dynamic route generation | Parse at runtime | Build programmatically | N/A |
| Code generation | Emit strings | Emit builder calls | Emit attributes |
| Validation timing | Runtime parse errors | Compile-time | Compile-time |
| Single source of truth | Pattern + Command separate | Pattern + Command separate | Command IS the route |
| Zero-ceremony | Requires Map call | Requires Map call | Auto-registered |

---

## Attribute-Based Route Definition

In addition to explicit `Map` calls, routes can be defined directly on Command classes using attributes. This provides a **zero-ceremony** approach where the Command definition IS the route definition.

### Attribute API

```csharp
// Route prefix (literals) - applied to Command class
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RouteAttribute : Attribute
{
    public RouteAttribute(string pattern) { }  // "git commit" or just "deploy" or ""
    public string? Description { get; set; }
}

// Route alias - for commands with multiple patterns
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class RouteAliasAttribute : Attribute
{
    public RouteAliasAttribute(string pattern) { }
}

// Positional parameter - applied to properties/parameters
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class ParameterAttribute : Attribute
{
    public string? Name { get; set; }           // Override parameter name
    public string? Description { get; set; }
    public bool IsCatchAll { get; set; }
    // Optional is inferred from nullability (string? = optional)
}

// Option (flag or valued) - applied to properties/parameters
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class OptionAttribute : Attribute
{
    public OptionAttribute(string longForm, string? shortForm = null) { }
    public string? Description { get; set; }
    public bool IsRepeated { get; set; }
    // Optional is inferred from nullability or bool type
}
```

### Examples

#### Simple Command

```csharp
[Route("greet")]
public sealed record GreetCommand(
    [Parameter] string Name
) : ICommand;

// Generates route: "greet {name}"
```

#### With Options

```csharp
[Route("deploy", Description = "Deploy to an environment")]
public sealed record DeployCommand(
    [Parameter(Description = "Target environment")] string Env,
    [Option("--force", "-f", Description = "Skip confirmation")] bool Force,
    [Option("--config", "-c")] string? ConfigFile
) : ICommand<int>;

// Generates route: "deploy {env} --force,-f --config,-c {configFile?}"
```

#### Nested Literals

```csharp
[Route("docker compose up")]
public sealed record DockerComposeUpCommand(
    [Option("--detach", "-d")] bool Detach,
    [Option("--build")] bool Build
) : ICommand;

// Generates route: "docker compose up --detach,-d --build"
```

#### Catch-All

```csharp
[Route("exec")]
public sealed record ExecCommand(
    [Parameter(IsCatchAll = true)] string[] Args
) : ICommand<int>;

// Generates route: "exec {*args}"
```

#### Default Route

```csharp
[Route("")]  // Empty = default route
public sealed record HelpCommand(
    [Option("--verbose", "-v")] bool Verbose
) : ICommand;

// Generates route: "--verbose,-v" (matches when no other route matches)
```

#### Aliases

```csharp
[Route("exit")]
[RouteAlias("quit")]
[RouteAlias("q")]
public sealed record ExitCommand : ICommand;

// Generates routes: "exit", "quit", "q" - all map to same command
```

#### Mixed Parameters and Options

```csharp
[Route("git commit")]
public sealed record GitCommitCommand(
    [Parameter] string? Message,                           // Optional positional
    [Option("--amend", "-a")] bool Amend,                 // Boolean flag
    [Option("--author")] string? Author,                  // Optional valued option
    [Option("--message", "-m")] string? MessageOption     // Alternative to positional
) : ICommand<int>;

// Generates route: "git commit {message?} --amend,-a --author {author?} --message,-m {messageOption?}"
```

### What Gets Generated from Attributed Commands

```csharp
// User writes:
[Route("deploy")]
public sealed record DeployCommand(
    [Parameter] string Env,
    [Option("--force", "-f")] bool Force,
    [Option("--replicas", "-r")] int? Replicas
) : ICommand<int>;

public sealed class DeployCommandHandler : ICommandHandler<DeployCommand, int>
{
    public Task<int> Handle(DeployCommand command, CancellationToken ct) { ... }
}


// Source generator emits:

// 1. CompiledRoute
private static readonly CompiledRoute __Route_DeployCommand = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force", shortForm: "f")
    .WithOption("replicas", shortForm: "r", expectsValue: true, isOptional: true)
    .Build();

// 2. Route pattern string (for help display)
private const string __Pattern_DeployCommand = "deploy {env} --force,-f --replicas,-r {replicas?}";

// 3. Auto-registration (no Map call needed!)
internal static class GeneratedRouteRegistration
{
    [ModuleInitializer]
    public static void Register()
    {
        NuruRouteRegistry.Register<DeployCommand>(__Route_DeployCommand, __Pattern_DeployCommand);
    }
}
```

### Auto-Registration

With attributed commands, no explicit `Map` calls are needed. The source generator discovers all `[Route]` attributed commands and registers them automatically:

```csharp
// User code - just build and run!
var builder = NuruApp.CreateBuilder(args);

// Optionally configure services
builder.ConfigureServices(services => 
{
    services.AddSingleton<IDeploymentService, DeploymentService>();
});

var app = builder.Build();
return await app.RunAsync();

// That's it! All [Route] commands are auto-registered.
```

### Combining Approaches

Attributed commands can coexist with explicit `Map` calls:

```csharp
var builder = NuruApp.CreateBuilder(args);

// Explicit delegate-based routes
builder.Map("quick {name}", (string name) => Console.WriteLine($"Quick: {name}"));

// Explicit command-based routes (overrides attribute if present)
builder.Map<SpecialCommand>("special-route");

var app = builder.Build();
return await app.RunAsync();

// Meanwhile, all other [Route] commands are auto-registered
```

### Benefits of Attribute Approach

| Benefit | Explanation |
|---------|-------------|
| **Self-documenting** | Command definition IS the route definition |
| **Single source of truth** | No drift between command properties and route pattern |
| **Zero ceremony** | No `Map` calls needed at all |
| **Refactor-safe** | Rename property = route updates automatically |
| **IDE support** | Full IntelliSense on attributes |
| **Validation** | Analyzer can verify attributes match property types |
| **Discoverability** | Look at any Command to see its route |

### Trade-offs

| Consideration | Notes |
|---------------|-------|
| **Centralized view** | Must look at command classes to see routes (vs. one file with all `Map` calls) |
| **Dynamic routes** | Can't do runtime-computed patterns (but that's rare) |
| **Learning curve** | Another way to do things (but consistent with ASP.NET patterns) |

---

## Summary: Three Styles

All three styles generate the same runtime artifacts - the difference is ergonomics:

```csharp
// Style 1: Delegate + String Pattern (quick prototyping)
app.Map("deploy {env} --force", (string env, bool force) => { ... });

// Style 2: Command + String Pattern (explicit mapping)
app.Map<DeployCommand>("deploy {env} --force");

// Style 3: Attributed Command (zero-ceremony, self-documenting)
[Route("deploy")]
public sealed record DeployCommand(
    [Parameter] string Env,
    [Option("--force", "-f")] bool Force
) : ICommand<int>;
// No Map call needed - auto-registered!
```

| Style | Best For |
|-------|----------|
| **Delegate + Pattern** | Quick scripts, prototyping, simple CLIs |
| **Command + Pattern** | When you want explicit control over mapping |
| **Attributed Command** | Production apps, large CLIs, team projects |

---

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

All syntaxes become the same thing at runtime:

```csharp
// 1. Delegate syntax
app.Map("greet {name}", (string name) => Console.WriteLine($"Hello {name}"));

// 2. Fluent syntax  
app.Map(r => r.WithLiteral("greet").WithParameter("name"), 
    (string name) => Console.WriteLine($"Hello {name}"));

// 3. Explicit command
app.Map<GreetCommand>("greet {name}");

// 4. Attributed command
[Route("greet")]
public sealed record GreetCommand([Parameter] string Name) : ICommand;

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

The string pattern, fluent builder, and attributes are all syntactic sugar. The source generator transforms everything into the unified Command/Handler model, ensuring consistent behavior regardless of how the route was originally declared.

## Relationship to Existing Parser

The existing `PatternParser` (already used in the analyzer for compile-time validation) can be leveraged:

1. **For string patterns:** Parser extracts segments → Generator emits equivalent builder calls
2. **For fluent patterns:** Generator walks the builder expression tree directly
3. **For attributed commands:** Generator reads attributes → emits equivalent builder calls

This means the parser remains the single source of truth for pattern syntax, while the builder becomes the single mechanism for constructing `CompiledRoute` instances.

## RoutePattern String Reconstruction

Since `Endpoint.RoutePattern` is required for help display, builder-created routes need a way to generate the pattern string. Options:

1. **Add `ToPatternString()` to `CompiledRoute`** - Reconstruct from segments
2. **Require explicit pattern** - Consumer provides display string
3. **Generator emits both** - Builder calls AND original pattern string

Option 3 (generator emits both) is cleanest since the generator has access to the original source.
