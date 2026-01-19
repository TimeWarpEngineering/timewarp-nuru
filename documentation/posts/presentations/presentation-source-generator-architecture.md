# Nuru Source Generator Architecture
## Presentation Overview (20 min)

---

## 1. The Generator Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SOURCE CODE (User writes)                          │
│                                                                             │
│   NuruApp.CreateBuilder(args)                                               │
│     .Map("add {x:double} {y:double}")                                       │
│     .WithHandler((double x, double y) => x + y)                             │
│     .Build();                                                               │
│   await app.RunAsync(args);                                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  LOCATORS (Syntactic)      Find Build() and RunAsync() call sites          │
│  ────────────────────      Fast pattern matching, no type resolution       │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  EXTRACTORS (Semantic)     Extract typed information using SemanticModel   │
│  ─────────────────────     Validates types, resolves symbols               │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  INTERPRETER               "Execute" the fluent DSL at compile time        │
│  ───────────────           Track variable state, dispatch to IR builders   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  IR BUILDERS               Build intermediate representation               │
│  ───────────────           Mirror fluent API: Map(), WithHandler(), etc.   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  MODELS                    Immutable DTOs representing the app             │
│  ──────                    AppModel, RouteDefinition, HandlerDefinition    │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  EMITTERS                  Generate C# code from models                    │
│  ────────                  InterceptorEmitter, RouteMatcherEmitter, etc.   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         GENERATED CODE (Output)                             │
│                                                                             │
│   [InterceptsLocation("file.cs", 10, 5)]                                    │
│   static async Task<int> RunAsync_Intercepted(this NuruCoreApp app, ...)   │
│   {                                                                         │
│       // Route matching, type conversion, handler invocation               │
│   }                                                                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Folder Structure

```
source/timewarp-nuru-analyzers/generators/
│
├── nuru-generator.cs          ◄── ENTRY POINT: Orchestrates entire pipeline
│
├── locators/                  ◄── PHASE 1: Find syntax patterns (syntactic)
│   ├── build-locator.cs           Find Build() calls
│   ├── run-async-locator.cs       Find RunAsync() calls
│   └── map-locator.cs             Find Map() calls
│
├── extractors/                ◄── PHASE 2: Extract semantic information
│   ├── app-extractor.cs           Main extraction orchestrator
│   ├── handler-extractor.cs       Extract lambda/method handlers
│   ├── pattern-string-extractor   Parse route patterns
│   └── service-extractor.cs       Extract DI services
│
├── interpreter/               ◄── PHASE 3: "Execute" DSL at compile time
│   └── dsl-interpreter.cs         Walk statements, dispatch to builders
│
├── ir-builders/               ◄── PHASE 4: Build intermediate representation
│   ├── ir-app-builder.cs          Mirror NuruCoreAppBuilder
│   ├── ir-route-builder.cs        Mirror route configuration
│   └── ir-group-builder.cs        Mirror group configuration
│
├── models/                    ◄── PHASE 5: Immutable data transfer objects
│   ├── app-model.cs               Complete app definition
│   ├── route-definition.cs        Single route specification
│   └── generator-model.cs         All apps combined
│
└── emitters/                  ◄── PHASE 6: Generate C# source
    ├── interceptor-emitter.cs     Main entry, InterceptsLocation
    ├── route-matcher-emitter.cs   Route matching logic
    └── handler-invoker-emitter.cs Handler execution
```

---

## 3. Execution Order

