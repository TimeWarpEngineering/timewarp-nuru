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

- [x] Rename `RouteConfigurator` → `EndpointBuilder`
- [x] Rename `RouteConfigurator<TBuilder>` → `EndpointBuilder<TBuilder>`
- [x] Rename `CompiledRouteBuilder` → `RouteBuilder`
- [x] Update all usages in source code
- [x] Update all tests
- [ ] Update documentation
- [x] Update samples
- [ ] Add `WithHandler(Delegate)` method to `EndpointBuilder`
- [ ] Add `Map(Action<RouteBuilder>)` overload that returns `EndpointBuilder`

## Impact

| File | Changes |
|------|---------|
| `route-configurator.cs` → `endpoint-builder.cs` | ✅ Renamed class and file |
| `compiled-route-builder.cs` → `route-builder.cs` | ✅ Renamed class and file |
| `nuru-core-app-builder.routes.cs` | ✅ Updated return types |
| `nuru-app-extensions.cs` | ✅ Updated extension methods |
| `nuru-app-builder-extensions.cs` | ✅ Updated extension methods |
| `nuru-attributed-route-generator.cs` | ✅ Updated generated code |
| Tests | ✅ Updated all references |
| Samples | ✅ Updated all references |
| Docs | ⏳ Pending |

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
