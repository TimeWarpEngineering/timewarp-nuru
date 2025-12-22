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

- [x] Add tests validating calling `Map` multiple times with same handler works properly
  - [x] Test: multiple patterns with same handler all route correctly
  - [x] Test: multiple patterns with same description are grouped in help output
  - [x] Test: execution invokes the correct shared handler
- [x] Remove `MapMultiple(string[] patterns, Delegate handler)` from `NuruCoreAppBuilder`
- [x] Remove `MapMultiple<TCommand>(string[] patterns)` from `NuruCoreAppBuilder`
- [x] Remove `MapMultiple<TCommand, TResponse>(string[] patterns)` from `NuruCoreAppBuilder`
- [x] Remove `MapMultiple` from `EndpointBuilder`
- [x] Update comments in `nuru-app-extensions.cs` that reference `MapMultiple`
- [x] Update test comments in `repl-34-interactive-route-alias.cs`
- [x] Run tests to ensure nothing breaks
- [x] Update any documentation mentioning `MapMultiple` (none found)

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

## Results

**Completed (Dec 2025):**
- Added new test file `routing-23-multiple-map-same-handler.cs` with 5 tests validating:
  - Multiple patterns with same handler route correctly
  - Help output groups patterns with same description
  - Parameters pass correctly to shared handlers
  - Async handlers work with multiple patterns
  - Different descriptions create separate help groups
- Removed all 3 `MapMultiple` overloads from `NuruCoreAppBuilder`
- Removed `MapMultiple` wrapper from `EndpointBuilder`
- Updated comments in `nuru-app-extensions.cs` and `repl-34-interactive-route-alias.cs`
- All 1689 tests pass (1673 passed, 16 skipped)
