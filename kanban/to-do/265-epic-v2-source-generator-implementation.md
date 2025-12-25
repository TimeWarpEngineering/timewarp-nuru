# Epic: V2 Source Generator Implementation

## Description

Implement the V2 source generator that intercepts `RunAsync()` and generates compile-time routing code. This enables full Native AOT support with zero reflection.

The generator supports three input DSLs:
- **Fluent DSL** - `.Map().WithHandler().AsQuery().Done()`
- **Mini-Language** - Rich pattern strings with `{params}` and `--options`
- **Attributed Routes** - `[NuruRoute]` class-per-endpoint pattern

All three produce the same IR (`AppModel` with `RouteDefinition[]`) and emit a single `RunAsync` interceptor.

## Design Documents

**CRITICAL - Read these before any phase:**
- `.agent/workspace/2024-12-25T14-00-00_v2-source-generator-architecture.md` - Full architecture
- `.agent/workspace/2024-12-25T12-00-00_v2-fluent-dsl-design.md` - DSL specification

## Reference Implementation

- `tests/timewarp-nuru-core-tests/routing/dsl-example.cs` - Working DSL example

## Phases

- [x] #266 Phase 0: Reorganization (7 commits) ✅ DONE
- [x] #267 Phase 1: Core Models (2 commits) ✅ DONE
- [x] #268 Phase 2: Locators (2 commits) ✅ DONE
- [ ] #269 Phase 3: Extractors (2 commits) ← NEXT
- [ ] #270 Phase 4: Emitters (2 commits)
- [ ] #271 Phase 5: Generator Entry Point (1 commit)
- [ ] #272 Phase 6: Testing (incremental)

## Current State (After Phase 2)

### Folder Structure
```
source/timewarp-nuru-analyzers/
├── analyzers/                    # Existing analyzers (unchanged)
│   ├── diagnostics/              # 6 diagnostic files
│   └── *.cs                      # 3 analyzer files
├── generators/                   # NEW - V2 generator code
│   ├── locators/                 # 25 files (Phase 2)
│   │   ├── run-async-locator.cs
│   │   ├── map-locator.cs
│   │   ├── with-handler-locator.cs
│   │   └── ... (22 more)
│   ├── extractors/               # Phase 3 target
│   │   └── builders/             # 3 files (Phase 0 + Phase 1)
│   │       ├── app-model-builder.cs
│   │       ├── route-definition-builder.cs
│   │       └── handler-definition-builder.cs
│   ├── emitters/                 # Phase 4 target (empty)
│   └── models/                   # 12 files (Phase 0 + Phase 1)
│       ├── app-model.cs
│       ├── route-definition.cs
│       ├── handler-definition.cs
│       ├── segment-definition.cs
│       └── ... (8 more)
└── reference-only/               # Old code for reference
    ├── extractors/               # 5 old extractor files
    ├── emitters/                 # 1 old emitter file
    └── generators/               # Old generator files
```

### Key Technical Notes

1. **Namespace Conflict:** `TimeWarp.Nuru.SyntaxNode` shadows Roslyn's `SyntaxNode`
   - Solution: Use `using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;`

2. **Coding Standards:** Follow `documentation/developer/standards/csharp-coding.md`
   - PascalCase for private fields (no underscore)
   - 2-space indentation
   - Allman bracket style

3. **Global Usings:** Located in `source/timewarp-nuru-analyzers/global-usings.cs`
   - Includes `TimeWarp.Nuru.Generators` namespace

## Notes

Key architectural decisions:
- Intercept `RunAsync()` using C# 12 interceptors (not `Build()`)
- Flat namespace: `TimeWarp.Nuru.Generators`
- Folder structure: `generators/{locators,extractors,emitters,models}`
- Reuse existing `PatternParser` from `timewarp-nuru-parsing`
- Existing IR models (`RouteDefinition`, `HandlerDefinition`, etc.) are solid and reusable

## Completed Work Summary

### Phase 0: Reorganization
- Created `generators/` folder structure
- Moved 6 model files to `generators/models/` with namespace update
- Moved 2 builder files to `generators/extractors/builders/` with namespace update
- Moved old code to `reference-only/` for reference

### Phase 1: Core Models
- Created 6 new model files: `AppModel`, `InterceptSiteModel`, `HelpModel`, `ReplModel`, `BehaviorDefinition`, `ServiceDefinition`
- Created `AppModelBuilder` fluent builder

### Phase 2: Locators
- Created 21 fluent DSL locators for all builder methods
- Created 4 attributed route locators for class/property attributes
- All locators have `IsPotentialMatch()` (syntactic) and `Extract()` (semantic) methods
