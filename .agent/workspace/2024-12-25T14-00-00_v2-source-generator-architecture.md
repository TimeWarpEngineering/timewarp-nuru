# V2 Source Generator Architecture

**Date:** 2024-12-25  
**Status:** Design Complete  
**Related:** 
- `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md` — DSL specification
- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs` — DSL reference implementation
- `samples/attributed-routes/` — Attributed routes pattern

## Executive Summary

This document defines the architecture for the V2 source generator. The generator transforms two input sources (Fluent DSL and Attributed Routes) into generated C# code that intercepts `RunAsync()` and provides compile-time routing with zero reflection.

---

## 1000ft View

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        SOURCE GENERATOR PIPELINE                         │
└─────────────────────────────────────────────────────────────────────────┘

    Consumer's Code                     Generator                      Output
    ──────────────                      ─────────                      ──────

┌──────────────────┐              ┌──────────────────┐          ┌──────────────────┐
│   Program.cs     │              │                  │          │  Generated.g.cs  │
│                  │   Roslyn     │   1. LOCATE      │          │                  │
│  NuruApp         │   Syntax  ──▶│   Find builder   │          │  [Interceptor]   │
│   .CreateBuilder │   Tree       │   chain + call   │          │  RunAsync_Gen()  │
│   .Map(...)      │              │   to RunAsync()  │          │  {              │
│   .Build()       │              │                  │          │    if status... │
│                  │              ├──────────────────┤          │    if admin...  │
│  app.RunAsync()◀─┼─ intercept ──│   2. EXTRACT     │──emit───▶│    ...          │
│                  │              │   Parse DSL to   │          │  }              │
└──────────────────┘              │   RouteModel[]   │          │                  │
                                  │                  │          │  Capabilities    │
┌──────────────────┐              ├──────────────────┤          │  HelpText        │
│  GreetQuery.cs   │              │   3. EMIT        │          └──────────────────┘
│                  │   Roslyn     │   Generate C#    │
│  [NuruRoute]     │   Symbols ──▶│   interceptor    │
│  class GreetQuery│              │                  │
│                  │              └──────────────────┘
└──────────────────┘
```

---

## Three Core Stages

| Stage        | Input                      | Output                   | Complexity |
| ------------ | -------------------------- | ------------------------ | ---------- |
| **1. Locate**    | Syntax tree / Symbols      | Call sites, DSL elements | Low-Medium |
| **2. Extract**   | Located syntax nodes       | `AppModel` with routes   | High       |
| **3. Emit**      | `AppModel`                   | C# source string         | Medium     |

---

## Folder Structure

```
source/timewarp-nuru-analyzers/
├── analyzers/               # Emit diagnostics (existing)
└── generators/              # Emit code (new)
    ├── locators/            # Find syntax elements
    ├── extractors/          # Parse DSL into models  
    ├── emitters/            # Generate C# code
    └── models/              # IR data structures
```

**Namespace:** `TimeWarp.Nuru.Generators` (flat, no sub-namespaces)

**File naming:** kebab-case per project standards (e.g., `run-async-locator.cs`)

---

## Two Input Sources

The generator handles two ways to define routes:

### 1. Fluent DSL

```csharp
NuruCoreApp app = NuruApp.CreateBuilder(args)
    .Map("status")
        .WithHandler(() => "healthy")
        .WithDescription("Check status")
        .AsQuery()
        .Done()
    .Build();

return await app.RunAsync(args);
```

**Characteristics:**
- Routes defined inline in builder chain
- Requires walking syntax tree from `RunAsync()` back through builder
- Handler is a lambda expression in the source
- Complex nested structures (`WithGroupPrefix`)

### 2. Attributed Routes

```csharp
[NuruRoute("greet", Description = "Greet someone")]
public sealed class GreetQuery : IQuery<Unit>
{
    [Parameter(Description = "Name to greet")]
    public string Name { get; set; } = string.Empty;

    public sealed class Handler : IQueryHandler<GreetQuery, Unit>
    {
        public ValueTask<Unit> Handle(GreetQuery query, CancellationToken ct)
        {
            // ...
        }
    }
}
```

