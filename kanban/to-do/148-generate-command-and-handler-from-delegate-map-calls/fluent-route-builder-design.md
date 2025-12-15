# Fluent Route Builder Design

## Overview

This document describes the design for a fluent `CompiledRouteBuilder` API that provides an alternative to string-based route patterns. Both syntaxes ultimately compile down to the same runtime artifact: a `CompiledRoute` feeding into the unified Command/Handler pipeline.

> **IMPORTANT: Conventions**
> 
> 1. **Class Convention:** All Command classes use **classes with properties**, NOT records or primary constructors. This aligns with API-side conventions.
> 
> 2. **Interface Naming:** We use the Mediator interfaces `IRequest<TResponse>`/`IRequestHandler<TRequest, TResponse>` directly. Class names use "Command" terminology (e.g., `DeployCommand`) since it's more natural for CLI contexts.
> 
> ```csharp
> // Actual implementation
> public sealed class DeployCommand : IRequest<int>
> {
>     public string Env { get; set; } = string.Empty;
>     public bool Force { get; set; }
> }
> 
> public sealed class DeployCommandHandler : IRequestHandler<DeployCommand, int>
> {
>     public Task<int> Handle(DeployCommand request, CancellationToken ct) { ... }
> }
> ```

## Design Goals

1. **Dual syntax support** - Consumers can use string patterns OR fluent builder
2. **Single runtime model** - Both compile to static `CompiledRoute` + Command/Handler
3. **Source generation** - No runtime parsing; everything resolved at compile time
4. **Unified pipeline** - All routes (delegate or command) flow through mediator pipeline

## Phased Implementation

The implementation follows a phased approach. Each phase builds on the previous without rework, and most phases are independently releasable.

| Phase | Name | Description | Releasable? |
|-------|------|-------------|-------------|
| **0** | Foundation | `CompiledRouteBuilder` (internal) + tests | No |
| **1** | Attributed Routes | `[Route]`, `[RouteGroup]` → auto-registration | **Yes** |
| **2** | Delegate Generation | String pattern + delegate → Command/Handler gen | **Yes** |
| **3** | Unified Pipeline | Remove `DelegateExecutor`, single code path | **Yes** |
| **4** | Fluent Builder API | Public `CompiledRouteBuilder`, `MapGroup()` | **Yes** |
| **5** | Relaxed Constraints | Data flow analysis for `MapGroup()` | **Yes** |

```
Phase 0          Phase 1           Phase 2              Phase 3           Phase 4              Phase 5
────────────────────────────────────────────────────────────────────────────────────────────────────────────
CompiledRoute    [Route] attrs     String+Delegate      Remove            Public fluent        Data flow
Builder          auto-register     → Command/Handler    DelegateExecutor  builder + MapGroup   analysis
(internal)                         generation           Single code path  API

Foundation       First Release     Second Release       Third Release     Fourth Release       Fifth Release
```

---

### Phase 0: Foundation (Internal)

**Goal:** Create `CompiledRouteBuilder` and validate it produces correct `CompiledRoute` instances.

**Scope:**
- Create `CompiledRouteBuilder` class (internal visibility)
- Add `[InternalsVisibleTo]` for test project
- Write tests comparing:
  - `PatternParser.Parse("pattern")` result
  - `new CompiledRouteBuilder().WithLiteral()...Build()` result
- Existing `Compiler` stays as-is (parallel code paths for now)

**Not Releasable** - No consumer-facing changes. This is internal infrastructure.

---

### Phase 1: Attributed Routes ✨ Release

**Goal:** Commands with `[Route]` attributes auto-register without explicit `Map()` calls.

**Scope:**
- `[Route]`, `[RouteAlias]`, `[RouteGroup]`, `[Parameter]`, `[Option]`, `[GroupOption]` attributes
- Source generator reads attributes from Command classes
- Generator emits `CompiledRouteBuilder` calls for each attributed Command
- Auto-registration via `[ModuleInitializer]`
- User still writes Command and Handler classes (no generation)

**What the generator does:**
1. Find all classes with `[Route]` attribute
2. Read attributes → emit `CompiledRouteBuilder` calls
3. Emit registration code

**Releasable** - Production use case for Command-based CLIs. Clean, attribute-driven development.

