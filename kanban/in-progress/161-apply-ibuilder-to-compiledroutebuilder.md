# Apply IBuilder to RouteBuilder

## Description

Make `RouteBuilder` implement `IBuilder<TParent>` for consistent fluent API across all Nuru builders.

**No backward compatibility concerns** - we're in beta, prioritize the best API.

## Parent

160-unify-builders-with-ibuilder-pattern

## Current State (after Task 164)

```csharp
// RouteBuilder is standalone with Build()
var route = new RouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .Build();  // Returns CompiledRoute

// Map() overload takes Action<RouteBuilder>
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
// RouteBuilder<TParent> implements IBuilder<TParent>
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

### RouteBuilder<TParent> Pattern

Following `TestAddressBuilder` pattern from `timewarp-fluent-builder`:

```csharp
public sealed class RouteBuilder<TParent> : IBuilder<TParent>
    where TParent : class
{
    private readonly TParent _parent;
    private readonly Action<CompiledRoute> _onBuild;
    
    // ... existing state fields ...

    internal RouteBuilder(TParent parent, Action<CompiledRoute> onBuild)
    {
        _parent = parent;
        _onBuild = onBuild;
    }

    // All WithX() methods return RouteBuilder<TParent>
    public RouteBuilder<TParent> WithLiteral(string value) { ... return this; }
    public RouteBuilder<TParent> WithParameter(...) { ... return this; }
    public RouteBuilder<TParent> WithOption(...) { ... return this; }
    public RouteBuilder<TParent> WithCatchAll(...) { ... return this; }

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
public virtual EndpointBuilder Map(Func<RouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
{
    Endpoint endpoint = new() { /* minimal init */ };
    EndpointCollection.Add(endpoint);
    
    var endpointBuilder = new EndpointBuilder(this, endpoint);
    var routeBuilder = new RouteBuilder<EndpointBuilder>(
        endpointBuilder,
        route => endpoint.CompiledRoute = route
    );
    
    return configure(routeBuilder);  // User calls Done() which returns EndpointBuilder
}
```

### Usage Flow

```
builder.Map(r => r                    // RouteBuilder<EndpointBuilder>
    .WithLiteral("deploy")            // RouteBuilder<EndpointBuilder>
    .WithParameter("env")             // RouteBuilder<EndpointBuilder>
    .Done()                           // Builds route, returns EndpointBuilder
)                                     // EndpointBuilder
.WithHandler(handler)                 // EndpointBuilder  
.AsCommand()                          // EndpointBuilder
.Done()                               // TBuilder (app builder)
```

## Checklist

### Implementation
- [ ] Create `RouteBuilder<TParent>` implementing `IBuilder<TParent>`
- [ ] Move all `WithX()` methods to return `RouteBuilder<TParent>`
- [ ] Add `Done()` method that builds route, passes to callback, returns parent
- [ ] Make `Build()` private (or remove if not needed standalone)
- [ ] Update `Map(Action<RouteBuilder>)` to `Map(Func<RouteBuilder<EndpointBuilder>, EndpointBuilder>)`
- [ ] Remove non-generic `RouteBuilder` (no backward compat needed)
- [ ] Generate route pattern string in `Done()` for help display

### Testing
- [ ] Test `Map(r => r.WithLiteral().Done()).WithHandler().Done()`
- [ ] Test full fluent chain with `AsQuery()`, `AsCommand()`
- [ ] Test route pattern generation for help
- [ ] Update existing RouteBuilder tests

### Documentation
- [ ] Update XML docs
- [ ] Update task 160 epic example to match

## Notes

### Why Func instead of Action?

```csharp
// Action pattern (current) - user doesn't call Done()
Map(Action<RouteBuilder> configure)

// Func pattern (target) - user must call Done()
Map(Func<RouteBuilder<EndpointBuilder>, EndpointBuilder> configure)
```

The `Func` pattern:
1. Forces user to call `Done()` - can't forget it
2. Type system enforces correct return
3. Matches the nested builder pattern from `timewarp-fluent-builder`

### Relationship to Task 164

Task 164 added:
- `EndpointBuilder.WithHandler()`
- `Map(Action<RouteBuilder>)` returning `EndpointBuilder`

This task evolves that to:
- `RouteBuilder<TParent>` with `IBuilder<TParent>`
- `Map(Func<...>)` pattern requiring `Done()`

### No Standalone RouteBuilder

Since we don't care about backward compat:
- Remove non-generic `RouteBuilder`
- Only `RouteBuilder<TParent>` exists
- If someone needs standalone route building, they can use `PatternParser.Parse()`
