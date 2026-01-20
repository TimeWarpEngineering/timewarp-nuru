# TimeWarp.Nuru Source Generator Architecture

**Presentation Notes for 20-minute Technical Talk**

---

## Executive Summary

TimeWarp.Nuru uses a **semantic-first source generator architecture** that transforms a fluent DSL into a high-performance CLI application. The key innovation is a **DSL Interpreter** that semantically "executes" your code at compile time, naturally handling all syntactic variations without fragile expression tree parsing.

---


## 1. The Big Picture: Source Generation Flow

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         YOUR NURU DSL CODE                                   │
│                                                                             │
│   NuruApp app = NuruApp.CreateBuilder(args)                            │
│     .Map("ping").WithHandler(() => "pong").Done()                          │
│     .Map("echo {message:string}").WithHandler(...)                         │
│     .Build();                                                              │
│   await app.RunAsync(args);                                                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                    ┌───────────────┴───────────────┐
                    ▼                               ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  STEP 1: LOCATORS                    STEP 2: EXTRACTORS                    │
│  ─────────────────                   ──────────────────                    │
│  Fast syntactic filter               Pull semantic data                    │
│  ~10ns per node                      using SemanticModel                   │
│                                      Extract domain objects                 │
│  Purpose:                            Purpose:                              │
│  "Is this a Nuru call?"              "What does this call mean?"           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  STEP 3: INTERPRETER                                                         │
│  ──────────────────                                                         │
│  Semantic DSL execution                                                     │
│  Walks statements, tracks variables                                         │
│  Dispatches to IR builders                                                  │
│                                                                             │
│  Purpose: "Execute" DSL semantically                                        │
│  ════════════════════════════                                               │
│  var app = NuruApp.CreateBuilder()  ──►  Creates IrAppBuilder              │
│  .Map("ping")                       ──►  Creates IrRouteBuilder            │
│  .WithHandler(...)                  ──►  Populates handler                 │
│  .Build()                           ──►  Finalizes app                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  STEP 4: IR BUILDERS                  STEP 5: MODELS                        │
│  ───────────────────                  ──────────────                        │
│  Mirror DSL structure                 Immutable data structures            │
│  Accumulate state                     Complete app model                    │
│  CRTP fluent chaining                 Ready for emission                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  STEP 6: EMITTERS                                                           │
│  ────────────────                                                           │
│  Generate optimized C# code                                                 │
│  Route matching, handlers, help, services                                  │
│  Output: NuruGenerated.g.cs                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         GENERATED CODE (at compile time)                    │
│                                                                             │
│   file static partial class GeneratedInterceptor {                         │
│     [InterceptsLocation(1, "encoded-data")]                                │
│     public static async Task<int> RunAsync_Intercepted(...) {              │
│       if (args is ["ping"]) {                                              │
│         var result = "pong";                                               │
│         app.Terminal.WriteLine(result);                                    │
│         return 0;                                                          │
│       }                                                                    │
│       // ... more routes ...                                               │
│     }                                                                       │
│   }                                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---


## 2. Component Order & Directory Structure

```
source/timewarp-nuru-analyzers/generators/
│
├── 🔍 locators/          [STEP 1] Fast syntactic filtering
│   ├── build-locator.cs
│   ├── map-locator.cs
│   ├── run-async-locator.cs
│   └── nuru-route-attribute-locator.cs
│   └── ...20+ more locators
│
├── 📦 extractors/        [STEP 2] Pull semantic information
│   ├── app-extractor.cs
│   ├── handler-extractor.cs
│   ├── service-extractor.cs
│   └── pattern-string-extractor.cs
│
├── 🧠 interpreter/       [STEP 3] Semantic DSL execution
│   └── dsl-interpreter.cs     ← THE CORE INNOVATION
│
├── 🏗️  ir-builders/       [STEP 4] Accumulate state (DSL mirrors)
│   ├── ir-app-builder.cs
│   ├── ir-route-builder.cs
│   └── ir-group-builder.cs
│
├── 📋 models/            [STEP 5] Immutable data structures
│   ├── app-model.cs
│   ├── route-definition.cs
│   └── handler-definition.cs
│
├── 💡 emitters/          [STEP 6] Generate C# source
│   ├── interceptor-emitter.cs
│   ├── route-matcher-emitter.cs
│   ├── handler-invoker-emitter.cs
│   └── help-emitter.cs
│
└── ⚙️  nuru-generator.cs  [ORCHESTRATION] Entry point
```