---

### Phase 2: Delegate Generation ✨ Release

**Goal:** Delegates in `Map()` calls automatically become Commands through the pipeline.

**Scope:**
- Source generator detects `Map(string pattern, Delegate handler)` calls
- Generates Command class from delegate signature
- Generates Handler class from delegate body
- Rewrites parameter references (`x` → `command.X`)
- Emits `CompiledRouteBuilder` calls for route
- Emits registration code
- DI parameter detection (parameters not in route → constructor injection)

**What the generator does:**
1. Find all `Map(pattern, delegate)` calls
2. Parse pattern string → emit `CompiledRouteBuilder` calls
3. Extract delegate signature → generate Command class
4. Extract delegate body → generate Handler class (with parameter rewriting)
5. Emit registration code

**Releasable** - Enables quick prototyping with delegates while maintaining pipeline benefits.

---

### Phase 3: Unified Pipeline ✨ Release

**Goal:** All routes flow through Mediator pipeline. Single code path for `CompiledRoute` construction.

**Scope:**
- Remove `DelegateExecutor` - no more direct delegate invocation
- All routes (delegate-based and command-based) flow through Mediator pipeline
- Refactor `Compiler` to use `CompiledRouteBuilder` internally
- Single mechanism for constructing `CompiledRoute` instances

**Benefits:**
- Consistent middleware behavior for ALL routes
- Simplified internals (one code path)
- Better AOT compatibility

**Releasable** - Internal simplification with consistent runtime behavior.

---

### Phase 4: Fluent Builder API + MapGroup ✨ Release

**Goal:** Expose fluent builder to consumers. Add `MapGroup()` for delegate-based grouped routes.

**Scope:**
- Make `CompiledRouteBuilder` public
- Add `Map(Action<CompiledRouteBuilder>, Delegate)` overload
- Add `Map<TCommand>(Action<CompiledRouteBuilder>)` overload
- Add `MapGroup()` API with fluent chain constraint
- Source generator walks fluent builder syntax tree

**MapGroup Constraint:**
Group routes must be defined in a fluent chain or with immediate `Map` calls:
```csharp
// SUPPORTED
builder.MapGroup("docker").WithGroupOptions("--debug").Map("run {image}", handler);

// SUPPORTED
var docker = builder.MapGroup("docker");
docker.Map("run {image}", handler);  // Immediate, trackable
```

**Releasable** - Advanced consumer API for complex CLIs.

---

### Phase 5: Relaxed Constraints ✨ Release

**Goal:** More flexible `MapGroup()` usage via data flow analysis.

**Scope:**
- Data flow analysis within method scope for `MapGroup()` variable tracking
- Diagnostic warnings for truly unresolvable cases (passed to methods, stored in fields)

```csharp
// Phase 4: Warning
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // ⚠️ NURU003

// Phase 5: Works
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // ✓ Resolved
```

**Releasable** - Quality of life improvement for `MapGroup()` users.

---

## The Full Architecture

