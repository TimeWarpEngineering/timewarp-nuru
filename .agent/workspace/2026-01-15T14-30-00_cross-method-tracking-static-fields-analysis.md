# Cross-Method Tracking for Static Fields: Technical Analysis

## Executive Summary

The TimeWarp.Nuru source generator fails to trace `RunAsync()`/`RunReplAsync()` calls back to their `Build()` call when the app is stored in a static or instance field. This affects ~128 REPL tests that use the `Setup()` pattern. The root cause is that `DslInterpreter.ResolveIdentifier()` only handles `ILocalSymbol` and cannot resolve `IFieldSymbol` or `IPropertySymbol`. The current incremental generator architecture processes syntax nodes independently, making cross-method tracking a significant architectural change requiring whole-compilation analysis.

## Scope

This analysis covers:

1. **Primary affected components:**
   - `DslInterpreter.ResolveIdentifier()` - symbol resolution logic
   - `AppExtractor.ExtractFromBuildCall()` - app model extraction
   - `NuruGenerator` - incremental generator pipeline
   
2. **Related components:**
   - `InterceptSiteExtractor` - entry point location extraction
   - `IrAppBuilder` - IR builder for app models
   - `AppModel` - top-level IR record with intercept sites

3. **Test coverage:**
   - ~128 REPL tests using `Setup()` pattern with static fields
   - Test pattern: `private static NuruCoreApp? App` assigned in `Setup()`, called in test methods

## Methodology

1. **Codebase exploration:** Examined source generator architecture and symbol resolution
2. **Symbol analysis:** Analyzed `ResolveIdentifier()` to understand current symbol handling
3. **Flow tracing:** Mapped the entry point → Build() traceback flow
4. **Pattern analysis:** Reviewed failing tests to confirm the static field pattern
5. **Architecture review:** Evaluated incremental generator constraints

## Findings

### 1. Current Symbol Resolution Limitation

**File:** `generators/interpreter/dsl-interpreter.cs:412-438`

```csharp
private object? ResolveIdentifier(IdentifierNameSyntax identifier)
{
  ISymbol? symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;
  if (symbol is null)
    return null;

  // Check cache first (avoid re-evaluating same declaration)
  if (VariableState.TryGetValue(symbol, out object? cached))
    return cached;

  // Use semantic model to find declaration and evaluate initializer
  if (symbol is ILocalSymbol localSymbol)
  {
    SyntaxReference? syntaxRef = localSymbol.DeclaringSyntaxReferences.FirstOrDefault();
    if (syntaxRef?.GetSyntax(CancellationToken) is VariableDeclaratorSyntax declarator
        && declarator.Initializer?.Value is { } initializer)
    {
      object? value = EvaluateExpression(initializer);
      VariableState[symbol] = value;
      return value;
    }
  }

  return null;  // <-- IFieldSymbol and IPropertySymbol fall through here
}
```

**Problem:** The method only handles `ILocalSymbol`. Static fields (`IFieldSymbol`) and properties (`IPropertySymbol`) are not processed, resulting in `null` being returned.

### 2. Interpreter State Lifecycle

**File:** `generators/interpreter/dsl-interpreter.cs:38-40`

```csharp
// Per-interpretation state
private Dictionary<ISymbol, object?> VariableState = null!;
private List<IrAppBuilder> BuiltApps = null!;
private List<Diagnostic> CollectedDiagnostics = null!;
```

Each interpretation creates a fresh `VariableState` dictionary. When `ExtractFromBuildCall()` interprets a method containing `Build()`:

1. `VariableState` is populated with local variable → value mappings
2. Entry points (`RunAsync`, `RunReplAsync`) in the **same block** are processed and intercept sites are added
3. Static field assignments are **not tracked** because they're not local symbols
4. When `App!.RunReplAsync()` is called from a **different method**, `VariableState` is empty for that context

### 3. Extraction Flow for Build()-Based Approach

**File:** `generators/extractors/app-extractor.cs:227-276`

```csharp
public static ExtractionResult ExtractFromBuildCall(
  GeneratorSyntaxContext context,
  CancellationToken cancellationToken
)
{
  // ... validation ...
  
  DslInterpreter interpreter = new(context.SemanticModel, cancellationToken);
  ExtractionResult result;

  BlockSyntax? block = FindContainingBlock(buildInvocation);
  if (block is not null)
  {
    // Traditional method body - interpret the whole block
    result = interpreter.InterpretWithDiagnostics(block);
  }
  else
  {
    // Top-level statements
    result = interpreter.InterpretTopLevelStatementsWithDiagnostics(compilationUnit);
  }
  
  // ...
}
```

