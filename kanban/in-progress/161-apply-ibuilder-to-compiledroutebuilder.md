# Establish Builder Interface Pattern and Apply to CompiledRouteBuilder

## Description

Establish the two-interface builder pattern (`IBuilder<TBuilt>` and `INestedBuilder<TParent>`) and apply it to `CompiledRouteBuilder` as the first implementation.

- **`IBuilder<TBuilt>`** - Top-level builders that create objects via `Build()`
- **`INestedBuilder<TParent>`** - Nested builders that return to parent via `Done()`

Nested builders use **composition** - they wrap a standalone builder and add the return-to-parent capability. This avoids code duplication while keeping clear API surfaces.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Parent

160-unify-builders-with-ibuilder-pattern

## Interface Design

```csharp
// Top-level builder - creates TBuilt
public interface IBuilder<out TBuilt>
{
    TBuilt Build();
}

// Nested builder - returns to TParent (wraps a standalone builder internally)
public interface INestedBuilder<out TParent> where TParent : class
{
    TParent Done();  // Done() = Build() + pass result to parent + return parent
}
```

## Current State

```csharp
// CompiledRouteBuilder is standalone with Build() but no interface
var route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .Build();  // Returns CompiledRoute

// Map() overload takes Action<CompiledRouteBuilder>
builder
    .Map(r => r
        .WithLiteral("deploy")
        .WithParameter("env")
    )
    .WithHandler(handler)
    .Done();
```

## Target API

```csharp
// Standalone builder (for tests, source generators, static routes)
CompiledRoute route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .Build();

// Nested builder (for fluent app configuration)
builder
    .Map(r => r
        .WithLiteral("deploy")
        .WithParameter("env")
        .Done()              // Builds route, passes to parent, returns EndpointBuilder
    )
    .WithHandler(handler)
    .AsCommand()
    .Done();                 // Returns to app builder
```

## Design

### Composition Pattern (Nested wraps Standalone)

```csharp
// Standalone - full implementation, implements IBuilder<CompiledRoute>
public sealed class CompiledRouteBuilder : IBuilder<CompiledRoute>
{
    private readonly List<RouteMatcher> _segments = [];
    private int _specificity;
    // ... all state lives here ...

    public CompiledRouteBuilder WithLiteral(string value) { ... return this; }
    public CompiledRouteBuilder WithParameter(...) { ... return this; }
    public CompiledRouteBuilder WithOption(...) { ... return this; }
    public CompiledRouteBuilder WithCatchAll(...) { ... return this; }
    public CompiledRouteBuilder WithMessageType(...) { ... return this; }

    public CompiledRoute Build() { ... }
}

// Nested - wraps standalone, implements INestedBuilder<TParent>
public sealed class NestedCompiledRouteBuilder<TParent> : INestedBuilder<TParent>
    where TParent : class
{
    private readonly CompiledRouteBuilder _inner = new();  // Composition!
    private readonly TParent _parent;
    private readonly Action<CompiledRoute> _onBuild;

    internal NestedCompiledRouteBuilder(TParent parent, Action<CompiledRoute> onBuild)
    {
        _parent = parent;
        _onBuild = onBuild;
    }

    // Thin wrappers delegate to _inner
    public NestedCompiledRouteBuilder<TParent> WithLiteral(string value)
    {
        _inner.WithLiteral(value);
        return this;
    }
    public NestedCompiledRouteBuilder<TParent> WithParameter(...) { _inner.WithParameter(...); return this; }
    public NestedCompiledRouteBuilder<TParent> WithOption(...) { _inner.WithOption(...); return this; }
    public NestedCompiledRouteBuilder<TParent> WithCatchAll(...) { _inner.WithCatchAll(...); return this; }
    public NestedCompiledRouteBuilder<TParent> WithMessageType(...) { _inner.WithMessageType(...); return this; }

    public TParent Done()
    {
        CompiledRoute route = _inner.Build();  // Delegate to standalone
        _onBuild(route);                        // Pass to parent
        return _parent;                         // Return to parent
    }
}
```

### Updated Map() Overload

```csharp
// In NuruCoreAppBuilder
public virtual EndpointBuilder Map(
    Func<NestedCompiledRouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
{
    Endpoint endpoint = new() { /* minimal init */ };
    EndpointCollection.Add(endpoint);

    var endpointBuilder = new EndpointBuilder(this, endpoint);
    var routeBuilder = new NestedCompiledRouteBuilder<EndpointBuilder>(
        endpointBuilder,
        route => endpoint.CompiledRoute = route
    );

    return configure(routeBuilder);  // User calls Done() which returns EndpointBuilder
}
```

