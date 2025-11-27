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
- [x] Rename methods in `NuruAppBuilder.Routes.cs` using LSP
- [x] Verify Source/ compiles

### Phase 2: Samples/Tests/Docs (sed)
- [x] Update all files in `Samples/`
- [x] Update all files in `Tests/`
- [x] Update `README.md`
- [x] Update files in `documentation/`

### Phase 3: Verification
- [x] Build solution
- [x] Run tests
- [x] Verify samples work

## Notes

- Backward compatibility is NOT a concern
- Goal is to reduce tech debt and simplify API
- Internal methods renamed: `AddRouteInternal` → `MapInternal`, `AddMediatorRoute` → `MapMediator`
- Updated Roslyn analyzer to recognize `Map` method
- Updated MCP GenerateHandlerTool to generate `Map` code
- Fixed `new NuruAppBuilder()` default to use `BuilderMode.Empty` for backward compatibility

## Implementation Notes

Completed in commit `bfb858f` - 186 files changed across entire codebase.