```
┌───────────────────────────────────────────────────────────────────┐
│                      NuruGenerator.Initialize()                   │
└───────────────────────────────────────────────────────────────────┘
                                │
        ┌───────────────────────┴───────────────────────┐
        │                                               │
        ▼                                               ▼
┌───────────────────┐                      ┌───────────────────────┐
│  1. LOCATE        │                      │  2. LOCATE            │
│  Build() calls    │                      │  [NuruRoute] classes  │
│  (syntactic)      │                      │  (attribute based)    │
└───────────────────┘                      └───────────────────────┘
        │                                               │
        ▼                                               ▼
┌───────────────────┐                      ┌───────────────────────┐
│  3. EXTRACT       │                      │  4. EXTRACT           │
│  from Build()     │                      │  attributed routes    │
│  AppExtractor     │                      │  AttributedExtractor  │
└───────────────────┘                      └───────────────────────┘
        │                                               │
        └───────────────────────┬───────────────────────┘
                                │
                                ▼
                ┌───────────────────────────┐
                │  5. COMBINE               │
                │  GeneratorModel           │
                │  (all apps + routes)      │
                └───────────────────────────┘
                                │
                                ▼
                ┌───────────────────────────┐
                │  6. VALIDATE              │
                │  Check duplicates,        │
                │  overlapping patterns     │
                └───────────────────────────┘
                                │
                                ▼
                ┌───────────────────────────┐
                │  7. EMIT                  │
                │  InterceptorEmitter       │
                │  → NuruGenerated.g.cs     │
                └───────────────────────────┘
```

---

## 4. Semantic vs Syntactic: Why It Matters

### Syntactic (Locators) - Fast but Imprecise
```
┌─────────────────────────────────────────────────────────────────┐
│  Q: "Is this a Build() call?"                                   │
│                                                                 │
│  Syntactic check:                                               │
│    - Is it an InvocationExpression?            ✓                │
│    - Is the method name "Build"?               ✓                │
│    - Does it have zero arguments?              ✓                │
│                                                 │
│  Result: MAYBE - could be any .Build() method!                  │
│          StringBuilder.Build()? ✓                               │
│          NuruCoreApp.Build()?   ✓                               │
└─────────────────────────────────────────────────────────────────┘
```

### Semantic (Extractors) - Accurate Type Resolution
```
┌─────────────────────────────────────────────────────────────────┐
│  Q: "Is this a NuruCoreAppBuilder.Build() call?"               │
│                                                                 │
│  Semantic check:                                                │
│    - Get symbol from SemanticModel                              │
│    - Check containing type is NuruCoreAppBuilder                │
│    - Verify return type is NuruCoreApp                          │
│                                                                 │
│  Result: DEFINITE - type-safe confirmation                      │
│          StringBuilder.Build()? ✗ (wrong type)                  │
│          NuruCoreApp.Build()?   ✓ (correct!)                    │
└─────────────────────────────────────────────────────────────────┘
```

### The Two-Phase Strategy

```
Phase 1: SYNTACTIC (Locators)
┌─────────────────────────────────────────────────────────────────┐
│  • Cast a WIDE net                                              │
│  • Very fast (no type resolution)                               │
│  • May have false positives                                     │
│  • Runs on every keystroke                                      │
└─────────────────────────────────────────────────────────────────┘
                    │
                    │ Filter candidates
                    ▼
Phase 2: SEMANTIC (Extractors)
┌─────────────────────────────────────────────────────────────────┐
│  • Narrow down with type information                            │
│  • More expensive (requires SemanticModel)                      │
│  • No false positives                                           │
│  • Only runs on syntactic matches                               │
└─────────────────────────────────────────────────────────────────┘

WHY THIS MATTERS FOR INCREMENTAL GENERATORS:
• Syntactic changes invalidate Phase 1 (cheap)
• Only semantic changes invalidate Phase 2 (expensive)
• Result: Fast IDE response times
```

---

## 5. The Interpreter: Compile-Time DSL Execution

```
USER CODE:
┌─────────────────────────────────────────────────────────────────┐
│  var builder = NuruApp.CreateBuilder(args);                     │
│  builder = builder.Map("add {x} {y}");                          │
│  builder = builder.WithHandler((int x, int y) => x + y);        │
│  var app = builder.Build();                                     │
└─────────────────────────────────────────────────────────────────┘

INTERPRETER EXECUTION:
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  VariableState = {}                                             │
│                                                                 │
│  1. var builder = NuruApp.CreateBuilder(args)                   │
│     → VariableState["builder"] = new IrAppBuilder()             │
│                                                                 │
│  2. builder = builder.Map("add {x} {y}")                        │
│     → lookup "builder" in state                                 │
│     → dispatch Map("add {x} {y}") to IrAppBuilder               │
│     → returns IrRouteBuilder (child of app builder)             │
│                                                                 │
│  3. builder = builder.WithHandler(...)                          │
│     → dispatch WithHandler to IrRouteBuilder                    │
│     → extract handler parameters, return type                   │
│                                                                 │
│  4. var app = builder.Build()                                   │
│     → IrAppBuilder.FinalizeModel()                              │
│     → produces AppModel with all routes                         │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## 6. IR Builders: Mirroring the Fluent API

```
REAL API (Runtime):                    IR BUILDER (Compile-time):
┌──────────────────────────┐          ┌──────────────────────────┐
│  NuruCoreAppBuilder      │          │  IrAppBuilder            │
│  ───────────────────     │          │  ─────────────           │
│  .Map(pattern)           │   ◄──►   │  .Map(pattern)           │
│  .WithName(name)         │          │  .WithName(name)         │
│  .AddHelp()              │          │  .AddHelp()              │
│  .Build()                │          │  .FinalizeModel()        │
└──────────────────────────┘          └──────────────────────────┘

