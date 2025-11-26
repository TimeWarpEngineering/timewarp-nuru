# Consolidate AddRoute to Map API

## Description

Replace `AddRoute()` methods with `Map()` to match ASP.NET Core's minimal API pattern. This reduces API surface area and provides a single, familiar way to register routes.

## Method Renames

| Before | After |
|--------|-------|
| `AddRoute(pattern, handler)` | `Map(pattern, handler)` |
| `AddRoute<TCommand>(pattern)` | `Map<TCommand>(pattern)` |
| `AddRoute<TCommand, TResponse>(pattern)` | `Map<TCommand, TResponse>(pattern)` |
| `AddRoutes(patterns, handler)` | `Map(patterns, handler)` |
| `AddRoutes<TCommand>(patterns)` | `Map<TCommand>(patterns)` |
| `AddRoutes<TCommand, TResponse>(patterns)` | `Map<TCommand, TResponse>(patterns)` |
| `AddDefaultRoute(handler)` | `MapDefault(handler)` |

## Checklist

### Phase 1: Library Refactoring (LSP Rename)
- [ ] Rename methods in `NuruAppBuilder.Routes.cs` using LSP
- [ ] Verify Source/ compiles

### Phase 2: Samples/Tests/Docs (sed)
- [ ] Update all files in `Samples/`
- [ ] Update all files in `Tests/`
- [ ] Update `README.md`
- [ ] Update files in `documentation/`

### Phase 3: Verification
- [ ] Build solution
- [ ] Run tests
- [ ] Verify samples work

## Notes

- Backward compatibility is NOT a concern
- Goal is to reduce tech debt and simplify API
- Keep internal `AddRouteInternal()` as implementation detail
- Remove alias comments from Map methods (they become primary)