The interpreter processes **only the containing block** of the `Build()` call. Entry points in other methods are never seen during this interpretation.

### 4. Entry Point Dispatch Logic

**File:** `generators/interpreter/dsl-interpreter.cs:1266-1292`

```csharp
private object? DispatchRunAsyncCall(InvocationExpressionSyntax invocation, object? receiver)
{
  IrAppBuilder? appBuilder = receiver switch
  {
    BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
    IrAppBuilder builder => builder,
    _ => null  // <-- Static field reference returns null here
  };

  if (appBuilder is null)
    return null;  // No intercept site is added

  InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
  if (site is not null)
    appBuilder.AddInterceptSite("RunAsync", site);

  return null;
}
```

When `App.RunReplAsync()` is evaluated from a different method:
1. `EvaluateExpression(App)` is called
2. `ResolveIdentifier(App)` returns `null` (field symbol not handled)
3. `receiver` is `null`
4. `appBuilder` is `null`
5. No intercept site is added to any `IrAppBuilder`

### 5. Generator Pipeline Architecture

**File:** `generators/nuru-generator.cs:33-131`

The incremental generator uses separate `CreateSyntaxProvider` steps:

| Step | Provider | Trigger | Limitation |
|------|----------|---------|------------|
| 1 | BuildLocator | `.Build()` call | Processes one block at a time |
| 2 | ForAttributeWithMetadataName | `[NuruRoute]` classes | Independent of Build() |
| 3 | MapLocator | `.Map()` calls | Independent of Build() |
| 4 | CompilationProvider | Assembly metadata | Once per compilation |

Each provider runs independently, and there's no mechanism to:
- Correlate a `RunAsync()` call in method A with a `Build()` in method B
- Track static field assignments across method boundaries
- Perform cross-method symbol resolution

### 6. Failing Test Pattern

**File:** `tests/timewarp-nuru-tests/repl/repl-18-psreadline-keybindings.cs:21-70`

```csharp
private static NuruCoreApp? App;

public static async Task Setup()
{
  Terminal = new TestTerminal();
  App = NuruApp.CreateBuilder([])
    .UseTerminal(Terminal)
    .Map("aXb")...
    .AddRepl(options => {...})
    .Build();  // Build() is interpreted here - VariableState populated
}

public static async Task Should_move_backward_char_with_left_arrow()
{
  Terminal!.QueueKeys("ab");
  Terminal.QueueKey(ConsoleKey.LeftArrow);
  Terminal.QueueLine("exit");
  
  await App!.RunReplAsync();  // Different method - new interpretation, VariableState empty
}
```

