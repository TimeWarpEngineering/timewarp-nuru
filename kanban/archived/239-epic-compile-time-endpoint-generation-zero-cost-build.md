# Epic: Compile-time endpoint generation - zero-cost Build()

## Description

Move all deterministic work from runtime `Build()` to compile-time source generation. The only runtime work should be:

1. **Match** - input args are only known at runtime
2. **Execute** - the actual handler work
3. **Configuration** - appsettings, env vars, user secrets

Everything else is known at compile time and can be generated.

## Vision

```
args[] → Match (runtime) → Execute handler (runtime) → done
           ↑
    Pre-computed endpoint table (compile-time)
    Pre-sorted, pre-bound, inlined pipeline
```

`Build()` becomes a near no-op - just returns a reference to pre-generated structures.

## Key Insights

1. **CLI apps don't need plugins** - Want to extend? Recompile.
2. **No DI container needed** - Services become static fields with lazy init
3. **No Mediator needed** - Direct handler invocation, inlined pipeline
4. **Pipeline is deterministic** - Behaviors registered in source code, generator sees them
5. **Two-layer data model** - Rich design-time model for generator, minimal runtime model

## What moves to compile-time

| Component                  | Today   | Compile-time approach                      |
| -------------------------- | ------- | ------------------------------------------ |
| Route pattern parsing      | Runtime | Source generator parses patterns           |
| CompiledRoute creation     | Runtime | Source generator emits pre-built instances |
| Endpoint creation          | Runtime | Source generator emits Endpoint[]          |
| Sorting by specificity     | Runtime | Source generator pre-sorts                 |
| Help text                  | Runtime | Source generator emits strings             |
| Capabilities JSON          | Runtime | Source generator emits JSON                |
| Completion scripts         | Runtime | Source generator emits scripts             |
| Pipeline/middleware        | Runtime | Source generator inlines behavior chain    |
| Handler invocation         | Runtime | Source generator emits direct calls        |
| Parameter binding metadata | Runtime | Source generator knows delegate signatures |

## What remains at runtime (unavoidable)

- Configuration loading (appsettings, env vars, user secrets)
- Service instantiation (logger, config reader)
- Route matching against user input
- Handler execution

## Implementation Strategy

### Temporary Dual Build (Migration Scaffold)

Same source, two outputs during development:
- **AppA** - Current runtime path (reference implementation)
- **AppB** - Generated path (new implementation)

Parity tests verify both produce identical output. Once proven, delete AppA infrastructure.

```
Phase 1: Build both, run parity tests
Phase 2: AppB passes all tests
Phase 3: AppB becomes only output
Phase 4: Delete runtime infrastructure
```

## Sub-Tasks

| Task | Description |
|------|-------------|
| #240 | Design-time model for source generator |
| #241 | Dual-build infrastructure (AppA/AppB) |
| #242 | Jaribu parity test suite (overview) |
| #242-step-1 | Parse route pattern to design-time model |
| #242-step-2 | Manual runtime construction from design-time model |
| #242-step-3 | Source generator for design-time model |
| #242-step-4 | Source generator emits runtime structures |
| #243 | Emit pre-sorted Endpoint[] |
| #244 | Emit inlined pipeline |
| #245 | Emit static service fields (replace DI) |
| #246 | Emit help text and capabilities |
| #247 | Emit completion scripts |
| #248 | Zero-cost Build() implementation |
| #249 | Delete runtime infrastructure |
| #250 | Benchmark and documentation |
| #260 | Add `<UseNewGen>` toggle infrastructure |
| #261 | Baseline test run and V2 gap analysis |
| #262 | V2 generator - core endpoint generation |
| #263 | Dual-build parity and benchmark testing |

## Checklist

- [x] #240 Design-time model
- [x] #241 Dual-build infrastructure
- [ ] #242 Parity tests
  - [x] #242-step-1 Manually build design-time model (RouteDefinition)
  - [x] #242-step-2 Manually build runtime, `add 2 2` works
  - [ ] #242-step-3 Source gen helpers for design-time model
  - [ ] #242-step-4 Source gen emits runtime
- [ ] #243 Pre-sorted endpoints
- [ ] #244 Inlined pipeline
- [ ] #245 Static services
- [ ] #246 Help/capabilities
- [ ] #247 Completion scripts
- [ ] #248 Zero-cost Build()
- [ ] #249 Delete runtime code
- [ ] #250 Benchmark/docs
- [x] #260 UseNewGen toggle infrastructure
- [x] #261 V2 gap analysis
- [x] #262 V2 core endpoint generation
- [ ] #263 Dual-build parity/benchmarks

## Notes

### What Gets Deleted After Migration

| Delete | Reason |
|--------|--------|
| `NuruCoreAppBuilder` runtime logic | Replaced by generated code |
| `EndpointCollection` | Pre-sorted array in generated code |
| `NuruRouteRegistry` | No runtime registration needed |
| `InvokerRegistry` runtime lookup | Handlers inlined |
| `EndpointResolver` | Matching logic generated |
| MS.Extensions.DependencyInjection | Static fields instead |
| Mediator dependency | Direct invocation |
| `IPipelineBehavior` infrastructure | Inlined pipeline |

### Analysis Documents

- Compile-time data structure analysis: `.agent/workspace/2024-12-22T14-30-00_compile-time-data-structure-analysis.md`
- V2 runtime types decision: `.agent/workspace/2024-12-24T18-30-00_v2-generator-runtime-types-analysis.md`

### Key Decision: V2 Uses Existing Runtime Types

The V2 generator emits code that instantiates **existing** runtime types (`LiteralMatcher`, `ParameterMatcher`, `CompiledRoute`) from `TimeWarp.Nuru` namespace. It does NOT generate new type definitions. This avoids type conflicts and ensures compatibility with existing runtime infrastructure until #248 wires everything together.

### Related

- Task #238: Make EndpointCollection internal (prerequisite - establishes immutability)

### Archive Reason (2024-12-25)

**Status: Superseded by redesign**

This epic and its remaining child tasks are being archived because we have decided to redesign the compile-time generation approach. The completed sub-tasks (#240, #241, #242*, #260, #261, #262) remain in done/ as they represent valid completed work. The remaining incomplete tasks will be recreated according to the new design.

The learnings from this epic will inform the new approach.