```
                        CONSUMER WRITES (all equivalent)

  ┌────────────────────────┐  ┌─────────────────────────────┐  ┌────────────────────────┐  ┌────────────────────────┐
  │   String + Delegate    │  │     Fluent + Delegate       │  │   Command + Pattern    │  │  Attributed Command    │
  │       (Phase 2)        │  │        (Phase 4)            │  │      (Phase 2)         │  │      (Phase 1)         │
  │                        │  │                             │  │                        │  │                        │
  │ app.Map(               │  │ app.Map(r => r              │  │ app.Map<DeployCommand>(│  │ [Route("deploy")]      │
  │   "deploy {env}        │  │   .WithLiteral("deploy")    │  │   "deploy {env}        │  │ record DeployCommand(  │
  │    --force",           │  │   .WithParameter("env")     │  │    --force");          │  │   [Parameter] string   │
  │   (env, force) =>      │  │   .WithOption("force"),     │  │                        │  │     Env,               │
  │   { ... });            │  │   (env, force) =>           │  │                        │  │   [Option("--force")]  │
  │                        │  │   { ... });                 │  │                        │  │     bool Force         │
  │                        │  │                             │  │                        │  │ ) : IRequest<int>;     │
  └───────────┬────────────┘  └──────────────┬──────────────┘  └───────────┬────────────┘  └───────────┬────────────┘
              │                              │                             │                          │
              ▼                              ▼                             ▼                          ▼
  ┌────────────────────────────────────────────────────────────────────────────────────────────────────────────────┐
  │                                        SOURCE GENERATOR                                                        │
  │                                                                                                                │
  │  1. Parse route (string → fluent builder calls)                         [Phase 2]                              │
  │  2. OR walk fluent builder calls directly                               [Phase 4]                              │
  │  3. OR read attributes from Command class                               [Phase 1]                              │
  │  4. Generate Command class from delegate signature (if delegate-based)  [Phase 2]                              │
  │  5. Generate Handler class from delegate body (if delegate-based)       [Phase 2]                              │
  │  6. Generate static CompiledRoute (via CompiledRouteBuilder)            [Phase 0+]                             │
  │  7. Generate registration code                                          [Phase 1+]                             │
  │                                                                                                                │
  └─────────────────────────────────────────────────┬──────────────────────────────────────────────────────────────┘
                                                    ▼
  ┌───────────────────────────────────────────────────────────────────────────────┐
  │                          GENERATED OUTPUT                                     │
  │                                                                               │
  │  // Command (generated from delegate signature, or user-provided)             │
  │  public sealed record Deploy_Generated_Command(string Env, bool Force)        │
  │      : IRequest<int>;                                                         │
  │                                                                               │
  │  // Handler (wraps original delegate body, or user-provided)                  │
  │  public sealed class Deploy_Generated_CommandHandler                          │
  │      : IRequestHandler<Deploy_Generated_Command, int>                         │
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
  │                          RUNTIME (same for ALL) [Phase 3+]                    │
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

> **Note:** Internal in Phase 0-3, public in Phase 4+

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

### Phase 1-3: `IEndpointCollectionBuilder`

```csharp
public interface IEndpointCollectionBuilder
{
    // === Delegate-based (source generator creates Command/Handler) [Phase 2+] ===
    
    // String pattern + delegate
    void Map(string routePattern, Delegate handler, string? description = null);
    
    // === Command-based (user provides Command, Handler already exists) ===
    
    // String pattern + command type
    void Map<TCommand>(string routePattern, string? description = null) 
        where TCommand : IRequest<Unit>;
}
```

### Phase 4+: Extended `IEndpointCollectionBuilder`

```csharp
public interface IEndpointCollectionBuilder
{
    // === Delegate-based (source generator creates Command/Handler) ===
    
    // String pattern + delegate
    void Map(string routePattern, Delegate handler, string? description = null);
    
    // Fluent builder + delegate [Phase 4+]
    void Map(Action<CompiledRouteBuilder> configure, Delegate handler, string? description = null);
    
    // Pre-built route + delegate [Phase 4+]
    void Map(CompiledRoute compiledRoute, Delegate handler, string? description = null);
    
    // === Command-based (user provides Command, Handler already exists) ===
    
    // String pattern + command type
    void Map<TCommand>(string routePattern, string? description = null) 
        where TCommand : IRequest<Unit>;
    
    // Fluent builder + command type [Phase 4+]
    void Map<TCommand>(Action<CompiledRouteBuilder> configure, string? description = null) 
        where TCommand : IRequest<Unit>;
    
    // Pre-built route + command type [Phase 4+]
    void Map<TCommand>(CompiledRoute compiledRoute, string? description = null) 
        where TCommand : IRequest<Unit>;
    
    // === Grouped routes [Phase 4+] ===
    
    // Create a route group with shared prefix and/or options
    IRouteGroupBuilder MapGroup(string prefix);
}

public interface IRouteGroupBuilder : IEndpointCollectionBuilder
{
    // Add description to the group (for help display)
    IRouteGroupBuilder WithDescription(string description);
    
    // Add options that apply to all routes in the group
    IRouteGroupBuilder WithGroupOptions(string optionsPattern);
    
    // Fluent version of group options
    IRouteGroupBuilder WithGroupOptions(Action<CompiledRouteBuilder> configure);
}
```

### Consumer Choice

```csharp
// === Phase 1: Attributed Commands (recommended for production) ===