**Characteristics:**
- Routes defined via attributes on classes
- Easy to locate — just find `[NuruRoute]` attribute
- Message type inferred from interface (`IQuery`, `ICommand`, etc.)
- Grouping via base class with `[NuruRouteGroup]`
- Parameters/Options are attributed properties

---

## Locators

Locators find specific syntax elements. One locator per DSL element.

### Fluent DSL Locators

```
generators/locators/
├── run-async-locator.cs              # Find app.RunAsync(...) call sites
├── create-builder-locator.cs         # Find NuruApp.CreateBuilder(...)
├── map-locator.cs                    # Find .Map("pattern") calls
├── with-handler-locator.cs           # Find .WithHandler(...) calls
├── with-description-locator.cs       # Find .WithDescription(...) calls
├── with-group-prefix-locator.cs      # Find .WithGroupPrefix(...) calls
├── with-alias-locator.cs             # Find .WithAlias(...) calls
├── as-query-locator.cs               # Find .AsQuery() calls
├── as-command-locator.cs             # Find .AsCommand() calls
├── as-idempotent-command-locator.cs  # Find .AsIdempotentCommand() calls
├── add-behavior-locator.cs           # Find .AddBehavior(...) calls
├── add-help-locator.cs               # Find .AddHelp(...) calls
├── add-repl-locator.cs               # Find .AddRepl(...) calls
├── add-configuration-locator.cs      # Find .AddConfiguration() calls
├── configure-services-locator.cs     # Find .ConfigureServices(...) calls
├── use-terminal-locator.cs           # Find .UseTerminal(...) calls
├── with-name-locator.cs              # Find .WithName(...) calls
├── with-description-locator.cs       # Find .WithDescription(...) calls
├── with-ai-prompt-locator.cs         # Find .WithAiPrompt(...) calls
└── done-locator.cs                   # Find .Done() calls (scope tracking)
```

### Attributed Route Locators

```
generators/locators/
├── attributed-route-locator.cs       # Find classes with [NuruRoute]
├── route-group-locator.cs            # Find classes with [NuruRouteGroup]
├── parameter-attribute-locator.cs    # Find properties with [Parameter]
└── option-attribute-locator.cs       # Find properties with [Option]
```

**Note:** For attributed routes, locating and extracting are nearly the same operation because attribute arguments are immediately available. The line between locator and extractor is blurry here.

---

## Extractors

Extractors parse located syntax into model objects.

```
generators/extractors/
├── fluent-chain-extractor.cs         # Walk builder chain, build AppModel
├── route-extractor.cs                # Extract single route from .Map() chain
├── handler-extractor.cs              # Extract handler lambda/method info
├── parameter-extractor.cs            # Extract route parameters from pattern
├── service-extractor.cs              # Extract services from ConfigureServices
├── behavior-extractor.cs             # Extract behaviors with ordering
├── metadata-extractor.cs             # Extract name, description, aiPrompt
├── attributed-route-extractor.cs     # Extract route from [NuruRoute] class
└── intercept-site-extractor.cs       # Extract file/line/column for interceptor
```

### Fluent DSL Extraction Flow

```
RunAsync() call site
       │
       ▼
   Trace back to 'app' variable
       │
       ▼
   Find assignment from .Build()
       │
       ▼
   Walk builder chain upward to CreateBuilder()
       │
       ▼
   For each .Map() encountered:
       ├── Extract route pattern string
       ├── Find .WithHandler() → extract handler
       ├── Find .WithDescription() → extract description
       ├── Find .AsQuery()/.AsCommand()/etc. → extract message type
       ├── Find .WithAlias() → extract aliases
       └── Track .WithGroupPrefix() scope for prefixes
       │
       ▼
   Build AppModel with all routes
```

### Attributed Route Extraction