### Flow Visualization

```
┌────────────┐    ┌────────────┐    ┌────────────┐
│  locators  │───►│ extractors │───►│ interpreter│
│  (syntactic│    │ (semantic  │    │ (semantic  │
│   filter)  │    │  extract)  │    │ execution) │
└────────────┘    └────────────┘    └────────────┘
                                            │
                                            ▼
┌────────────┐    ┌────────────┐    ┌────────────┐
│  emitters  │◄───│   models   │◄───│ ir-builders│
│  (generate │    │ (data      │    │ (accumulate│
│   code)    │    │   models)  │    │   state)   │
└────────────┘    └────────────┘    └────────────┘
```

---


## 3. Semantic vs Syntactic Evaluation

### The Two-Phase Filtering Pattern

```csharp
// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PHASE 1: SYNTACTIC - Fast, ~10 nanoseconds                              │
// │ ─────────────────────────────────────────────────────────────────────── │
// │ Just check the SHAPE of the code, no type resolution needed             │
// └─────────────────────────────────────────────────────────────────────────┘

public static bool IsPotentialMatch(RoslynSyntaxNode node)
{
  if (node is not InvocationExpressionSyntax invocation)
    return false;
  if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
    return false;
  // Only checking: "Does this LOOK like a .Build() call?"
  return memberAccess.Name.Identifier.ValueText == "Build";
}

// ┌─────────────────────────────────────────────────────────────────────────┐
// │ PHASE 2: SEMANTIC - Slower, but tells us WHAT IT IS                     │
// │ ─────────────────────────────────────────────────────────────────────── │
// │ Uses SemanticModel to resolve types and symbols                         │
// └─────────────────────────────────────────────────────────────────────────┘

public static bool IsConfirmedBuildCall(
  InvocationExpressionSyntax invocation,
  SemanticModel semanticModel,
  CancellationToken cancellationToken)
{
  if (!IsPotentialMatch(invocation))  // Quick reject
    return false;

  // NOW we do expensive semantic analysis
  SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(invocation, cancellationToken);
  if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
    return false;

  // Critical: Verify it's OUR NuruApp, not some other Build() method
  return methodSymbol.ReturnType.Name == "NuruApp";
}
```

### Why Prefer Semantic Over Syntactic?

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                          SYNTACTIC EVALUATION                                │
│  ───────────────────────────                                                │
│  • Fragile - breaks on code style changes                                   │
│  • Cannot handle:                                                           │
│    - Renames/aliases                                                        │
│    - Different formatting                                                   │
│    - Partial statements                                                     │
│  • Example: var b = app.Build();  vs  app.Build();                         │
└──────────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────────┐
│                          SEMANTIC EVALUATION                                 │
│  ────────────────────────                                                   │
│  • Robust - works regardless of code style                                  │
│  • Uses SemanticModel to:                                                   │
│    - Resolve identifier to declaration                                      │
│    - Get type information                                                   │
│    - Track variable state                                                   │
│  • All these are EQUIVALENT semantically:                                    │
│                                                                              │
│    // Style 1: Fluent                                                       │
│    app.Map("ping").WithHandler(...).Done()                                  │
│                                                                              │
│    // Style 2: Variables                                                    │
│    var route = app.Map("ping");                                             │
│    route.WithHandler(...);                                                  │
│    route.Done();                                                            │
│                                                                              │
│    // Style 3: Fragmented                                                   │
│    app.Map("ping").WithHandler(...);                                        │
│    app.Build();                                                             │
└──────────────────────────────────────────────────────────────────────────────┘
```

### The Semantic DSL Interpreter

```csharp
// This is THE KEY innovation - it "executes" your DSL at compile time

public class DslInterpreter
{
  private readonly SemanticModel _semanticModel;
  private readonly VariableState _variables;  // ISymbol → IR object
  private readonly List<IrAppBuilder> _builtApps;

  public void Interpret(SyntaxNode block)
  {
    foreach (var statement in block.ChildNodes())
    {
      switch (statement)
      {
        case LocalDeclarationStatementSyntax localDecl:
          ProcessVariableDeclaration(localDecl);
          break;

        case ExpressionStatementSyntax expr:
          ProcessExpression((ExpressionStatementSyntax)expr);
          break;
      }
    }
  }

