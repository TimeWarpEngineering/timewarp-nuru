# Phase 2: Add Group Support with CRTP (Interpreter)

**Status:** COMPLETED (2024-12-27)

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

### 2.0 Create Marker Interfaces (NEW - unified approach)

- [x] Create folder: `generators/ir-builders/abstractions/`
- [x] Create `iir-route-source.cs` - base interface with `Map()` and `WithGroupPrefix()`
- [x] Create `iir-app-builder.cs` - app-level interface (extends `IIrRouteSource`)
- [x] Create `iir-group-builder.cs` - group interface (extends `IIrRouteSource`, adds `Done()`)
- [x] Create `iir-route-builder.cs` - route configuration interface

### 2.1 Create `IrGroupBuilder<TParent>`

- [x] Create file: `source/timewarp-nuru-analyzers/generators/ir-builders/ir-group-builder.cs`
- [x] Implement `IIrGroupBuilder` interface
- [x] Constructor takes: parent, accumulatedPrefix, registerRoute callback
- [x] Field: `AccumulatedPrefix` (string) - the full prefix including all parent groups
- [x] Method: `Map(pattern)` → returns `IrRouteBuilder<IrGroupBuilder<TParent>>` (UNIFIED - no separate IrGroupRouteBuilder)
  - Pattern becomes `$"{AccumulatedPrefix} {pattern}"`
  - Segments are extracted from the FULL pattern internally
- [x] Method: `WithGroupPrefix(prefix)` → returns `IrGroupBuilder<IrGroupBuilder<TParent>>`
  - New prefix is `$"{AccumulatedPrefix} {prefix}"`
- [x] Method: `Done()` → returns `TParent`
- [x] Explicit interface implementation for `IIrGroupBuilder.Done()` returning `object`

### 2.2 Update `IrRouteBuilder<TParent>` (UNIFIED - no separate IrGroupRouteBuilder needed!)

- [x] Implement `IIrRouteBuilder` interface
- [x] Add explicit interface implementations returning interface types
- [x] Works for both app-level and group-level routes via `TParent` parameter

### 2.3 Add `WithGroupPrefix` to `IrAppBuilder`

- [x] Implement `IIrAppBuilder` interface (extends `IIrRouteSource`)
- [x] Add method: `WithGroupPrefix(string prefix)` → returns `IrGroupBuilder<TSelf>`
- [x] Update `Map()` to extract segments internally (Option A)
- [x] Add explicit interface implementations for polymorphic dispatch

### 2.4 Update `DslInterpreter`

- [x] Add dispatching for `WithGroupPrefix` using `IIrRouteSource`
- [x] Update `Map` dispatch to use `IIrRouteSource`
- [x] Update route methods to use `IIrRouteBuilder`
- [x] Update `Done` dispatch to handle both `IIrRouteBuilder` and `IIrGroupBuilder`
- [x] Update `Build` dispatch to use `IIrAppBuilder`
- [x] Update `BuiltAppMarker` to use `IIrAppBuilder`

### 2.5 Add Tests

- [x] Create test file: `tests/timewarp-nuru-analyzers-tests/interpreter/dsl-interpreter-group-test.cs`
- [x] Test: Simple group - one level ✓
- [x] Test: Nested groups - two levels ✓
- [x] Test: Route in outer group after nested group ✓
- [x] Test: Multiple routes in same group ✓
- [x] Test: Three levels of nesting ✓
- [x] Test: Mixed top-level and grouped routes ✓
- [x] All Phase 1 tests still pass (no regressions) ✓

## Files Created

| File | Purpose |
|------|---------|
| `generators/ir-builders/abstractions/iir-route-source.cs` | Base interface for route sources |
| `generators/ir-builders/abstractions/iir-app-builder.cs` | App-level builder interface |
| `generators/ir-builders/abstractions/iir-group-builder.cs` | Group builder interface |
| `generators/ir-builders/abstractions/iir-route-builder.cs` | Route builder interface |
| `generators/ir-builders/ir-group-builder.cs` | Group builder with prefix accumulation |
| `tests/.../interpreter/dsl-interpreter-group-test.cs` | Group nesting tests (6 tests) |

## Files Modified

| File | Change |
|------|--------|
| `generators/ir-builders/ir-app-builder.cs` | Implement `IIrAppBuilder`, add `WithGroupPrefix()` |
| `generators/ir-builders/ir-route-builder.cs` | Implement `IIrRouteBuilder` |
| `generators/interpreter/dsl-interpreter.cs` | Interface-based polymorphic dispatch |

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

1. ✅ All group tests pass (6/6)
2. ✅ Single-level groups work
3. ✅ Nested groups accumulate prefixes correctly
4. ✅ Routes after nested groups have correct prefix
5. ✅ `Done()` correctly returns to parent at each level
6. ✅ Existing Phase 1 tests still pass (4/4)

## Key Design Decisions

### Unified `IrRouteBuilder<TParent>` (No Separate `IrGroupRouteBuilder`)

The original plan called for a separate `IrGroupRouteBuilder<TParent>`, but we realized this was redundant. The `TParent` generic parameter already distinguishes:
- `IrRouteBuilder<IrAppBuilder>` - top-level route
- `IrRouteBuilder<IrGroupBuilder<...>>` - route inside a group

This unification simplifies the codebase significantly.

### Marker Interfaces for Polymorphic Dispatch

Instead of explicit type enumeration in the dispatcher, we introduced marker interfaces:
- `IIrRouteSource` - can create routes and groups (`Map`, `WithGroupPrefix`)
- `IIrAppBuilder` - app-level operations (`Build`, `FinalizeModel`, `AddInterceptSite`)
- `IIrGroupBuilder` - nested group with `Done()`
- `IIrRouteBuilder` - route configuration with `Done()`

This enables clean dispatch:
```csharp
if (currentReceiver is IIrRouteSource source)
  return source.Map(pattern);
```

### Explicit Interface Implementations

Concrete classes have both:
- **Public methods** returning concrete types (for CRTP fluent chaining)
- **Explicit interface implementations** returning interface types (for polymorphic dispatch)

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