[Route("deploy")]
public sealed record DeployCommand(
    [Parameter] string Env,
    [Option("--force", "-f")] bool Force
) : IRequest<int>;
// No Map call needed - auto-registered!


// === Phase 2+: Delegate-based ===

// String pattern + delegate (quick prototyping)
app.Map("deploy {env} --force", (string env, bool force) => { ... });


// === Phase 4+: Fluent builder ===

// Fluent builder + delegate (IDE autocomplete, refactor-friendly)
app.Map(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"),
    (string env, bool force) => { ... });

// Fluent builder + command
app.Map<DeployCommand>(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"));
```

### Default Routes

Default routes (fallback when no other route matches) use an empty string pattern. No separate `MapDefault` method is needed.

```csharp
// Default route with delegate [Phase 2+]
app.Map("", () => Console.WriteLine("Usage: mycli <command>"));

// Default route with options
app.Map("--verbose,-v", (bool verbose) => ShowHelp(verbose));

// Default route with command
app.Map<HelpCommand>("");

// Default route with attributed command [Phase 1+]
[Route("")]
public sealed record HelpCommand([Option("--verbose", "-v")] bool Verbose) : IRequest<Unit>;
```

### Grouped Routes

**Phase 1:** Use `[RouteGroup]` attributes only.

**Phase 4+:** Use `MapGroup()` API for delegate-based grouped routes.

```csharp
// Phase 1: Attributed groups
[RouteGroup("docker", Options = "--debug,-D")]
[Route("run")]
public sealed record DockerRunCommand(
    [Parameter] string Image,
    [GroupOption("--debug", "-D")] bool Debug
) : IRequest<Unit>;

// Phase 4+: MapGroup API
var docker = builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D");

docker.Map("run {image}", (string image, bool debug) => { ... });
docker.Map("build {path}", (string path, bool debug) => { ... });
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
| Available in Phase | 2+ | 4+ | 1+ |

---

## Attribute-Based Route Definition (Phase 1+)

Routes can be defined directly on Command classes using attributes. This is the **recommended approach for production** CLIs.

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

// Route group - for grouping related commands with shared prefix/options
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class RouteGroupAttribute : Attribute
{
    public RouteGroupAttribute(string prefix) { }
    public string? Description { get; set; }
    public string? Options { get; set; }  // Shared options pattern
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

// Group option - marks a parameter as coming from the group's shared options
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class GroupOptionAttribute : Attribute
{
    public GroupOptionAttribute(string longForm, string? shortForm = null) { }
}
```

### Examples

#### Simple Command

```csharp
[Route("greet")]
public sealed record GreetCommand(
    [Parameter] string Name
) : IRequest<Unit>;

// Generates route: "greet {name}"
```

#### With Options

```csharp
[Route("deploy", Description = "Deploy to an environment")]
public sealed record DeployCommand(
    [Parameter(Description = "Target environment")] string Env,
    [Option("--force", "-f", Description = "Skip confirmation")] bool Force,
    [Option("--config", "-c")] string? ConfigFile
) : IRequest<int>;

// Generates route: "deploy {env} --force,-f --config,-c {configFile?}"
```

#### Nested Literals

```csharp
[Route("docker compose up")]
public sealed record DockerComposeUpCommand(
    [Option("--detach", "-d")] bool Detach,
    [Option("--build")] bool Build
) : IRequest<Unit>;

// Generates route: "docker compose up --detach,-d --build"
```

#### Catch-All

```csharp
[Route("exec")]
public sealed record ExecCommand(
    [Parameter(IsCatchAll = true)] string[] Args
) : IRequest<int>;

// Generates route: "exec {*args}"
```

#### Default Route

```csharp
[Route("")]  // Empty = default route
public sealed record HelpCommand(
    [Option("--verbose", "-v")] bool Verbose
) : IRequest<Unit>;

// Generates route: "--verbose,-v" (matches when no other route matches)
```

#### Aliases

```csharp
[Route("exit")]
[RouteAlias("quit")]
[RouteAlias("q")]
public sealed record ExitCommand : IRequest<Unit>;

// Generates routes: "exit", "quit", "q" - all map to same command
```

#### Grouped Commands with Attributes

```csharp
// Define the group's shared options once
[RouteGroup("docker", Options = "--debug,-D --log-level {level?}")]
public abstract record DockerCommandBase(
    [GroupOption("--debug", "-D")] bool Debug,
    [GroupOption("--log-level")] string? LogLevel
);

// Commands inherit group prefix and options
[Route("run")]
public sealed record DockerRunCommand(
    [Parameter] string Image,
    bool Debug,
    string? LogLevel
) : DockerCommandBase(Debug, LogLevel), IRequest<Unit>;

// Generates route: "docker run {image} --debug,-D? --log-level {level?}"
```

### What Gets Generated from Attributed Commands

```csharp
// User writes:
[Route("deploy")]
public sealed record DeployCommand(
    [Parameter] string Env,
    [Option("--force", "-f")] bool Force,
    [Option("--replicas", "-r")] int? Replicas
) : IRequest<int>;

public sealed class DeployCommandHandler : IRequestHandler<DeployCommand, int>
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

With attributed commands, no explicit `Map` calls are needed:

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

### NuruRouteRegistry (Phase 1 Infrastructure)

`NuruRouteRegistry` is a static class that holds routes registered via `[ModuleInitializer]`. It enables auto-registration without explicit `Map()` calls.

```csharp
/// <summary>
/// Static registry for routes registered via [ModuleInitializer].
/// Used by source generator for attributed commands.
/// </summary>
public static class NuruRouteRegistry
{
    private static readonly ConcurrentDictionary<Type, RegisteredRoute> _routes = new();

    /// <summary>
    /// Register a route for a command type. Called by generated [ModuleInitializer] code.
    /// </summary>
    public static void Register<TCommand>(CompiledRoute route, string pattern)
        where TCommand : IBaseRequest
    {
        _routes[typeof(TCommand)] = new RegisteredRoute(route, pattern, typeof(TCommand));
    }

    /// <summary>
    /// Get all registered routes. Called by NuruApp.Build() to include auto-registered routes.
    /// </summary>
    public static IEnumerable<RegisteredRoute> GetRegisteredRoutes() => _routes.Values;

    /// <summary>
    /// Clear registry. Used for testing.
    /// </summary>
    internal static void Clear() => _routes.Clear();
}

public sealed class RegisteredRoute
{
    public CompiledRoute Route { get; }
    public string Pattern { get; }
    public Type CommandType { get; }

    public RegisteredRoute(CompiledRoute route, string pattern, Type commandType)
    {
        Route = route;
        Pattern = pattern;
        CommandType = commandType;
    }
}
```

**Integration with NuruApp:**

```csharp
// In NuruApp.Build() or similar:
foreach (var registered in NuruRouteRegistry.GetRegisteredRoutes())
{
    // Add to endpoint collection alongside explicit Map() routes
    endpoints.AddRoute(registered.Route, registered.Pattern, registered.CommandType);
}
```

---

## What Gets Generated (Phase 2+)

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

### From Fluent Syntax (Phase 4+)

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
) : IRequest<int>;
```

### Generated Handler

```csharp
[GeneratedCode("TimeWarp.Nuru.Generator", "1.0.0")]
public sealed class Deploy_Generated_CommandHandler 
    : IRequestHandler<Deploy_Generated_Command, int>
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
services.AddTransient<IRequestHandler<Deploy_Generated_Command, int>, Deploy_Generated_CommandHandler>();
```

## DI Integration (Phase 2+)

Parameters not in the route pattern are resolved from DI:

```csharp
// User writes (ILogger not in route, must be injected):
app.Map("deploy {env}", (string env, ILogger logger) => 
{
    logger.LogInformation("Deploying to {Env}", env);
    return 0;
});

// Generated Command (only route parameters):
public sealed record Deploy_Command(string Env) : IRequest<int>;

// Generated Handler (DI parameters injected via constructor):
public sealed class Deploy_CommandHandler : IRequestHandler<Deploy_Command, int>
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

## Unified Runtime Model (Phase 3+)

All syntaxes become the same thing at runtime:

```csharp
// 1. Delegate syntax [Phase 2+]
app.Map("greet {name}", (string name) => Console.WriteLine($"Hello {name}"));

// 2. Fluent syntax [Phase 4+]
app.Map(r => r.WithLiteral("greet").WithParameter("name"), 
    (string name) => Console.WriteLine($"Hello {name}"));

// 3. Explicit command [Phase 2+]
app.Map<GreetCommand>("greet {name}");

// 4. Attributed command [Phase 1+]
[Route("greet")]
public sealed record GreetCommand([Parameter] string Name) : IRequest<Unit>;

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

---

## Phase 4 Details: MapGroup() API

Phase 4 adds the `MapGroup()` fluent API for delegate-based grouped routes.

### Usage

```csharp
var docker = builder.MapGroup("docker")
    .WithDescription("Container management commands")
    .WithGroupOptions("--debug,-D --log-level {level?}");

docker.Map("run {image}", (string image, bool debug, string? logLevel) => { ... });
docker.Map("build {path}", (string path, bool debug, string? logLevel) => { ... });
```

**Resulting effective patterns:**
- `docker run {image} --debug,-D? --log-level {level?}`
- `docker build {path} --debug,-D? --log-level {level?}`

### Nested Groups

Groups can be nested, with prefixes and options accumulating:

```csharp
var docker = builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D");

var compose = docker.MapGroup("compose")
    .WithGroupOptions("--file,-f {path?}");

compose.Map("up", (bool debug, string? file) => { ... });
// Effective: "docker compose up --debug,-D? --file,-f {path?}"
```

### Fluent Chain Constraint (Phase 4)

**API CONSTRAINT:** Group routes must be defined in a fluent chain or with immediate `Map` calls:

```csharp
// SUPPORTED - fluent chain
builder.MapGroup("docker")
    .WithGroupOptions("--debug")
    .Map("run {image}", handler);

// SUPPORTED - variable but immediate Map calls
var docker = builder.MapGroup("docker").WithGroupOptions("--debug");
docker.Map("run {image}", handler);  // Same statement block, trackable
```

**Why:** Enables source generator to resolve group context without complex data flow analysis.

### What Gets Generated from Groups

```csharp
// User writes:
var docker = builder.MapGroup("docker")
    .WithGroupOptions("--debug,-D");

docker.Map("run {image}", (string image, bool debug) => 
{
    Console.WriteLine($"Running {image}, debug={debug}");
});

// Source generator emits:

// 1. Command with combined parameters
public sealed record DockerRun_Generated_Command(
    string Image,
    bool Debug
) : IRequest<Unit>;

// 2. CompiledRoute with combined pattern
private static readonly CompiledRoute __Route_DockerRun = new CompiledRouteBuilder()
    .WithLiteral("docker")
    .WithLiteral("run")
    .WithParameter("image")
    .WithOption("debug", shortForm: "D")
    .Build();

// 3. Handler with delegate body
public sealed class DockerRun_Generated_CommandHandler 
    : IRequestHandler<DockerRun_Generated_Command>
{
    public Task Handle(DockerRun_Generated_Command command, CancellationToken ct)
    {
        Console.WriteLine($"Running {command.Image}, debug={command.Debug}");
        return Task.CompletedTask;
    }
}
```

---

## Phase 5 Details: Relaxed Constraints

Phase 5 relaxes the fluent chain constraint by adding data flow analysis within method scope.

### What Changes

```csharp
// Phase 4: This emits a warning
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // ⚠️ NURU003: Cannot resolve group context

// Phase 5: This works - generator tracks variable within method
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // ✓ Resolved via data flow analysis
```

### Still Not Supported

Some patterns remain unsupported even in Phase 5:

```csharp
// Passed to another method - can't see inside
var docker = builder.MapGroup("docker");
RegisterDockerCommands(docker);  // ⚠️ Still emits warning

// Stored in a field - cross-method tracking too complex
private IRouteGroupBuilder _docker;

public void Setup(IEndpointCollectionBuilder builder)
{
    _docker = builder.MapGroup("docker");
}

public void RegisterCommands()
{
    _docker.Map("run {image}", handler);  // ⚠️ Still emits warning
}
```

### Recommendation

For complex scenarios where data flow analysis can't resolve group context:
- Use `[RouteGroup]` attributes (always works)
- Or specify the full pattern: `app.Map("docker run {image}", handler)`