**Result:** When `Setup()` is interpreted, `App` is assigned but never registered in `VariableState` (it's a field, not a local). When the test method runs, `App.RunReplAsync()` can't be traced back.

### 7. Working Inline Pattern

**File:** `tests/timewarp-nuru-tests/repl/repl-36-run-repl-async-inline.cs:21-42`

```csharp
public static async Task Should_intercept_run_repl_async_inline()
{
  using TestTerminal terminal = new();
  terminal.QueueLine("exit");

  NuruCoreApp app = NuruApp.CreateBuilder([])  // Local variable
    .UseTerminal(terminal)
    .Map("hello")...
    .AddRepl()
    .Build();

  await app.RunReplAsync();  // Same method, same interpretation - works!
}
```

**Result:** Both `Build()` and `RunReplAsync()` are in the same method body. The interpreter processes them in a single `VariableState` context.

## Impact Assessment

### Affected Tests

| Test Category | Approximate Count | Pattern |
|---------------|-------------------|---------|
| REPL tests with Setup() | ~128 | Static `NuruCoreApp? App` field |
| Instance field tests | Unknown | Instance `NuruCoreApp App` field |
| Property tests | Unknown | `NuruCoreApp App { get; set; }` |

### Functional Impact

1. **No intercept sites registered** - Generated code doesn't contain `[InterceptsLocation]` attributes for affected entry points
2. **Original methods execute** - `NuruCoreApp.RunReplAsync()` runs without interception
3. **Test failures** - Tests expect REPL behavior but get original implementation
4. **No route matching** - Generated routing logic is bypassed

### Architecture Impact

The fix requires modifying fundamental generator assumptions:
- Current: Single-block interpretation with local symbol tracking
- Required: Whole-compilation analysis with cross-method symbol resolution

## Potential Solutions

### Option A: Static Field Assignment Tracking

**Approach:** Extend `DslInterpreter` to track assignments to fields and properties of type `NuruCoreApp`.

**Implementation:**
1. Add new symbol types to `ResolveIdentifier()`:
   ```csharp
   if (symbol is IFieldSymbol fieldSymbol)
   {
     // Find all assignments to this field in the compilation
     // Track: field symbol → IrAppBuilder
   }
   if (symbol is IPropertySymbol propertySymbol)
   {
     // Find all assignments to this property
     // Track: property symbol → IrAppBuilder
   }
   ```

2. Build a field-to-builder mapping during extraction:
   ```csharp
   // During interpretation of Setup()
   var fieldTracker = new FieldAssignmentTracker(semanticModel);
   fieldTracker.TrackFieldAssignments();
   ```

3. When evaluating entry point on field:
   ```csharp
   if (receiver is null && symbol is IFieldSymbol field)
   {
     return FieldAssignmentTracker.GetBuilderForField(field);
   }
   ```

**Challenges:**
- Cross-method tracking requires whole-compilation analysis
- Field assignments may occur in multiple methods
- Need to handle property setters
- Incremental build complexity increases significantly

### Option B: Two-Pass Extraction

**Approach:** First pass collects all `Build()` calls and their results, second pass matches entry points.

**Implementation:**
1. **Pass 1:** Extract all `AppModel` instances from `Build()` calls
   ```csharp
   var allApps = compilation.FindAllBuildCalls()
     .Select(call => ExtractAppModel(call))
     .ToImmutableList();
   ```

2. **Pass 2:** Find all `RunAsync()`/`RunReplAsync()` calls
   ```csharp
   var allEntryPoints = compilation.FindAllEntryPointCalls();
   
   foreach (var entryPoint in allEntryPoints)
   {
     var receiver = GetReceiverSymbol(entryPoint);
     var matchingApp = allApps.First(app => 
       app.VariableName == receiver.Name ||
       app.AssignedFieldSymbol == receiver.Symbol);
     
     matchingApp.AddInterceptSite(entryPoint);
   }
   ```

**Challenges:**
- Requires restructuring the incremental generator pipeline
- Performance impact of two compilation-wide passes
- Symbol matching must handle edge cases (multiple assignments)

### Option C: Whole-Compilation Analysis (Recommended)

**Approach:** Perform comprehensive compilation analysis to build complete app→entry point maps.

**Implementation:**
1. Create a new `CrossMethodAnalyzer`:
   ```csharp
   internal sealed class CrossMethodAnalyzer
   {
     public ImmutableDictionary<ISymbol, IrAppBuilder> FieldAssignments { get; }
     public ImmutableDictionary<InvocationExpressionSyntax, IrAppBuilder> EntryPoints { get; }
     
     public void Analyze(Compilation compilation)
     {
       // 1. Find all Build() calls
       var buildCalls = FindBuildCalls(compilation);
       
       // 2. For each Build(), extract app and track what it assigns to
       foreach (var buildCall in buildCalls)
       {
         var app = ExtractApp(buildCall);
         TrackAssignment(app, buildCall);
       }
       
       // 3. Find all entry point calls
       var entryPoints = FindEntryPointCalls(compilation);
       
       // 4. Match entry points to apps by symbol
       foreach (var ep in entryPoints)
       {
         var builder = FindMatchingBuilder(ep.ReceiverSymbol);
         builder?.AddInterceptSite(ep);
       }
     }
   }
   ```

2. Integrate with generator pipeline:
   ```csharp
   // Single compilation-wide analysis step
   var crossMethodAnalysis = compilation
     .Select((comp, ct) => new CrossMethodAnalyzer().Analyze(comp));
   
   // Combine with existing providers
   ```

3. Modify `ResolveIdentifier()` to use cross-method tracking:
   ```csharp
   private object? ResolveIdentifier(IdentifierNameSyntax identifier)
   {
     var symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;
     
     // Check local variable state first
     if (VariableState.TryGetValue(symbol, out var cached))
       return cached;
     
     // Check cross-method field/property tracking
     if (CrossMethodTracker.TryGetValue(symbol, out var builder))
       return builder;
     
     return null;
   }
   ```

**Benefits:**
- Handles all assignment patterns (static field, instance field, property)
- Single analysis pass for the entire compilation
- Can be cached incrementally with proper granularity

**Challenges:**
- Significant architectural change
- Must maintain incremental build performance
- Complex caching strategy required

### Option D: Pragmatic Approach - Setup() Pattern Detection

**Approach:** Detect the common `Setup()` pattern and handle it specially.

**Implementation:**
1. Recognize test pattern:
   ```csharp
   private static NuruCoreApp? App;
   
   public static async Task Setup()
   {
     App = NuruApp.CreateBuilder([...]).Build();
   }
   ```

2. When interpreting `Setup()`, track the field assignment:
   ```csharp
   var assignment = node as AssignmentExpressionSyntax;
   if (IsNuruCoreAppField(assignment.Left, semanticModel))
   {
     var builder = EvaluateExpression(assignment.Right);
     TestPatternTracker.TrackFieldAssignment(assignment.Left, builder);
   }
   ```

3. When evaluating entry point, check test pattern tracker:
   ```csharp
   if (symbol is IFieldSymbol field && TestPatternTracker.HasAssignment(field))
   {
     return TestPatternTracker.GetBuilder(field);
   }
   ```

**Benefits:**
- Targeted fix for the most common failing pattern
- Minimal architectural change
- Can be implemented incrementally

**Challenges:**
- Only handles test Setup() pattern, not general use cases
- May miss other cross-method patterns
- Special-casing adds complexity

## Recommendations

### Primary Recommendation: Option C (Whole-Compilation Analysis)

This is the most complete solution that addresses the root cause. However, it requires careful implementation to maintain incremental build performance.

**Implementation Roadmap:**

1. **Phase 1: Symbol Tracking Infrastructure**
   - Create `CrossMethodSymbolTracker` class
   - Implement `FindAllFieldAssignmentsOfType()` helper
   - Add unit tests for symbol tracking

2. **Phase 2: Analyzer Integration**
   - Create `CrossMethodAnalyzer` to orchestrate analysis
   - Modify `DslInterpreter` to use cross-method tracking
   - Update `AppExtractor` to work with new architecture

3. **Phase 3: Incremental Optimization**
   - Implement caching for symbol tracking results
   - Add change detection to avoid full recompilation
   - Profile and optimize hot paths

4. **Phase 4: Validation**
   - Run all affected tests (~128 REPL tests)
   - Verify incremental build performance
   - Test edge cases (multiple assignments, null checks)

### Immediate Workaround: Option D (Setup Pattern Detection)

While implementing Option C, provide an immediate fix for the test suite:

1. Detect `Setup()` method pattern in tests
2. Track static field assignments within Setup()
3. Wire entry points in test methods to tracked builders
4. This enables ~128 tests to pass while full solution is developed

## Acceptance Criteria Progress

| Criterion | Status | Notes |
|-----------|--------|-------|
| Tests using Setup() pattern pass | ❌ Not implemented | ~128 tests affected |
| Static field assignment is tracked | ❌ Not implemented | Requires cross-method analysis |
| Instance field assignment is tracked | ❌ Not implemented | Requires cross-method analysis |
| Property assignment is tracked | ❌ Not implemented | Requires cross-method analysis |
| Incremental build performance is maintained | ⚠️ Risk | New analysis must be incremental |

## References

### Source Files

| File | Purpose |
|------|---------|
| `generators/interpreter/dsl-interpreter.cs:412-438` | `ResolveIdentifier()` - symbol resolution |
| `generators/interpreter/dsl-interpreter.cs:1266-1322` | `DispatchRunAsyncCall()`, `DispatchRunReplAsyncCall()` |
| `generators/extractors/app-extractor.cs:227-276` | `ExtractFromBuildCall()` |
| `generators/nuru-generator.cs:33-131` | Generator pipeline |
| `generators/models/app-model.cs:30-53` | AppModel record definition |
| `tests/timewarp-nuru-tests/repl/repl-18-psreadline-keybindings.cs` | Failing test example |
| `tests/timewarp-nuru-tests/repl/repl-36-run-repl-async-inline.cs` | Working test example |

### Key Symbol Types

| Symbol Type | Handles | Currently Supported |
|-------------|---------|---------------------|
| `ILocalSymbol` | Local variables | ✅ Yes |
| `IFieldSymbol` | Static/instance fields | ❌ No |
| `IPropertySymbol` | Properties | ❌ No |

### Related Documentation

- [Roslyn Symbol API](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.isymbol)
- [Incremental Generators](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators)
- [SyntaxNode vs SemanticModel](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/semantic-model)