  private void ProcessExpression(ExpressionStatementSyntax expression)
  {
    var invocation = (InvocationExpressionSyntax)expression.Expression;
    var methodName = GetMethodName(invocation);

    switch (methodName)
    {
      case "Map":
        DispatchMap(invocation);
        break;

      case "WithHandler":
        DispatchWithHandler(invocation);
        break;

      case "Done":
        DispatchDone(invocation);
        break;

      case "Build":
        DispatchBuild(invocation);
        break;

      case "RunAsync":
        DispatchRunAsync(invocation);
        break;
    }
  }

  private void DispatchMap(InvocationExpressionSyntax invocation)
  {
    // Look up the app variable (semantic!)
    var appBuilder = _variables.Resolve<IBuilder>("app");

    // Parse the pattern string
    var patternArg = invocation.ArgumentList.Arguments[0];
    var pattern = _semanticModel.GetConstantValue(patternArg.Expression);

    // Create IR route builder
    var routeBuilder = appBuilder.Map(pattern);

    // Track the route builder for subsequent .WithHandler() etc.
    _variables["currentRoute"] = routeBuilder;
  }
}
```

---


## 4. Demonstration Example: Ping-Pong

**Source file:** `samples/01-hello-world/01-hello-world-lambda.cs`

```csharp
#!/usr/bin/dotnet --
using TimeWarp.Nuru;

NuruApp app = NuruApp.CreateBuilder(args)
  .Map("")
    .WithHandler(() => "Hello World")
    .AsQuery()
    .Done()
  .Build();

await app.RunAsync(args);
```

### What the Generator Sees (Step by Step)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 1: LOCATOR finds "CreateBuilder" call                                  │
│ ─────────────────────────────────────────────────────────────────────────   │
│ IsPotentialMatch(node) → true                                              │
│   └─► Returns: Method name = "CreateBuilder", type = NuruApp               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 2: EXTRACTOR creates AppContext                                        │
│ ─────────────────────────────────────────────────────────────────────────   │
│ Extracts:                                                                   │
│   - Variable name: "app"                                                   │
│   - Builder type: NuruApp                                                  │
│   - Initialized from: CreateBuilder(args)                                  │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 3: INTERPRETER "walks" the DSL                                        │
│ ─────────────────────────────────────────────────────────────────────────   │
│ Statement 1: var app = NuruApp.CreateBuilder(args)                         │
│   └─► VariableState["app"] = IrAppBuilder instance                         │
│                                                                             │
│ Statement 2: .Map("")                                                       │
│   └─► DispatchMap() → app.Map("")                                          │
│   └─► VariableState["currentRoute"] = IrRouteBuilder("")                   │
│                                                                             │
│ Statement 3: .WithHandler(() => "Hello World")                             │
│   └─► DispatchWithHandler()                                                │
│   └─► Extracts lambda: () => "Hello World"                                 │
│   └─► IrRouteBuilder.Handler = InlinedHandler("Hello World")               │
│                                                                             │
│ Statement 4: .Done()                                                        │
│   └─► DispatchDone()                                                       │
│   └─► IrRouteBuilder ──► RouteDefinition("")                               │
│   └─► app.Routes.Add(RouteDefinition)                                      │
│                                                                             │
│ Statement 5: .Build()                                                       │
│   └─► DispatchBuild()                                                      │
│   └─► app.FinalizeModel() ──► AppModel                                     │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 4: IR BUILDERS accumulate state                                        │
│ ─────────────────────────────────────────────────────────────────────────   │
│ IrAppBuilder {                                                               │
│   Name: "App"                                                               │
│   Routes: [                                                                │
│     RouteDefinition {                                                       │
│       Pattern: ""                                                           │
│       Segments: []                                                          │
│       Handler: InlinedHandler {                                             │
│         ReturnType: string                                                  │
│         Expression: "Hello World"                                          │
│       }                                                                     │
│       Description: null                                                     │
│       MessageType: Query                                                    │
│     }                                                                       │
│   ]                                                                          │
│   Services: []                                                              │
│   Behaviors: []                                                             │
│ }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 5: MODELS create immutable AppModel                                    │
│ ─────────────────────────────────────────────────────────────────────────   │
│ AppModel {                                                                  │
│   Name: "Application"                                                       │
│   Routes: ImmutableArray<RouteDefinition>                                  │
│   InterceptSitesByMethod: {                                                 │
│     "RunAsync": InterceptSiteModel { ... }                                 │
│   }                                                                         │
│ }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ STEP 6: EMITTERS generate C# code                                           │
│ ─────────────────────────────────────────────────────────────────────────   │
│ OUTPUT: NuruGenerated.g.cs                                                  │
│                                                                             │
│ namespace TimeWarp.Nuru.Generated {                                        │
│   file static partial class GeneratedInterceptor {                         │
│     [InterceptsLocation(1, "...")]                                         │
│     public static async Task<int> RunAsync_Intercepted(                    │
│       string[] args,                                                        │
│       ITerminal terminal,                                                   │
│       TimeWarp.Nuru.Services.ServiceResolver serviceResolver,              │
│       CancellationToken cancellationToken)                                 │
│     {                                                                       │
│       if (args.Length == 0) {                                              │
│         var result = "Hello World";                                        │
│         terminal.WriteLine(result);                                        │
│         return 0;                                                          │
│       }                                                                     │
│       return 1;  // No route matched                                       │
│     }                                                                       │
│   }                                                                         │
│ }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Generated Code (Simplified)

```csharp
// This is what gets injected into your assembly at compile time

