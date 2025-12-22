# Implement CRTP Pattern for NuruCoreAppBuilder

## Description

Convert `NuruCoreAppBuilder` to use CRTP (Curiously Recurring Template Pattern) so fluent methods preserve the derived type throughout the chain. This eliminates the need for override boilerplate and enables perfect type preservation in the fluent API.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Parent

160-unify-builders-with-ibuilder-pattern

## Problem

Currently, when `NuruAppBuilder` (which extends `NuruCoreAppBuilder`) calls fluent methods, the return type is `NuruCoreAppBuilder`, losing the derived type:

```csharp
NuruApp.CreateBuilder(args)
    .Map(r => r.WithLiteral("deploy").Done())  // Returns EndpointBuilder
    .WithHandler(...)
    .Done()  // Returns NuruCoreAppBuilder, NOT NuruAppBuilder!
    .AddReplSupport()  // Extension method might not work correctly
```

This forces `NuruAppBuilder` to override every fluent method (see `nuru-app-builder.overrides.cs` - 102 lines of boilerplate).

## Solution: CRTP

```csharp
// Base class takes itself as generic parameter
public partial class NuruCoreAppBuilder<TSelf> 
    where TSelf : NuruCoreAppBuilder<TSelf>
{
    public virtual TSelf AddAutoHelp() { ... return (TSelf)this; }
    public virtual TSelf AddConfiguration(...) { ... return (TSelf)this; }
    // All fluent methods return TSelf
}

// Derived class passes itself
public partial class NuruAppBuilder : NuruCoreAppBuilder<NuruAppBuilder>
{
    // Inherits all methods returning NuruAppBuilder - no overrides needed!
}
```

## Target Consumer API

```csharp
// Perfect type preservation - no explicit generics needed
NuruApp.CreateBuilder(args)
    .Map(r => r.WithLiteral("deploy").WithParameter("env").Done())
    .WithHandler((string env) => Deploy(env))
    .Done()  // Returns NuruAppBuilder!
    .Map(r => r.WithLiteral("status").Done())
    .WithHandler(() => "OK")
    .Done()  // Still NuruAppBuilder!
    .AddReplSupport()  // Extension methods work!
    .Build();
```

## Impact Analysis

### Classes to Modify

| Class | Change |
|-------|--------|
| `NuruCoreAppBuilder` | Add `<TSelf>` generic parameter |
| `NuruAppBuilder` | Change to `: NuruCoreAppBuilder<NuruAppBuilder>` |
| `EndpointBuilder<TBuilder>` | Update constraint to `NuruCoreAppBuilder<TBuilder>` |

### Methods That Return `NuruCoreAppBuilder` (Change to `TSelf`)

**nuru-core-app-builder.cs:**
- `AddAutoHelp()`
- `ConfigureHelp()`
- `WithMetadata()`

**nuru-core-app-builder.routes.cs:**
- `AddReplOptions()`
- `MapMultiple()` (3 overloads)
- `AddTypeConverter()`

**nuru-core-app-builder.configuration.cs:**
- `AddConfiguration()`
- `AddDependencyInjection()`
- `ConfigureServices()` (2 overloads)
- `UseLogging()`
- `UseTerminal()`

### Files to Delete

- `source/timewarp-nuru/nuru-app-builder.overrides.cs` (102 lines) - no longer needed

### Extension Methods to Update Constraints

All extension methods with `where TBuilder : NuruCoreAppBuilder` need update to `where TBuilder : NuruCoreAppBuilder<TBuilder>`:

- `timewarp-nuru-completion/nuru-app-builder-extensions.cs`
- `timewarp-nuru-repl/nuru-app-extensions.cs`
- `timewarp-nuru-logging/nuru-logging-extensions.cs`
- `timewarp-nuru-telemetry/nuru-telemetry-extensions.cs`
- `timewarp-nuru/nuru-app-builder-extensions.cs`

### Factory Methods

| Method | Current Return | Decision Needed |
|--------|---------------|-----------------|
| `NuruApp.CreateBuilder()` | `NuruAppBuilder` | Keep as-is |
| `NuruCoreApp.CreateSlimBuilder()` | `NuruCoreAppBuilder` | Need non-generic alias |
| `NuruCoreApp.CreateEmptyBuilder()` | `NuruCoreAppBuilder` | Need non-generic alias |

**Solution:** Create non-generic alias class:
```csharp
public class NuruCoreAppBuilder : NuruCoreAppBuilder<NuruCoreAppBuilder> 
{ 
    // Constructors only - for factory methods
}
```

## Checklist

