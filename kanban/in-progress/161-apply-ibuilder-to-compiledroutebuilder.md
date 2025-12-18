# Apply IBuilder to CompiledRouteBuilder

## Description

Make `CompiledRouteBuilder` implement `IBuilder<TParent>` for consistent fluent API across all Nuru builders.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Parent

160-unify-builders-with-ibuilder-pattern

## Current State (after Task 164)

```csharp
// CompiledRouteBuilder is standalone with Build()
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
// CompiledRouteBuilder<TParent> implements IBuilder<TParent>
// Done() builds the route, sets it on parent, returns parent
builder
    .Map(r => r
        .WithLiteral("deploy")
        .WithParameter("env")
        .Done()              // Builds route, returns EndpointBuilder
    )
    .WithHandler(handler)
    .AsCommand()
    .Done();                 // Returns to app builder
```

## Design

### CompiledRouteBuilder<TParent> Pattern

Following `TestAddressBuilder` pattern from `timewarp-fluent-builder`:

```csharp
public sealed class CompiledRouteBuilder<TParent> : IBuilder<TParent>
    where TParent : class
{
    private readonly TParent _parent;
    private readonly Action<CompiledRoute> _onBuild;
    
    // ... existing state fields ...

    internal CompiledRouteBuilder(TParent parent, Action<CompiledRoute> onBuild)
    {
        _parent = parent;
        _onBuild = onBuild;
    }

    // All WithX() methods return CompiledRouteBuilder<TParent>
    public CompiledRouteBuilder<TParent> WithLiteral(string value) { ... return this; }
    public CompiledRouteBuilder<TParent> WithParameter(...) { ... return this; }
    public CompiledRouteBuilder<TParent> WithOption(...) { ... return this; }
    public CompiledRouteBuilder<TParent> WithCatchAll(...) { ... return this; }

    public TParent Done()
    {
        CompiledRoute route = Build();  // Internal build
        _onBuild(route);                 // Pass to parent
        return _parent;
    }

    private CompiledRoute Build() { ... }  // Now private
}
```

### Updated Map() Overload

```csharp
// In NuruCoreAppBuilder
public virtual EndpointBuilder Map(Func<CompiledRouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
{
    Endpoint endpoint = new() { /* minimal init */ };
    EndpointCollection.Add(endpoint);
    
    var endpointBuilder = new EndpointBuilder(this, endpoint);
    var routeBuilder = new CompiledRouteBuilder<EndpointBuilder>(
        endpointBuilder,
        route => endpoint.CompiledRoute = route
    );
    
    return configure(routeBuilder);  // User calls Done() which returns EndpointBuilder
}
```

### Usage Flow

```
builder.Map(r => r                    // CompiledRouteBuilder<EndpointBuilder>
    .WithLiteral("deploy")            // CompiledRouteBuilder<EndpointBuilder>
    .WithParameter("env")             // CompiledRouteBuilder<EndpointBuilder>
    .Done()                           // Builds route, returns EndpointBuilder
)                                     // EndpointBuilder
.WithHandler(handler)                 // EndpointBuilder  
.AsCommand()                          // EndpointBuilder
.Done()                               // TBuilder (app builder)
```

## Checklist

### Implementation
- [ ] Create `CompiledRouteBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Move all `WithX()` methods to return `CompiledRouteBuilder<TParent>`
- [ ] Add `Done()` method that builds route, passes to callback, returns parent
- [ ] Make `Build()` private (or remove if not needed standalone)
- [ ] Update `Map(Action<CompiledRouteBuilder>)` to `Map(Func<CompiledRouteBuilder<EndpointBuilder>, EndpointBuilder>)`
- [ ] Remove non-generic `CompiledRouteBuilder` (no backward compat needed)
- [ ] Generate route pattern string in `Done()` for help display

### Testing
- [ ] Test `Map(r => r.WithLiteral().Done()).WithHandler().Done()`
- [ ] Test full fluent chain with `AsQuery()`, `AsCommand()`
- [ ] Test route pattern generation for help
- [ ] Update existing CompiledRouteBuilder tests

### Documentation
- [ ] Update XML docs
- [ ] Update task 160 epic example to match

## Notes

### Why Func instead of Action?

```csharp
// Action pattern (current) - user doesn't call Done()
Map(Action<CompiledRouteBuilder> configure)

// Func pattern (target) - user must call Done()
Map(Func<CompiledRouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
```

The `Func` pattern:
1. Forces user to call `Done()` - can't forget it
2. Type system enforces correct return
3. Matches the nested builder pattern from `timewarp-fluent-builder`

### Relationship to Task 164

Task 164 added:
- `EndpointBuilder.WithHandler()`
- `Map(Action<CompiledRouteBuilder>)` returning `EndpointBuilder`

This task evolves that to:
- `CompiledRouteBuilder<TParent>` with `IBuilder<TParent>`
- `Map(Func<...>)` pattern requiring `Done()`

### No Standalone CompiledRouteBuilder

Since we don't care about backward compat:
- Remove non-generic `CompiledRouteBuilder`
- Only `CompiledRouteBuilder<TParent>` exists
- If someone needs standalone route building, they can use `PatternParser.Parse()`