file static partial class GeneratedInterceptor
{
  [InterceptsLocation(
    location: 1,  // Points to the RunAsync call in source
    "encoded-build-data-here"
  )]
  public static async Task<int> RunAsync_Intercepted(
    string[] args,
    ITerminal terminal,
    ServiceResolver serviceResolver,
    CancellationToken cancellationToken)
  {
    // Route matching with list patterns (C# 11+)
    if (args is [])
    {
      // Inlined handler - no reflection, no delegates
      var result = "Hello World";
      terminal.WriteLine(result);
      return 0;
    }

    if (args is ["help"] or ["h"] or ["?"])
    {
      // Help is auto-generated from AppModel
      terminal.WriteLine("Usage: app [command] [arguments]");
      terminal.WriteLine();
      terminal.WriteLine("Commands:");
      terminal.WriteLine("  <empty>    Hello World");
      return 0;
    }

    return 1;  // Exit code for "no route matched"
  }
}
```

---


## 5. Talking Points Summary

### 1. The Problem We're Solving
- CLI apps are tedious to write
- Manual routing, argument parsing, help generation
- Runtime reflection has overhead
- **Solution: Generate all this at compile time**

### 2. The Key Insight
> "Instead of parsing expression trees (syntactic), let's semantically INTERPRET the DSL"

This means:
- Code style doesn't matter
- Fragments, variables, fluent calls all work
- Compile-time safety

### 3. The Flow
1. **Locators** - Quick filter: "Is this Nuru code?"
2. **Extractors** - Pull semantic data: "What is this?"
3. **Interpreter** - Execute DSL: "What does this do?"
4. **IR Builders** - Build representation
5. **Models** - Immutable data
6. **Emitters** - Generate optimized C#

### 4. Why Semantic Over Syntactic?
- Syntactic: Breaks on whitespace, renaming, code reformatters
- Semantic: Uses Roslyn's SemanticModel to understand meaning
- Robust against code style variations

### 5. Benefits
- **Zero runtime reflection** - Everything generated at compile time
- **Compile-time errors** - Invalid routes caught before running
- **Performance** - if/goto routing with list patterns
- **IDE support** - Full IntelliSense on generated code

---


## 6. Key Files to Reference

| File | Purpose |
|------|---------|
| `generators/nuru-generator.cs` | Entry point, orchestrates pipeline |
| `generators/interpreter/dsl-interpreter.cs` | Core semantic execution engine |
| `generators/locators/build-locator.cs` | Example of two-phase filtering |
| `generators/emitters/route-matcher-emitter.cs` | Generates if/goto routing |
| `samples/01-hello-world/01-hello-world-lambda.cs` | Simplest working example |

---


## 7. Sample Demo Script

```bash
# Show the source
cat samples/01-hello-world/01-hello-world-lambda.cs

# Run it
dotnet run --project samples/01-hello-world/01-hello-world-lambda.cs -- --help

# Show generated code
# Find in: artifacts/generated/timewarp-nuru-analyzers/NuruGenerated.g.cs
```

---


## References

- **Design Document:** `kanban/to-do/277/epic-semantic-dsl-interpreter-with-mirrored-ir-builders.md`
- **Error Handling:** `documentation/developer/design/cross-cutting/error-handling.md`
- **Syntax Documentation:** Available via `TimeWarp_Nuru_Mcp_get_syntax` tool
- **Examples:** Available via `TimeWarp_Nuru_Mcp_get_example` tool
