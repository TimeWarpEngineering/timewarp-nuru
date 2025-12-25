# Epic: V2 Source Generator Implementation

## Description

Implement the V2 source generator that intercepts `RunAsync()` and generates compile-time routing code. This enables full Native AOT support with zero reflection.

The generator supports three input DSLs:
- **Fluent DSL** - `.Map().WithHandler().AsQuery().Done()`
- **Mini-Language** - Rich pattern strings with `{params}` and `--options`
- **Attributed Routes** - `[NuruRoute]` class-per-endpoint pattern

All three produce the same IR (`AppModel` with `RouteDefinition[]`) and emit a single `RunAsync` interceptor.

## Design Documents

- `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md`
- `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md`

## Reference Implementation

- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs`

## Phases

- [ ] #266 Phase 0: Reorganization (7 commits)
- [ ] #267 Phase 1: Core Models (2 commits)
- [ ] #268 Phase 2: Locators (2 commits)
- [ ] #269 Phase 3: Extractors (2 commits)
- [ ] #270 Phase 4: Emitters (2 commits)
- [ ] #271 Phase 5: Generator Entry Point (1 commit)
- [ ] #272 Phase 6: Testing (incremental)

## Notes

Key architectural decisions:
- Intercept `RunAsync()` using C# 12 interceptors (not `Build()`)
- Flat namespace: `TimeWarp.Nuru.Generators`
- Folder structure: `generators/{locators,extractors,emitters,models}`
- Reuse existing `PatternParser` from `timewarp-nuru-parsing`
- Existing IR models (`RouteDefinition`, `HandlerDefinition`, etc.) are solid and reusable