PARALLEL STRUCTURE:
┌─────────────────────────────────────────────────────────────────┐
│  Every method on the real fluent API has a corresponding       │
│  method on the IR builder that captures the same information    │
│  but stores it in an intermediate representation                │
└─────────────────────────────────────────────────────────────────┘

BENEFITS:
• Method names match exactly → easy to understand
• Type-safe at compile time
• Changes to API require changes to IR builder (compile error)
```

---

## 7. Demo: Calculator Sample

### Input Code
```csharp
#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

using TimeWarp.Nuru;

NuruCoreApp app =
  NuruApp.CreateBuilder(args)
  .Map("add {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} + {y} = {x + y}"))
    .WithDescription("Add two numbers")
    .AsQuery()
    .Done()
  .Map("subtract {x:double} {y:double}")
    .WithHandler((double x, double y) => WriteLine($"{x} - {y} = {x - y}"))
    .WithDescription("Subtract numbers")
    .AsQuery()
    .Done()
  .Build();

return await app.RunAsync(args);
```

### What the Generator Sees

```
STEP 1: Locator finds Build() at line 18
        Locator finds RunAsync() at line 20

STEP 2: AppExtractor starts at Build() call
        Walks backward to find containing block

STEP 3: DslInterpreter walks statements:
        ┌────────────────────────────────────────────────┐
        │  Statement 1: NuruApp.CreateBuilder(args)      │
        │  → Creates IrAppBuilder                        │
        │                                                │
        │  Statement 2: .Map("add {x:double} {y:double}")│
        │  → PatternStringExtractor parses pattern       │
        │  → Finds: "add", param x:double, param y:double│
        │  → Creates IrRouteBuilder                      │
        │                                                │
        │  Statement 3: .WithHandler(...)                │
        │  → HandlerExtractor extracts lambda            │
        │  → Parameters: x:double, y:double              │
        │  → Body: WriteLine(...)                        │
        │                                                │
        │  Statement 4: .Done()                          │
        │  → Returns to IrAppBuilder                     │
        │                                                │
        │  (repeat for subtract route)                   │
        │                                                │
        │  Statement N: .Build()                         │
        │  → FinalizeModel() creates AppModel            │
        └────────────────────────────────────────────────┘

STEP 4: Models created
        AppModel:
          Routes: [
            RouteDefinition { Pattern: "add {x:double} {y:double}" },
            RouteDefinition { Pattern: "subtract {x:double} {y:double}" }
          ]
          InterceptSites: { "RunAsync": [line 20, col 10] }

STEP 5: InterceptorEmitter generates:
```

### Generated Code (Simplified)
```csharp
// <auto-generated />
namespace TimeWarp.Nuru.Generated
{
    [InterceptsLocation("calc.cs", line: 20, character: 10)]
    public static async Task<int> RunAsync_Intercepted(
        this NuruCoreApp app,
        string[] args)
    {
        // Parse args
        if (args.Length >= 3 && args[0] == "add")
        {
            // Type conversion
            if (double.TryParse(args[1], out var x) &&
                double.TryParse(args[2], out var y))
            {
                // Invoke handler
                WriteLine($"{x} + {y} = {x + y}");
                return 0;
            }
        }

        if (args.Length >= 3 && args[0] == "subtract")
        {
            // Similar pattern...
        }

        return 1; // No match
    }
}
```

---

## 8. Key Design Decisions

### Why Build() as Entry Point (Not RunAsync)?

```
PROBLEM:
┌─────────────────────────────────────────────────────────────────┐
│  var app = NuruApp.CreateBuilder(args).Map(...).Build();        │
│                                                                 │
│  await app.RunAsync(args);    // Call 1                         │
│  await app.RunAsync(args);    // Call 2 (same app!)             │
│  await app.RunAsync(args);    // Call 3                         │
└─────────────────────────────────────────────────────────────────┘