```
Find all [NuruRoute] classes in assembly
       │
       ▼
   For each class:
       ├── Read route pattern from attribute
       ├── Read description from attribute
       ├── Check base class for [NuruRouteGroup] → prefix
       ├── Check implemented interface → message type
       │     ├── IQuery<T> → Query
       │     ├── ICommand<T> → Command
       │     └── IIdempotentCommand<T> → IdempotentCommand
       ├── Find [Parameter] properties → route parameters
       ├── Find [Option] properties → options
       └── Find nested Handler class → handler reference
       │
       ▼
   Merge with fluent routes into AppModel
```

---

## Models (Intermediate Representation)

Models are the data structures passed from extractors to emitters.

```
generators/models/
├── app-model.cs                # Top-level application model
├── route-model.cs              # Single route definition
├── route-parameter-model.cs    # Parameter from route pattern
├── option-model.cs             # Option (--flag) definition
├── handler-model.cs            # Handler reference (lambda or method)
├── group-model.cs              # Route group with prefix
├── behavior-model.cs           # Pipeline behavior with order
├── service-model.cs            # Registered service
├── help-model.cs               # Help configuration
├── repl-model.cs               # REPL configuration
├── metadata-model.cs           # App name, description, aiPrompt
└── intercept-site-model.cs     # File/line/column for [InterceptsLocation]
```

### AppModel Structure

```csharp
namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Complete application model extracted from DSL and attributes.
/// This is the IR passed to emitters.
/// </summary>
public sealed class AppModel
{
    // Metadata
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? AiPrompt { get; init; }
    
    // Features
    public bool HasHelp { get; init; }
    public HelpModel? HelpOptions { get; init; }
    public bool HasRepl { get; init; }
    public ReplModel? ReplOptions { get; init; }
    public bool HasConfiguration { get; init; }
    
    // Routes (merged from fluent + attributed)
    public IReadOnlyList<RouteModel> Routes { get; init; } = [];
    
    // Pipeline
    public IReadOnlyList<BehaviorModel> Behaviors { get; init; } = [];
    
    // Services
    public IReadOnlyList<ServiceModel> Services { get; init; } = [];
    
    // Interception target
    public InterceptSiteModel InterceptSite { get; init; } = null!;
}
```

### RouteModel Structure

```csharp
public sealed class RouteModel
{
    public string Pattern { get; init; } = "";           // "admin config get {key}"
    public string? Description { get; init; }
    public MessageType MessageType { get; init; }        // Query, Command, IdempotentCommand
    public HandlerModel Handler { get; init; } = null!;
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public IReadOnlyList<RouteParameterModel> Parameters { get; init; } = [];
    public IReadOnlyList<OptionModel> Options { get; init; } = [];
    public RouteSource Source { get; init; }             // Fluent or Attributed
}

public enum MessageType { Unspecified, Query, Command, IdempotentCommand }
public enum RouteSource { Fluent, Attributed }
```

### HandlerModel Structure

```csharp
public sealed class HandlerModel
{
    public HandlerKind Kind { get; init; }               // Lambda, Method, MediatorHandler
    public string? LambdaSource { get; init; }           // For inline lambdas
    public string? TypeName { get; init; }               // For Mediator handlers
    public string? MethodName { get; init; }             // For method references
    public IReadOnlyList<HandlerParameterModel> Parameters { get; init; } = [];
    public string ReturnType { get; init; } = "string";
    public bool IsAsync { get; init; }
}

public enum HandlerKind { Lambda, Method, MediatorHandler }
```

### InterceptSiteModel Structure

```csharp
public sealed class InterceptSiteModel
{
    public string FilePath { get; init; } = "";
    public int Line { get; init; }
    public int Column { get; init; }
}
```

---

## Emitters

Emitters transform the `AppModel` into C# source code.

```
generators/emitters/
├── interceptor-emitter.cs            # Main RunAsync interceptor
├── route-matcher-emitter.cs          # Pattern matching code for routes
├── handler-invoker-emitter.cs        # Handler invocation code
├── service-resolver-emitter.cs       # Service resolution code
├── capabilities-emitter.cs           # --capabilities JSON response
├── help-emitter.cs                   # --help output generation
└── version-emitter.cs                # --version output
```

### Generated Code Structure

The emitter produces a single generated file with:

```csharp
// <auto-generated/>
#nullable enable

namespace TimeWarp.Nuru.Generators;

using System.Runtime.CompilerServices;

internal static class GeneratedRuntime
{
    [InterceptsLocation("Program.cs", line: 15, column: 16)]
    internal static async Task<int> RunAsync_Generated(this NuruCoreApp app, string[] args)
    {
        // Built-in flags
        if (args is ["--help"]) 
        { 
            PrintHelp(app.Terminal);
            return 0; 
        }
        if (args is ["--version"]) 
        { 
            PrintVersion(app.Terminal);
            return 0; 
        }
        if (args is ["--capabilities"]) 
        { 
            PrintCapabilities(app.Terminal);
            return 0; 
        }
        
        // Route matching (generated from RouteModel[])
        if (args is ["status"])
        {
            // Service resolution (generated)
            var logger = app.LoggerFactory?.CreateLogger<StatusHandler>() 
                ?? NullLogger<StatusHandler>.Instance;
            
            // Handler invocation (generated)
            var result = ((ILogger<StatusHandler> logger) => { 
                logger.LogInformation("Status checked");
                return "healthy"; 
            })(logger);
            
            app.Terminal.WriteLine(result);
            return 0;
        }
        
        if (args is ["greet", var name])
        {
            // Mediator handler invocation (for attributed routes)
            var request = new GreetQuery { Name = name };
            await app.Mediator.Send(request);
            return 0;
        }
        
        // No match
        app.Terminal.WriteLine("Unknown command. Use --help for usage.");
        return 1;
    }
    
    private static void PrintHelp(ITerminal terminal)
    {
        // Generated help text
    }
    
    private static void PrintVersion(ITerminal terminal)
    {
        // Generated version info
    }
    
    private static void PrintCapabilities(ITerminal terminal)
    {
        // Generated JSON capabilities
    }
    
    private static readonly CapabilitiesResponse Capabilities = new()
    {
        Name = "my app",
        Description = "Does Cool Things",
        // ...
    };
}
```

---

## Attributed Routes: Attribute Reference

| Attribute          | Target     | Purpose                        | Properties                    |
| ------------------ | ---------- | ------------------------------ | ----------------------------- |
| `[NuruRoute]`        | Class      | Define route pattern           | Pattern, Description          |
| `[NuruRouteGroup]`   | Class      | Prefix for derived routes      | Prefix                        |
| `[NuruRouteAlias]`   | Class      | Alternate route pattern        | Pattern                       |
| `[Parameter]`        | Property   | Positional parameter           | Description, IsCatchAll       |
| `[Option]`           | Property   | Named option (--flag)          | LongName, ShortName, Description |

### Message Type Inference

Message type is inferred from the interface implemented by the route class:

| Interface               | Message Type         | AI Safety              |
| ----------------------- | -------------------- | ---------------------- |
| `IQuery<T>`               | Query                | Safe to run freely     |
| `ICommand<T>`             | Command              | Confirm before running |
| `IIdempotentCommand<T>`   | IdempotentCommand    | Safe to retry          |
| (none of the above)     | Unspecified          | Treated as Command     |

### Route Grouping via Inheritance

```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

[NuruRoute("run", Description = "Run a container")]
public sealed class DockerRunCommand : DockerGroupBase, ICommand<Unit>
{
    // Route pattern becomes: "docker run"
}
```

---

## Generator Entry Point

The main incremental generator orchestrates the pipeline:

```csharp
[Generator]
public sealed class NuruGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Locate RunAsync call sites
        var runAsyncCalls = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: RunAsyncLocator.IsPotentialMatch,
                transform: RunAsyncLocator.GetCallSite
            );
        
        // 2. Locate attributed routes
        var attributedRoutes = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TimeWarp.Nuru.NuruRouteAttribute",
                predicate: (node, _) => node is ClassDeclarationSyntax,
                transform: AttributedRouteExtractor.Extract
            );
        
        // 3. Combine and extract full AppModel
        var appModel = runAsyncCalls
            .Combine(attributedRoutes.Collect())
            .Select((data, ct) => FluentChainExtractor.Extract(data.Left, data.Right, ct));
        
        // 4. Emit generated code
        context.RegisterSourceOutput(appModel, (ctx, model) =>
        {
            var source = InterceptorEmitter.Emit(model);
            ctx.AddSource("NuruGenerated.g.cs", source);
        });
    }
}
```

