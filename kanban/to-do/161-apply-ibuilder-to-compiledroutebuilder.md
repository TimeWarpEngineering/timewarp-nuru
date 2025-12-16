# Apply IBuilder to CompiledRouteBuilder

## Description

Make `CompiledRouteBuilder` implement `IBuilder<TBuilder>` to enable nested fluent chaining when defining routes programmatically.

## Parent

160-unify-builders-with-ibuilder-pattern

## Current API

```csharp
// Standalone usage (keep supporting this)
var route = new CompiledRouteBuilder()
    .WithLiteral("deploy")
    .WithParameter("env")
    .Build();

// Then register separately
app.Map(route, handler);
```

## Target API

```csharp
// Nested fluent usage with Done()
app.MapRoute(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force", "f"))
    .AsCommand()              // RouteConfigurator methods
    .Done()                   // Returns to app builder
    .AddReplSupport()
    .Build();

// Or inline with Also()
app.MapRoute(r => r
        .WithLiteral("deploy")
        .WithParameter("env"),
    handler)
    .Also(rc => rc.AsCommand())
    .Done()
    .AddReplSupport();
```

## Checklist

### Implementation
- [ ] Make `CompiledRouteBuilder` generic: `CompiledRouteBuilder<TBuilder>`
- [ ] Implement `IBuilder<TBuilder>` with `Done()` method
- [ ] Keep non-generic `CompiledRouteBuilder` for standalone usage (backward compat)
- [ ] Add `MapRoute(Action<CompiledRouteBuilder<TBuilder>>, Delegate)` overload to `NuruCoreAppBuilder`
- [ ] Add `MapRoute<TCommand>(Action<CompiledRouteBuilder<TBuilder>>)` overload for Mediator commands
- [ ] Ensure `Done()` returns `RouteConfigurator<TBuilder>` for post-config (AsQuery, etc.)

### Testing
- [ ] Test standalone `CompiledRouteBuilder` still works
- [ ] Test nested `MapRoute()` with `Done()` chaining
- [ ] Test `MapRoute()` with Mediator commands
- [ ] Test combining with `AsQuery()`, `AsCommand()`, etc.

### Documentation
- [ ] Update XML docs on new methods
- [ ] Add examples to user documentation

## Notes

### Pattern: Factory

`CompiledRouteBuilder` uses the **Factory** pattern:
- Accumulates state via `WithX()` methods
- Creates `CompiledRoute` at the end via `Build()` or implicitly via `Done()`

### Integration with RouteConfigurator

The flow should be:
1. `app.MapRoute(configure)` calls configure action
2. Builder creates `CompiledRoute`
3. Returns `RouteConfigurator<TBuilder>` for post-config
4. `Done()` returns to `TBuilder`

```
app.MapRoute(r => r.WithLiteral("x"))  →  RouteConfigurator<TBuilder>
    .AsQuery()                          →  RouteConfigurator<TBuilder>
    .Done()                             →  TBuilder
    .AddReplSupport()                   →  TBuilder
```

### Relationship to Task 153

Task 153 (Fluent Builder API Phase 4) defines `Map(Action<CompiledRouteBuilder>, Delegate)`. This task implements the `IBuilder<TBuilder>` foundation that Task 153 builds upon.
