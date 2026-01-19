# TimeWarp.Nuru Source Generator Architecture

## Executive Summary

The source generator uses a **Locate → Extract → Interpret → Build IR → Emit** pipeline to transform user DSL code into optimized interceptor code at compile time. The architecture separates concerns into distinct layers: locators find syntax, extractors pull semantic data, IR builders accumulate state, and emitters produce C# source.

## Directory Structure

```
./source/timewarp-nuru-analyzers/generators/
├── locators/       # Find specific syntax patterns
├── extractors/     # Extract semantic information
├── interpreter/    # Walk DSL statements semantically
├── ir-builders/    # Accumulate state (Intermediate Representation)
├── models/         # Immutable data structures
├── emitters/       # Generate C# source code
└── nuru-generator.cs  # Entry point & pipeline orchestration
```

## Pipeline Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                          USER SOURCE CODE                                    │
│  NuruApp.CreateBuilder()                                                    │
│    .Map("ping").WithHandler(() => "pong").Done()                           │
│    .Build().RunAsync(args);                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  1. LOCATORS - Fast syntactic filtering                                     │
│                                                                              │
│  RunAsyncLocator.IsPotentialMatch(node)                                     │
│    → Is this an invocation of "RunAsync"?                                   │
│                                                                              │
│  ForAttributeWithMetadataName("NuruRouteAttribute")                         │
│    → Is this a class with [NuruRoute]?                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  2. EXTRACTORS - Pull semantic information                                   │
│                                                                              │
│  AppExtractor.Extract(context)                                              │
│    → Find containing block or top-level statements                          │
│    → Delegate to DslInterpreter                                             │
│                                                                              │
│  AttributedRouteExtractor.Extract(classDeclaration)                         │
│    → Extract route from [NuruRoute("pattern")] class                        │
│                                                                              │
│  HandlerExtractor.Extract(invocation)                                       │
│    → Extract handler from .WithHandler(() => ...)                           │
│                                                                              │
│  InterceptSiteExtractor.Extract(invocation)                                 │
│    → Get InterceptableLocation for .RunAsync() call                         │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  3. INTERPRETER - Semantic DSL execution                                     │
│                                                                              │
│  DslInterpreter.Interpret(block)                                            │
│    │                                                                         │
│    ├── ProcessStatement(LocalDeclarationStatement)                          │
│    │     var app = NuruApp.CreateBuilder()  → Creates IrAppBuilder          │
│    │                                                                         │
│    ├── EvaluateInvocation(".Map(pattern)")                                  │
│    │     → DispatchMap() → IrAppBuilder.Map() → IrRouteBuilder              │
│    │                                                                         │
│    ├── EvaluateInvocation(".WithHandler(lambda)")                           │
│    │     → DispatchWithHandler() → HandlerExtractor → IrRouteBuilder        │
│    │                                                                         │
│    ├── EvaluateInvocation(".Done()")                                        │
│    │     → DispatchDone() → Finalizes route, returns to parent              │
│    │                                                                         │
│    ├── EvaluateInvocation(".Build()")                                       │
│    │     → DispatchBuild() → Marks app as built                             │
│    │                                                                         │
│    └── EvaluateInvocation(".RunAsync()")                                    │
│          → DispatchRunAsyncCall() → Captures InterceptSiteModel             │
│                                                                              │
│  State Tracking:                                                             │
│    VariableState = { symbol → IR object }                                   │
│    BuiltApps = [IrAppBuilder, ...]                                          │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  4. IR BUILDERS - Accumulate state                                           │
│                                                                              │
│  IrAppBuilder                                                                │
│    ├── Name, Description, AiPrompt                                          │
│    ├── Routes: List<RouteDefinition>                                        │
│    ├── Services: List<ServiceDefinition>                                    │
│    ├── Behaviors: List<BehaviorDefinition>                                  │
│    ├── InterceptSites: List<InterceptSiteModel>                             │
│    └── FinalizeModel() → AppModel                                           │
│                                                                              │
│  IrRouteBuilder                                                              │
│    ├── Pattern, Segments                                                     │
│    ├── Handler, Description, Aliases                                        │
│    └── Done() → RouteDefinition → parent.RegisterRoute()                    │
│                                                                              │
│  IrGroupBuilder                                                              │
│    ├── Prefix                                                                │
│    ├── Map() → nested IrRouteBuilder with prefixed pattern                  │
│    └── Done() → returns to parent                                           │
│                                                                              │
│  Interfaces for polymorphic dispatch:                                        │
│    IIrRouteSource  (Map, WithGroupPrefix)                                   │
│    IIrAppBuilder   (WithName, AddHelp, ConfigureServices, etc.)             │
│    IIrRouteBuilder (WithHandler, WithDescription, Done)                     │
│    IIrGroupBuilder (Map, WithGroupPrefix, Done)                             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  5. MODELS - Immutable data structures                                       │
│                                                                              │
│  AppModel                                                                    │
│    └── Complete CLI application definition                                  │
│                                                                              │
│  RouteDefinition                                                             │
│    └── Single route with pattern, handler, options                          │
│                                                                              │
│  HandlerDefinition                                                           │
│    └── Handler kind (Delegate/Command/Method), lambda body, return type     │
│                                                                              │
│  SegmentDefinition                                                           │
│    └── Parsed route segment (literal, parameter, option, catchall)          │
│                                                                              │
│  InterceptSiteModel                                                          │
│    └── File path, line, column for [InterceptsLocation] attribute           │
│                                                                              │
│  ServiceDefinition, BehaviorDefinition, ParameterBinding, etc.              │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  6. EMITTERS - Generate C# source code                                       │
│                                                                              │
│  InterceptorEmitter.Emit(AppModel) → string                                 │
│    ├── Emit namespace, usings, class declaration                            │
│    ├── Emit [InterceptsLocation] attributes for each RunAsync site          │
│    ├── Emit interceptor method signature                                    │
│    │                                                                         │
│    ├── ConfigurationEmitter (if HasConfiguration)                           │
│    │     → Emit appsettings loading, environment detection                  │
│    │                                                                         │
│    ├── RouteMatcherEmitter (for each route)                                 │
│    │     → Emit if/goto pattern matching with list patterns                 │
│    │     → Emit parameter parsing with type conversion                      │
│    │     → Emit option extraction loops                                     │
│    │                                                                         │
│    ├── HandlerInvokerEmitter (for each route)                               │
│    │     → Emit service resolution                                          │
│    │     → Emit handler invocation (delegate/command/method)                │
│    │     → Emit result output                                               │
│    │                                                                         │
│    ├── HelpEmitter (if HasHelp)                                             │
│    │     → Emit --help/--version handling                                   │
│    │                                                                         │
│    ├── CapabilitiesEmitter (always)                                         │
│    │     → Emit --capabilities JSON generation                              │
│    │                                                                         │
│    ├── CheckUpdatesEmitter (if HasCheckUpdatesRoute)                        │
│    │     → Emit GitHub release version checking                             │
│    │                                                                         │
│    └── ServiceResolverEmitter                                               │
│          → Emit Lazy<T> singletons for registered services                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       GENERATED CODE (NuruGenerated.g.cs)                    │
│                                                                              │
│  namespace TimeWarp.Nuru.Generated                                          │
│  {                                                                           │
│    file static class NuruInterceptor                                        │
│    {                                                                         │
│      [InterceptsLocation(1, "encoded-location-data")]                       │
│      public static async Task<int> RunAsync_Intercepted(...)                │
│      {                                                                       │
│        // Route matching with list patterns                                 │
│        if (args is ["ping"])                                                │
│        {                                                                     │
│          string result = "pong";  // Inlined handler                        │
│          app.Terminal.WriteLine(result);                                    │
│          return 0;                                                          │
│        }                                                                     │
│        // More routes...                                                    │
│      }                                                                       │
│    }                                                                         │
│  }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