If we used RunAsync as entry point:
• Would extract the SAME app definition 3 times
• Duplicate processing, potential inconsistencies

SOLUTION: Use Build() as the unique identifier
• One Build() = One App = One extraction
• Multiple RunAsync() calls all reference the same app
• InterceptSites indexed by method: {"RunAsync": [site1, site2, site3]}
```

### Why Incremental Generation?

```
FULL REGENERATION:
┌─────────────────────────────────────────────────────────────────┐
│  User types...                                                  │
│  → Regenerate ALL sources                                       │
│  → Slow IDE response                                            │
│  → Battery drain                                                │
└─────────────────────────────────────────────────────────────────┘

INCREMENTAL GENERATION:
┌─────────────────────────────────────────────────────────────────┐
│  User types in File A...                                        │
│  → Only regenerate if File A contains Build() call              │
│  → Other files: use cached results                              │
│  → Fast IDE response                                            │
└─────────────────────────────────────────────────────────────────┘

ENABLED BY:
• ImmutableArray & ImmutableDictionary (efficient diffing)
• Separating syntactic from semantic phases
• Build() as unique cache key
```

---

## 9. Error Handling Philosophy

```
┌─────────────────────────────────────────────────────────────────┐
│  EXCEPTION-BASED (Traditional)                                  │
│  ──────────────────────────────                                 │
│  try {                                                          │
│    extract();     // throws on first error                      │
│  } catch {        // stop processing                            │
│    report();                                                    │
│  }                                                              │
│                                                                 │
│  PROBLEM: User fixes error 1, sees error 2, fixes, sees 3...    │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│  DIAGNOSTIC-BASED (Nuru)                                        │
│  ───────────────────────                                        │
│  var diagnostics = new List<Diagnostic>();                      │
│  extract(diagnostics);  // collects ALL errors                  │
│  reportAll(diagnostics); // user sees everything at once        │
│                                                                 │
│  BENEFIT: User sees ALL issues immediately                      │
└─────────────────────────────────────────────────────────────────┘

ExtractionResult = Model? + Diagnostic[]
• Model can be null (if extraction failed completely)
• Or partial (if some routes extracted successfully)
• Diagnostics contain all errors, warnings, info
```

---

## 10. Summary

```
┌─────────────────────────────────────────────────────────────────┐
│                    NURU SOURCE GENERATOR                        │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  LOCATORS        Syntactic pattern matching (fast)       │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  EXTRACTORS      Semantic type resolution (accurate)     │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  INTERPRETER     Compile-time DSL execution              │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  IR BUILDERS     Mirror fluent API structure             │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  MODELS          Immutable intermediate representation   │  │
│  └──────────────────────────────────────────────────────────┘  │
│                              │                                  │
│                              ▼                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  EMITTERS        Generate type-safe C# code              │  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  KEY PRINCIPLES:                                                │
│  • Semantic over syntactic (accuracy > speed)                   │
│  • Build() as unique app identifier                             │
│  • Incremental generation (cache-friendly)                      │
│  • Diagnostic collection (show all errors)                      │
│  • IR mirrors API (easy to maintain)                            │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Quick Reference: File Locations

| Component | File | Purpose |
|-----------|------|---------|
| Entry Point | `nuru-generator.cs` | Orchestrates pipeline |
| Locators | `locators/build-locator.cs` | Find Build() calls |
| Extractors | `extractors/app-extractor.cs` | Extract AppModel |
| Interpreter | `interpreter/dsl-interpreter.cs` | Execute DSL |
| IR Builders | `ir-builders/ir-app-builder.cs` | Build IR |
| Models | `models/app-model.cs` | Data structures |
| Emitters | `emitters/interceptor-emitter.cs` | Generate code |
