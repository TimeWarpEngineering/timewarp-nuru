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

- [x] #266 Phase 0: Reorganization ✅ DONE
- [x] #267 Phase 1: Core Models ✅ DONE
- [x] #268 Phase 2: Locators ✅ DONE
- [x] #269 Phase 3: Extractors ✅ DONE
- [x] #270 Phase 4: Emitters ✅ DONE
- [x] #271 Phase 5: Generator Entry Point ✅ DONE
- [ ] #272 Phase 6: Testing (in-progress)
- [ ] #289 Phase 7: Zero-Cost Runtime (eliminate builder overhead)

## Supersedes

This epic supersedes #239 (Compile-time endpoint generation). Key tasks from #239 that are incorporated here:
- #248 Zero-cost Build() → Now #289 Phase 7: Zero-Cost Runtime
- #249 Delete runtime code → Part of #289

The original approach intercepted `Build()`. The V2 approach intercepts `RunAsync()` instead, which is cleaner but still requires eliminating the runtime builder overhead (Phase 7).

## Current State (After Phase 5)

The V2 source generator is functional:
- Source generator extracts DSL at compile time
- Emits interceptor code for `RunAsync()`
- Generated code handles routing directly

**Remaining issue:** The DSL methods still execute at runtime (setting up DI, config, endpoints) even though the generated interceptor bypasses all of it. This causes ~8ms startup overhead that should be <3ms.

### Folder Structure
```
source/timewarp-nuru-analyzers/
├── analyzers/                    # Existing analyzers (unchanged)
│   ├── diagnostics/              # 6 diagnostic files
│   └── *.cs                      # 3 analyzer files
├── generators/                   # V2 generator code
│   ├── locators/                 # 25 files
│   ├── extractors/               # Extractors + IR builders
│   ├── interpreter/              # DSL interpreter
│   ├── emitters/                 # Code emitters
│   └── models/                   # IR models
└── reference-only/               # Old code for reference
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

### Phase 3: Extractors
- Created extractors for handler, pattern, intercept site
- Integrated with DSL interpreter

### Phase 4: Emitters
- Created `InterceptorEmitter` for generated code output
- Emits route matching, parameter binding, handler invocation

### Phase 5: Generator Entry Point
- Created `NuruGenerator` incremental source generator
- Wired up pipeline: Locate → Extract → Emit

## Benchmark Results (2025-12-28)

| Framework | Mean [ms] | Binary Size |
|-----------|-----------|-------------|
| ConsoleAppFramework | 4.0 ± 2.0 | 2.5 MB |
| System.CommandLine | 3.4 ± 0.4 | 3.3 MB |
| **Nuru-Full** | **8.0 ± 2.1** | **11.3 MB** |

Target after Phase 7: <3ms startup, <5 MB binary