## Key Design Decisions

### 1. Locators: Two-Phase Filtering

Locators use Roslyn's incremental generator pattern:
- **Phase 1 (Syntactic)**: Fast `IsPotentialMatch()` filters candidates by syntax only
- **Phase 2 (Semantic)**: `Extract()` uses SemanticModel for accurate type checking

This minimizes expensive semantic analysis.

### 2. DslInterpreter: Statement-by-Statement Execution

Rather than pattern-matching on syntax trees, the interpreter "executes" the DSL:
- Walks statements sequentially
- Tracks variable assignments in `VariableState` dictionary
- Dispatches method calls to IR builders based on method name
- Fails fast on unrecognized DSL methods

This handles complex patterns like:
```csharp
var builder = NuruApp.CreateBuilder();
var app = builder.Map("ping").WithHandler(() => "pong").Done().Build();
await app.RunAsync(args);
```

### 3. IR Builders: CRTP Pattern

IR builders use Curiously Recurring Template Pattern for fluent chaining:
```csharp
public class IrAppBuilder<TSelf> : IIrAppBuilder where TSelf : IrAppBuilder<TSelf>
{
  public TSelf WithName(string name) { ... return (TSelf)this; }
}
```

### 4. Marker Interfaces for Polymorphism

Instead of explicit type enumeration, IR builders implement marker interfaces:
- `IIrRouteSource` - can create routes (app, group)
- `IIrAppBuilder` - app-level configuration
- `IIrRouteBuilder` - route configuration
- `IIrGroupBuilder` - group configuration