---

## Merging Fluent and Attributed Routes

Both sources contribute to the same `AppModel.Routes` collection:

1. Extract fluent routes from builder chain
2. Extract attributed routes from `[NuruRoute]` classes
3. Merge into single list
4. Check for conflicts (duplicate patterns → diagnostic error)
5. Sort by specificity for matching order

**Conflict Example:**
```csharp
// Fluent
.Map("status").WithHandler(() => "fluent")

// Attributed
[NuruRoute("status")]
public class StatusQuery { }

// → NURU002: Duplicate route pattern "status"
```

---

## Compile-Time Diagnostics

| Code    | Severity | Description                                |
| ------- | -------- | ------------------------------------------ |
| NURU001 | Error    | Handler parameter not bound                |
| NURU002 | Error    | Duplicate route pattern                    |
| NURU003 | Warning  | Route has no description                   |
| NURU004 | Error    | Service not registered                     |
| NURU005 | Error    | RunAsync not found for builder             |
| NURU006 | Warning  | Message type not specified                 |
| NURU007 | Error    | Invalid route pattern syntax               |
| NURU008 | Error    | Handler not found for attributed route     |
| NURU009 | Warning  | Attributed route class should be sealed    |

---

## Implementation Phases

### Phase 1: Minimal End-to-End

**Goal:** Single fluent route working with interceptor

**Scope:**
- `run-async-locator.cs`
- `create-builder-locator.cs`
- `map-locator.cs`
- `with-handler-locator.cs`
- `fluent-chain-extractor.cs` (minimal)
- `interceptor-emitter.cs` (minimal)
- `AppModel`, `RouteModel`, `InterceptSiteModel`

**Test:** `dsl-example.cs` runs with `status` argument

### Phase 2: Multiple Fluent Routes

**Goal:** All fluent routes with grouping

**Scope:**
- All fluent DSL locators
- Full `fluent-chain-extractor.cs`
- `route-matcher-emitter.cs`
- `GroupModel`

### Phase 3: Attributed Routes

**Goal:** Merge attributed routes with fluent

**Scope:**
- `attributed-route-locator.cs`
- `attributed-route-extractor.cs`
- Route merging logic
- Conflict detection

### Phase 4: Parameters and Options

**Goal:** Route parameters and options binding

**Scope:**
- `RouteParameterModel`, `OptionModel`
- `parameter-extractor.cs`
- `handler-invoker-emitter.cs`

### Phase 5: Service Injection

**Goal:** Handler service resolution

**Scope:**
- `ServiceModel`
- `service-extractor.cs`
- `service-resolver-emitter.cs`

### Phase 6: Built-in Features

**Goal:** Help, version, capabilities

**Scope:**
- `help-emitter.cs`
- `version-emitter.cs`
- `capabilities-emitter.cs`

### Phase 7: Behaviors/Middleware

**Goal:** Pipeline execution

**Scope:**
- `BehaviorModel`
- `behavior-extractor.cs`
- Pipeline wrapping in emitter

### Phase 8: REPL Support

**Goal:** Interactive mode

**Scope:**
- `ReplModel`
- REPL loop generation

---

## Open Questions

### To Investigate Before Implementation

1. **Route pattern syntax** — What patterns are supported? (`{param}`, `{param:int}`, `{param?}`, `{*catchAll}`)

2. **Existing generator code** — What can be reused from current implementation?

3. **Handler lambda capture** — How to emit the lambda body in generated code? Copy source text or reference?

4. **Async handlers** — How to detect and handle `Task<T>` returns?

5. **Complex return types** — Serialize to JSON? Use ToString()?

---

## References

- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs` — Fluent DSL reference
- `samples/attributed-routes/` — Attributed routes examples
- `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md` — DSL specification
- `.agent/workspace/2024-12-25T01-00-00_v2-generator-architecture.md` — Architecture overview
