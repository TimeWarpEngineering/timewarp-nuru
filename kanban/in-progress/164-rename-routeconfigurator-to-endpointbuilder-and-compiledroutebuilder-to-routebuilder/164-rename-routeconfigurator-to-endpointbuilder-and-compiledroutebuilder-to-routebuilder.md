# Rename Builders to Match What They Build

## Description

Current builder names are misleading:
- `RouteConfigurator` actually configures an `Endpoint` (handler, message type, metadata)
- `CompiledRouteBuilder` builds a `CompiledRoute` (pattern segments only)

Rename for clarity:
- `RouteConfigurator` → `EndpointBuilder`
- `CompiledRouteBuilder` → `RouteBuilder` (or `PatternBuilder`)

This is a breaking change appropriate for Nuru 3.0.

## Rationale

```csharp
// Current - confusing names
.Map("pattern", handler)        // Returns RouteConfigurator (but configures Endpoint!)
    .AsQuery()                  // Sets Endpoint.MessageType

// After rename - names match what they do
.Map(route => route             // RouteBuilder builds CompiledRoute
    .WithLiteral("deploy")
    .WithParameter("env"))
    .WithHandler(handler)       // EndpointBuilder configures Endpoint
    .AsQuery()                  // EndpointBuilder configures Endpoint
    .Done()
```

## Checklist

- [ ] Rename `RouteConfigurator` → `EndpointBuilder`
- [ ] Rename `RouteConfigurator<TBuilder>` → `EndpointBuilder<TBuilder>`
- [ ] Rename `CompiledRouteBuilder` → `RouteBuilder`
- [ ] Update all usages in source code
- [ ] Update all tests
- [ ] Update documentation
- [ ] Update samples
- [ ] Add `WithHandler(Delegate)` method to `EndpointBuilder`
- [ ] Add `Map(Action<RouteBuilder>)` overload that returns `EndpointBuilder`

## Impact

| File | Changes |
|------|---------|
| `route-configurator.cs` | Rename class |
| `compiled-route-builder.cs` | Rename class |
| `nuru-core-app-builder.routes.cs` | Update return types, add overload |
| `nuru-app-extensions.cs` | Update extension methods |
| `nuru-app-builder-extensions.cs` | Update extension methods |
| Tests | Update all references |
| Samples | Update all references |
| Docs | Update all references |

## Notes

### Naming Alternatives

| Current | Option A | Option B |
|---------|----------|----------|
| `RouteConfigurator` | `EndpointBuilder` | `EndpointConfigurator` |
| `CompiledRouteBuilder` | `RouteBuilder` | `PatternBuilder` |

Recommend Option A - "Builder" is consistent with the `IBuilder<T>` pattern we're using.

### Related

- Task 158: Introduced `IBuilder<TParent>` and generic `RouteConfigurator<TBuilder>`
- Task 161: Apply IBuilder to CompiledRouteBuilder (do rename first)

## Parent

148-nuru-3-unified-route-pipeline