### Phase 1: Core CRTP Implementation
- [x] Create `NuruCoreAppBuilder<TSelf>` with generic parameter
- [x] Update all fluent methods to return `TSelf`
- [x] Create non-generic `NuruCoreAppBuilder` alias for factory methods
- [x] Update `NuruAppBuilder` to extend `NuruCoreAppBuilder<NuruAppBuilder>`
- [x] Delete `nuru-app-builder.overrides.cs`

### Phase 2: Update Dependent Types
- [x] Update `EndpointBuilder<TBuilder>` constraint
- [x] Update `NestedCompiledRouteBuilder<TParent>` if needed
- [x] Update factory methods in `NuruCoreApp` and `NuruApp`

### Phase 3: Update Extension Methods
- [x] Update `timewarp-nuru-completion` extension constraints
- [x] Update `timewarp-nuru-repl` extension constraints
- [x] Update `timewarp-nuru-logging` extension constraints
- [x] Update `timewarp-nuru-telemetry` extension constraints
- [x] Update `timewarp-nuru` extension constraints

### Phase 4: Testing
- [x] Build solution and fix compile errors
- [x] Run test suite
- [x] Update tests that use `NuruCoreAppBuilder` variable declarations
- [ ] Add test verifying type preservation through fluent chain

### Phase 5: Cleanup
- [x] Remove any obsolete internal typed methods (`MapInternalTyped`, `MapNestedTyped`, etc.)
- [ ] Update documentation

## Context from Previous Work

### What Was Done in Task 161

Task 161 (Apply IBuilder to CompiledRouteBuilder) established:

1. **Two interfaces:**
   - `IBuilder<TBuilt>` - standalone builders with `Build()`
   - `INestedBuilder<TParent>` - nested builders with `Done()`

2. **Created:**
   - `source/timewarp-nuru-core/fluent/i-builder.cs`
   - `source/timewarp-nuru-core/fluent/i-nested-builder.cs`
   - `source/timewarp-nuru-core/nested-compiled-route-builder.cs`

3. **Updated:**
   - `CompiledRouteBuilder` implements `IBuilder<CompiledRoute>`
   - `EndpointBuilder` implements `INestedBuilder<TParent>`
   - Added `Map(Func<NestedCompiledRouteBuilder<...>>)` overload

4. **Discovered Issue:**
   - The nested builder `Map()` returns `EndpointBuilder` (non-generic)
   - `Done()` returns `NuruCoreAppBuilder`, losing derived type
   - Added `MapNestedTyped<TBuilder>()` internal method as workaround
   - This workaround is ugly - CRTP is the proper solution

### Files Created/Modified in Task 161

```
source/timewarp-nuru-core/fluent/i-builder.cs (NEW)
source/timewarp-nuru-core/fluent/i-nested-builder.cs (NEW - renamed from i-builder.cs)
source/timewarp-nuru-core/nested-compiled-route-builder.cs (NEW)
source/timewarp-nuru-core/compiled-route-builder.cs (added IBuilder<CompiledRoute>)
source/timewarp-nuru-core/endpoint-builder.cs (IBuilder -> INestedBuilder)
source/timewarp-nuru-core/nuru-core-app-builder.routes.cs (added Map with nested builder)
```

### Builder Interface Table

| Interface | Purpose | Method | Example |
|-----------|---------|--------|---------|
| `IBuilder<TBuilt>` | Creates TBuilt | `Build()` | `CompiledRouteBuilder` |
| `INestedBuilder<TParent>` | Returns to parent | `Done()` | `NestedCompiledRouteBuilder<T>` |

### Naming Convention

- **Standalone:** `{Thing}Builder` - e.g., `CompiledRouteBuilder`
- **Nested:** `Nested{Thing}Builder<TParent>` - e.g., `NestedCompiledRouteBuilder<TParent>`

## Notes

### Why CRTP Over Alternatives

| Approach | Downside |
|----------|----------|
| Explicit generic `Map<NuruAppBuilder>(...)` | Ugly, redundant |
| Extension methods on derived type | Inconsistent API |
| Accept type loss | "Accept" doesn't build best library |
| **CRTP** | Proper solution, one-time refactor |

### Reference: Existing CRTP-like Pattern

The extension methods already use a similar pattern:
```csharp
public static TBuilder AddReplSupport<TBuilder>(this TBuilder builder)
    where TBuilder : NuruCoreAppBuilder
```

CRTP formalizes this at the base class level.

### Open Question

For factory methods `CreateSlimBuilder()` / `CreateEmptyBuilder()` that return `NuruCoreAppBuilder`:
- A) Keep returning non-generic `NuruCoreAppBuilder` (the alias class)?
- B) Make them generic too?
- C) Deprecate them in favor of `NuruApp.CreateBuilder()`?