### Usage Flow

```
builder.Map(r => r                    // NestedCompiledRouteBuilder<EndpointBuilder>
    .WithLiteral("deploy")            // NestedCompiledRouteBuilder<EndpointBuilder>
    .WithParameter("env")             // NestedCompiledRouteBuilder<EndpointBuilder>
    .Done()                           // Builds route, returns EndpointBuilder
)                                     // EndpointBuilder
.WithHandler(handler)                 // EndpointBuilder
.AsCommand()                          // EndpointBuilder
.Done()                               // TBuilder (app builder)
```

## Checklist

### Interfaces
- [ ] Create `IBuilder<TBuilt>` at `source/timewarp-nuru-core/fluent/i-builder.cs`
- [ ] Rename existing `IBuilder<TParent>` to `INestedBuilder<TParent>` at `source/timewarp-nuru-core/fluent/i-nested-builder.cs`
- [ ] Update `EndpointBuilder<TBuilder>` to implement `INestedBuilder<TBuilder>` (rename)
- [ ] Update `EndpointBuilder` to implement `INestedBuilder<NuruCoreAppBuilder>` (rename)

### CompiledRouteBuilder (Standalone)
- [ ] Add `IBuilder<CompiledRoute>` to existing `CompiledRouteBuilder`
- [ ] Keep all existing functionality (no changes needed beyond interface)

### NestedCompiledRouteBuilder (New)
- [ ] Create `NestedCompiledRouteBuilder<TParent>` implementing `INestedBuilder<TParent>`
- [ ] Use composition - wrap `CompiledRouteBuilder` internally
- [ ] Add thin wrapper methods for all `WithX()` methods
- [ ] Implement `Done()` that calls `_inner.Build()`, passes to callback, returns parent

### NuruCoreAppBuilder
- [ ] Update `Map(Action<CompiledRouteBuilder>)` to `Map(Func<NestedCompiledRouteBuilder<EndpointBuilder>, EndpointBuilder>)`
- [ ] Generate route pattern string in endpoint for help display

### Testing
- [ ] Test standalone: `new CompiledRouteBuilder().WithLiteral().Build()`
- [ ] Test nested: `Map(r => r.WithLiteral().Done()).WithHandler().Done()`
- [ ] Test full fluent chain with `AsQuery()`, `AsCommand()`
- [ ] Test route pattern generation for help
- [ ] Update existing CompiledRouteBuilder tests

### Documentation
- [ ] Update XML docs for both interfaces
- [ ] Update XML docs for both builder classes
- [ ] Update task 160 epic example to match

## Notes

### Why Two Interfaces?

| Interface               | Purpose                      | Method    | Example                        |
|-------------------------|------------------------------|-----------|--------------------------------|
| `IBuilder<TBuilt>`      | Creates TBuilt               | `Build()` | `CompiledRouteBuilder`         |
| `INestedBuilder<TParent>` | Returns to parent when done | `Done()`  | `NestedCompiledRouteBuilder<T>` |

### Why Composition?

- **No code duplication** - nested wraps standalone
- **Clear API surface** - standalone has `Build()`, nested has `Done()`
- **Simple mental model** - `Done()` = `Build()` + return to parent
- **Avoids CRTP complexity** - no generic base class gymnastics

### Why Func instead of Action?

```csharp
// Action pattern (current) - user doesn't call Done()
Map(Action<CompiledRouteBuilder> configure)

// Func pattern (target) - user must call Done()
Map(Func<NestedCompiledRouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
```

The `Func` pattern:
1. Forces user to call `Done()` - can't forget it
2. Type system enforces correct return
3. Consistent with nested builder pattern

### Naming Convention

- **Standalone**: `{Thing}Builder` - e.g., `CompiledRouteBuilder`
- **Nested**: `Nested{Thing}Builder<TParent>` - e.g., `NestedCompiledRouteBuilder<TParent>`

### Use Cases for Standalone Builder

```csharp
// 1. Unit testing route matching
var route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .Build();
Assert.True(route.Match(["deploy"]).IsMatch);

// 2. Source generator emits static routes (Task 151)
private static readonly CompiledRoute __Route_Deploy = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .Build();

// 3. Pre-computed routes for performance
public static class Routes
{
    public static readonly CompiledRoute Status = new CompiledRouteBuilder()
        .WithLiteral("status")
        .Build();
}
```