The interpreter dispatches polymorphically:
```csharp
if (receiver is not IIrRouteSource source) throw ...;
return source.Map(pattern);
```

### 5. Emitters: Composable Code Generation

Each emitter handles one concern:
- `InterceptorEmitter` - orchestrates and emits the main interceptor
- `RouteMatcherEmitter` - pattern matching logic
- `HandlerInvokerEmitter` - handler execution
- `ConfigurationEmitter` - appsettings/environment setup
- etc.

Emitters use `StringBuilder.AppendLine($"...")` for readable template composition.

## Locators Inventory

| Locator | Purpose |
|---------|---------|
| `RunAsyncLocator` | Find `app.RunAsync()` calls (interception entry points) |
| `CreateBuilderLocator` | Find `NuruApp.CreateBuilder()` calls |
| `MapLocator` | Find `.Map(pattern)` calls |
| `WithHandlerLocator` | Find `.WithHandler(lambda)` calls |
| `WithDescriptionLocator` | Find `.WithDescription(text)` calls |
| `DoneLocator` | Find `.Done()` calls |
| `BuildLocator` | Find `.Build()` calls |
| `AddHelpLocator` | Find `.AddHelp()` calls |
| `AddReplLocator` | Find `.AddRepl()` calls |
| `AddConfigurationLocator` | Find `.AddConfiguration()` calls |
| `ConfigureServicesLocator` | Find `.ConfigureServices(lambda)` calls |
| `AddBehaviorLocator` | Find `.AddBehavior(type)` calls |
| `NuruRouteAttributeLocator` | Find `[NuruRoute]` classes |
| `NuruRouteGroupAttributeLocator` | Find `[NuruRouteGroup]` classes |
| ... | Additional locators for options, aliases, etc. |

## Extractors Inventory

| Extractor | Input | Output |
|-----------|-------|--------|
| `AppExtractor` | `GeneratorSyntaxContext` | `AppModel` |
| `AttributedRouteExtractor` | `ClassDeclarationSyntax` | `RouteDefinition` |
| `HandlerExtractor` | `InvocationExpressionSyntax` | `HandlerDefinition` |
| `InterceptSiteExtractor` | `InvocationExpressionSyntax` | `InterceptSiteModel` |
| `PatternStringExtractor` | Route pattern string | `ImmutableArray<SegmentDefinition>` |
| `ServiceExtractor` | ConfigureServices lambda | `ImmutableArray<ServiceDefinition>` |

## Two DSL Styles Supported

### 1. Fluent DSL
```csharp
NuruApp.CreateBuilder()
  .Map("ping").WithHandler(() => "pong").Done()
  .Build().RunAsync(args);
```
Processed by: `DslInterpreter` → `IrAppBuilder` → `IrRouteBuilder`

### 2. Attributed Routes
```csharp
[NuruRoute("greet {name}")]
public class GreetCommand : IRequest<string>
{
  public required string Name { get; init; }
  
  public class Handler : IRequestHandler<GreetCommand, string>
  {
    public Task<string> Handle(GreetCommand request, CancellationToken ct)
      => Task.FromResult($"Hello, {request.Name}!");
  }
}
```
Processed by: `AttributedRouteExtractor` directly → `RouteDefinition`

Both styles are combined in `NuruGenerator.CombineModels()`.
