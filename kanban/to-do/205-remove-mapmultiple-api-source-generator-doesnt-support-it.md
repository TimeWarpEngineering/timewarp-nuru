# Remove MapMultiple API - source generator doesn't support it

## Description

Remove the `MapMultiple` API methods. They are just syntactic sugar for a foreach loop calling `Map`, the source generator doesn't support them, and they add unnecessary API surface complexity.

**Why remove:**
- Source generator (`NuruInvokerGenerator`) doesn't handle `MapMultiple` calls
- It's literally just a foreach loop - no special behavior
- Help grouping is based on description matching, not registration method
- Users can call `Map` multiple times with `.WithDescription("Same text")` to get alias grouping
- Simplifies the API surface for 3.0

## Checklist

- [ ] Remove `MapMultiple(string[] patterns, Delegate handler)` from `NuruCoreAppBuilder`
- [ ] Remove `MapMultiple<TCommand>(string[] patterns)` from `NuruCoreAppBuilder`
- [ ] Remove `MapMultiple<TCommand, TResponse>(string[] patterns)` from `NuruCoreAppBuilder`
- [ ] Remove `MapMultiple` from `EndpointBuilder`
- [ ] Update comments in `nuru-app-extensions.cs` that reference `MapMultiple`
- [ ] Update test comments in `repl-34-interactive-route-alias.cs`
- [ ] Run tests to ensure nothing breaks
- [ ] Update any documentation mentioning `MapMultiple`

## Notes

**Files to modify:**
- `source/timewarp-nuru-core/nuru-core-app-builder.routes.cs` - remove 3 methods
- `source/timewarp-nuru-core/endpoint-builder.cs` - remove wrapper method
- `source/timewarp-nuru-repl/nuru-app-extensions.cs` - update comments
- `tests/timewarp-nuru-repl-tests/repl-34-interactive-route-alias.cs` - update comments

**Migration path for users:**
```csharp
// Before:
builder.MapMultiple(["exit", "quit", "q"], handler);

// After:
builder.Map("exit", handler).WithDescription("Exit the application").Done();
builder.Map("quit", handler).WithDescription("Exit the application").Done();
builder.Map("q", handler).WithDescription("Exit the application").Done();
```

Routes with the same description are automatically grouped in help output.
