# Phase 2: Add Group Support with CRTP (Interpreter)

## Description

Add `IrGroupBuilder` and `IrGroupRouteBuilder` to handle nested route groups. This is the core feature that motivated the interpreter approach - nested groups naturally accumulate prefixes when the IR builders mirror the DSL structure.

## Parent

#277 Epic: Semantic DSL Interpreter with Mirrored IR Builders

## Depends On

#278 Phase 1: POC - Minimal Fluent Case ✅ (completed)

## Scope

Support nested groups:

```csharp
NuruApp.CreateBuilder([])
  .WithGroupPrefix("admin")
    .Map("status")
      .WithHandler(() => "admin status")
      .AsQuery()
      .Done()
    .WithGroupPrefix("config")
      .Map("get {key}")
        .WithHandler((string key) => $"value: {key}")
        .AsQuery()
        .Done()
      .Done()  // end config group
    .Done()    // end admin group
  .Build();
```

This should produce routes:
- `admin status`
- `admin config get {key}`

## Checklist

### 2.1 Create `IrGroupBuilder<TParent>`

- [ ] Create file: `source/timewarp-nuru-analyzers/generators/ir-builders/ir-group-builder.cs`
- [ ] Make class `public` (analyzer types don't need to be internal)
- [ ] Constructor takes: parent, accumulatedPrefix, registerRoute callback
- [ ] Field: `AccumulatedPrefix` (string) - the full prefix including all parent groups
- [ ] Method: `Map(pattern, segments)` → returns `IrGroupRouteBuilder<TParent>`
  - Pattern becomes `$"{AccumulatedPrefix} {pattern}"`
  - Segments are extracted from the FULL pattern
- [ ] Method: `WithGroupPrefix(prefix)` → returns `IrGroupBuilder<IrGroupBuilder<TParent>>`
  - New prefix is `$"{AccumulatedPrefix} {prefix}"`
- [ ] Method: `Done()` → returns `TParent`

### 2.2 Create `IrGroupRouteBuilder<TParent>`

- [ ] Create file: `source/timewarp-nuru-analyzers/generators/ir-builders/ir-group-route-builder.cs`
- [ ] Make class `public` (analyzer types don't need to be internal)
- [ ] Same structure as `IrRouteBuilder<TParent>` but parent is `IrGroupBuilder<TParent>`
- [ ] Constructor takes: parent (IrGroupBuilder), registerRoute callback, fullPattern
- [ ] All route configuration methods mirror `IrRouteBuilder`:
  - `WithHandler(HandlerDefinition)`
  - `WithDescription(string)`
  - `AsQuery()` / `AsCommand()` / `AsIdempotentCommand()`
- [ ] Method: `Done()` → registers route, returns `IrGroupBuilder<TParent>`

### 2.3 Add `WithGroupPrefix` to `IrAppBuilder`

- [ ] Add method: `WithGroupPrefix(string prefix)` → returns `IrGroupBuilder<TSelf>`
- [ ] Creates `IrGroupBuilder` with:
  - Parent: `(TSelf)this`
  - AccumulatedPrefix: `prefix`
  - RegisterRoute: callback to add to Routes collection

### 2.4 Update `DslInterpreter`

- [ ] Add dispatching for `WithGroupPrefix`:
  - On `IrAppBuilder` → call `WithGroupPrefix()`, returns `IrGroupBuilder`
  - On `IrGroupBuilder` → call `WithGroupPrefix()`, returns nested `IrGroupBuilder`
- [ ] Add dispatching for `Map` on `IrGroupBuilder`:
  - Extract pattern, call `Map()`, returns `IrGroupRouteBuilder`
- [ ] Add dispatching for route methods on `IrGroupRouteBuilder`:
  - Same as `IrRouteBuilder` but different receiver type
- [ ] Add dispatching for `Done` on `IrGroupBuilder`:
  - Returns parent builder
- [ ] Update receiver type checking to handle group builders

### 2.5 Method Dispatching Updates

New dispatch entries:

| DSL Method | Receiver Type | Action | Returns |
|------------|---------------|--------|---------|
| `WithGroupPrefix` | `IrAppBuilder` | Create `IrGroupBuilder` with prefix | `IrGroupBuilder<IrAppBuilder>` |
| `WithGroupPrefix` | `IrGroupBuilder<T>` | Create nested `IrGroupBuilder` | `IrGroupBuilder<IrGroupBuilder<T>>` |
| `Map` | `IrGroupBuilder<T>` | Call `Map()` with accumulated prefix | `IrGroupRouteBuilder<T>` |
| `WithHandler` | `IrGroupRouteBuilder<T>` | Call `WithHandler()` | `IrGroupRouteBuilder<T>` |
| `AsQuery` etc | `IrGroupRouteBuilder<T>` | Call method | `IrGroupRouteBuilder<T>` |
| `Done` | `IrGroupRouteBuilder<T>` | Register route, return parent | `IrGroupBuilder<T>` |
| `Done` | `IrGroupBuilder<T>` | Return parent | `T` (parent builder) |

### 2.6 Add Tests

- [ ] Create test file: `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-group-test.cs`
- [ ] Test: Simple group - one level
  ```csharp
  .WithGroupPrefix("admin")
    .Map("status").WithHandler(...).Done()
    .Done()
  ```
  - Assert route pattern is "admin status"
- [ ] Test: Nested groups - two levels
  ```csharp
  .WithGroupPrefix("admin")
    .WithGroupPrefix("config")
      .Map("get {key}").WithHandler(...).Done()
      .Done()
    .Done()
  ```
  - Assert route pattern is "admin config get {key}"
- [ ] Test: Route in outer group after nested group
  ```csharp
  .WithGroupPrefix("admin")
    .WithGroupPrefix("config")
      .Map("list").WithHandler(...).Done()
      .Done()
    .Map("status").WithHandler(...).Done()
    .Done()
  ```
  - Assert two routes: "admin config list" and "admin status"
- [ ] Test: Multiple routes in same group
- [ ] Test: Three levels of nesting

## Files to Create

| File | Purpose |
|------|---------|
| `generators/ir-builders/ir-group-builder.cs` | Group builder with prefix accumulation |
| `generators/ir-builders/ir-group-route-builder.cs` | Route builder for grouped routes |
| `tests/.../interpreter/dsl-interpreter-group-test.cs` | Group nesting tests |

## Files to Modify

| File | Change |
|------|--------|
| `generators/ir-builders/ir-app-builder.cs` | Add `WithGroupPrefix()` method |
| `generators/interpreter/dsl-interpreter.cs` | Add group method dispatching |

## Technical Notes

### Prefix Accumulation

The key insight is that prefix accumulation happens naturally:

```csharp
// In IrGroupBuilder<TParent>
public IrGroupBuilder<IrGroupBuilder<TParent>> WithGroupPrefix(string prefix)
{
  string newPrefix = $"{AccumulatedPrefix} {prefix}";
  return new IrGroupBuilder<IrGroupBuilder<TParent>>(this, newPrefix, RegisterRoute);
}

public IrGroupRouteBuilder<TParent> Map(string pattern, ImmutableArray<SegmentDefinition> segments)
{
  string fullPattern = $"{AccumulatedPrefix} {pattern}";
  // Segments should be extracted from fullPattern, not pattern
  ImmutableArray<SegmentDefinition> fullSegments = PatternStringExtractor.ExtractSegments(fullPattern);
  return new IrGroupRouteBuilder<TParent>(this, RegisterRoute, fullPattern, fullSegments);
}
```

### Type Flow

The recursive type structure enables infinite nesting:

```
IrAppBuilder
  .WithGroupPrefix("admin")     → IrGroupBuilder<IrAppBuilder>
    .WithGroupPrefix("config")  → IrGroupBuilder<IrGroupBuilder<IrAppBuilder>>
      .Map("get")               → IrGroupRouteBuilder<IrGroupBuilder<IrAppBuilder>>
        .Done()                 → IrGroupBuilder<IrGroupBuilder<IrAppBuilder>>
      .Done()                   → IrGroupBuilder<IrAppBuilder>
    .Done()                     → IrAppBuilder
  .Build()                      → AppModel
```

### Receiver Type Checking

The interpreter needs to check both the concrete type and the generic type:

```csharp
object? receiver = EvaluateReceiver(invocation);

if (receiver is IrGroupBuilder<object> groupBuilder)  // Won't work due to variance
{
  // Need a different approach
}

// Option: Use a marker interface
if (receiver is IIrGroupBuilder groupBuilder)
{
  // Handle group builder methods
}
```

Consider adding marker interfaces:
- `IIrAppBuilder`
- `IIrGroupBuilder`
- `IIrRouteBuilder`

## Success Criteria

1. All group tests pass
2. Single-level groups work
3. Nested groups accumulate prefixes correctly
4. Routes after nested groups have correct prefix
5. `Done()` correctly returns to parent at each level
6. Existing Phase 1 tests still pass

---

## Lessons Learned from Phase 1

### What Worked Well

1. **CRTP Pattern**: The `IrAppBuilder<TSelf>` pattern works well for fluent chaining
2. **Reusing Existing Extractors**: `PatternStringExtractor`, `HandlerExtractor`, `InterceptSiteExtractor` work out of the box
3. **Fluent Chain Unrolling**: Walking nested `MemberAccessExpressionSyntax` and reversing gives correct execution order
4. **Polymorphic Dispatch**: Using `object?` return type for dispatch methods allows different builder types to flow through

### Key Implementation Details

1. **Public Types**: All analyzer types should be `public` - they're in a separate DLL that users never reference directly
2. **Type Alias for SyntaxNode**: Use `using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;` to avoid conflict with `TimeWarp.Nuru.SyntaxNode`
3. **Suppress CA1859**: Add `[SuppressMessage("Performance", "CA1859:...")]` on `DslInterpreter` class since polymorphic dispatch requires `object?` returns
4. **Build vs FinalizeModel**: We agreed on `Build()` marks as built (returns `TSelf`), `FinalizeModel()` creates `AppModel`. Apply same pattern to group builders.
5. **RunAsync Handling**: The interpreter walks statements after `Build()` to find `RunAsync()` calls and extract intercept sites

### Test Infrastructure

1. **Directory.Build.props**: Create `tests/timewarp-nuru-analyzers-tests/interpreter/Directory.Build.props` that:
   - Imports parent Directory.Build.props
   - Adds `<ProjectReference>` to analyzer as regular assembly (not analyzer output)
2. **Test File Naming**: Use `dsl-interpreter-*.cs` pattern (NOT `temp-*`)
3. **Test Pattern**: Files are runfiles with shebang, compiled not interpreted

### Code Location

- IR builders: `source/timewarp-nuru-analyzers/generators/ir-builders/`
- Interpreter: `source/timewarp-nuru-analyzers/generators/interpreter/`
- Tests: `tests/timewarp-nuru-analyzers-tests/interpreter/`

### Existing Code Reference

Review these Phase 1 files for patterns:
- `ir-app-builder.cs` - CRTP pattern, `Build()`/`FinalizeModel()` separation
- `ir-route-builder.cs` - Internal `RouteDefinitionBuilder` usage, `Done()` pattern
- `dsl-interpreter.cs` - `UnrollFluentChain()`, `DispatchMethodCall()`, receiver type checking
