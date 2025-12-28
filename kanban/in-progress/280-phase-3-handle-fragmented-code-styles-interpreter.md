# Phase 3: Handle Fragmented Code Styles (Interpreter)

## Description

Enhance the interpreter to handle fragmented code styles where consumers use temporary variables and separate fluent chains across multiple statements. This phase focuses specifically on fragmented style support - the block-based infrastructure was implemented in #283 and #284.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

- #278 Phase 1: POC - Minimal Fluent Case ✅ (completed)
- #279 Phase 2: Add Group Support with CRTP ✅ (completed)
- #283 Phase 1a: Migrate Interpreter to Block-Based ✅ (completed)
- #284 Phase 2a: Verify Group Support with Block Interpreter ✅ (completed)

## Scope

Support these fragmented code styles (beyond the pure fluent style already supported):

```csharp
// Style 2: Mixed - builder in variable
var builder = NuruApp.CreateBuilder([]);
builder.Map("ping").WithHandler(() => "pong").Done();
builder.Build();

// Style 3: Fully fragmented
var builder = NuruApp.CreateBuilder([]);
var endpoint = builder.Map("ping");
endpoint.WithHandler(() => "pong");
endpoint.Done();
builder.Build();

// Style 4: With non-builder code interleaved
var builder = NuruApp.CreateBuilder([]);
var junk = "Hi mom";
Console.WriteLine(junk);
builder.Map("ping").WithHandler(() => "pong").Done();
builder.Build();
```

## Checklist

### Handle Non-DSL Method Calls

- [ ] Change `DispatchMethodCall` catch-all from throwing to calling `HandleNonDslMethod`
- [ ] Add `HandleNonDslMethod(invocation, receiver, methodName)` method:
  - If receiver is a builder type (`IIrRouteSource`, `IIrRouteBuilder`, `IIrGroupBuilder`, `IIrAppBuilder`): throw error (unknown DSL method)
  - If receiver is non-builder (e.g., `Console`): return `null` (ignore - not our DSL)

### Add Helper Method

- [ ] Add `IsBuilderType(ITypeSymbol?)` helper method for semantic type checking

### Add Tests

- [ ] Create test file: `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-fragmented-test.cs`
- [ ] Test: `Should_interpret_builder_in_variable` (Style 2)
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  builder.Map("ping").WithHandler(() => "pong").AsQuery().Done();
  builder.Build();
  await builder.RunAsync(["ping"]);
  ```
- [ ] Test: `Should_interpret_fully_fragmented` (Style 3)
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var endpoint = builder.Map("ping");
  endpoint.WithHandler(() => "pong");
  endpoint.AsQuery();
  endpoint.Done();
  builder.Build();
  await builder.RunAsync(["ping"]);
  ```
- [ ] Test: `Should_ignore_non_builder_code` (Style 4)
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var junk = "Hi mom";
  Console.WriteLine(junk);
  builder.Map("ping").WithHandler(() => "pong").AsQuery().Done();
  builder.Build();
  await builder.RunAsync(["ping"]);
  ```
- [ ] Test: `Should_interpret_mixed_group_and_fragmented`
  ```csharp
  var builder = NuruApp.CreateBuilder([]);
  var admin = builder.WithGroupPrefix("admin");
  admin.Map("status").WithHandler(() => "ok").AsQuery().Done();
  admin.Done();
  builder.Build();
  await builder.RunAsync(["admin", "status"]);
  ```
- [ ] Test: `Should_interpret_multiple_apps_in_block`
  ```csharp
  var app1 = NuruApp.CreateBuilder([]).Map("ping").WithHandler(() => "pong").AsQuery().Done().Build();
  var app2 = NuruApp.CreateBuilder([]).Map("status").WithHandler(() => "ok").AsQuery().Done().Build();
  await app1.RunAsync(["ping"]);
  await app2.RunAsync(["status"]);
  ```
  - Assert: 2 AppModels returned
  - Assert: app1 has route "ping", app2 has route "status"
  - Assert: Each has its own intercept site

## Files to Modify

| File | Change |
|------|--------|
| `generators/interpreter/dsl-interpreter.cs` | Add `HandleNonDslMethod()`, add `IsBuilderType()` |

## Files to Create

| File | Purpose |
|------|---------|
| `tests/.../interpreter/dsl-interpreter-fragmented-test.cs` | 5 fragmented style tests |

## Technical Notes

### HandleNonDslMethod Implementation

```csharp
private object? HandleNonDslMethod(
  InvocationExpressionSyntax invocation,
  object? receiver, 
  string methodName)
{
  // If receiver is a builder type, fail fast - unknown DSL method
  if (receiver is IIrRouteSource or IIrRouteBuilder or IIrGroupBuilder or IIrAppBuilder)
  {
    throw new InvalidOperationException(
      $"Unrecognized DSL method: {methodName}. Location: {invocation.GetLocation().GetLineSpan()}");
  }
  
  // Non-DSL method call (Console.WriteLine, etc.) - ignore
  return null;
}
```

### IsBuilderType Implementation

```csharp
private static bool IsBuilderType(ITypeSymbol? type)
{
  if (type is null) return false;
  
  string typeName = type.Name;
  return typeName is "NuruCoreAppBuilder" or "NuruAppBuilder"
      or "EndpointBuilder" or "GroupBuilder" or "GroupEndpointBuilder"
      or "NestedCompiledRouteBuilder";
}
```

### Why This Works

The block-based interpreter from Phase 1a already:
- Tracks all variables in `VariableState` dictionary
- Resolves identifiers via `ResolveIdentifier()` using semantic model
- Evaluates expressions recursively via `EvaluateExpression()`

The IR builders are mutable, so:
- `endpoint.WithHandler(...)` modifies the builder in place (no reassignment needed)
- `endpoint.Done()` returns parent but the route is already registered

## Known Limitation

**`return app.RunAsync(...)` is not yet supported.** When encountered, the interpreter should throw a clear error:
> "Return statements with RunAsync() are not yet supported. Use 'await app.RunAsync(...)' as a separate statement."

This will be addressed in a future phase.

## Success Criteria

1. All 5 fragmented style tests pass
2. Non-DSL statements (`Console.WriteLine`, string literals, etc.) are ignored without error
3. Builder method calls on variables work correctly
4. All existing 10 tests still pass (4 Phase 1 + 6 Phase 2)
5. **Total: 15 tests** pass

---

## Lessons Learned from Phase 1a/2a

### Block-Based Processing

The interpreter now uses `Interpret(BlockSyntax)` which:
- Returns `IReadOnlyList<AppModel>` (supports multiple apps per block)
- Processes statements one by one via `ProcessBlock()` → `ProcessStatement()`
- Tracks variables in `VariableState` dictionary with `SymbolEqualityComparer.Default`

### Variable Resolution

```csharp
private object? ResolveIdentifier(IdentifierNameSyntax identifier)
{
  ISymbol? symbol = SemanticModel.GetSymbolInfo(identifier).Symbol;
  if (symbol is null)
    return null;
  return VariableState.TryGetValue(symbol, out object? value) ? value : null;
}
```

### Current Test Count

- Phase 1: 4 tests (minimal fluent case)
- Phase 2: 6 tests (group support)
- **Total before Phase 3: 10 tests** - all passing
