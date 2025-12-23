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
| #242 | Jaribu parity test suite |
| #243 | Emit pre-sorted Endpoint[] |
| #244 | Emit inlined pipeline |
| #245 | Emit static service fields (replace DI) |
| #246 | Emit help text and capabilities |
| #247 | Emit completion scripts |
| #248 | Zero-cost Build() implementation |
| #249 | Delete runtime infrastructure |
| #250 | Benchmark and documentation |

## Checklist

- [ ] #240 Design-time model
- [ ] #241 Dual-build infrastructure
- [ ] #242 Parity tests
- [ ] #243 Pre-sorted endpoints
- [ ] #244 Inlined pipeline
- [ ] #245 Static services
- [ ] #246 Help/capabilities
- [ ] #247 Completion scripts
- [ ] #248 Zero-cost Build()
- [ ] #249 Delete runtime code
- [ ] #250 Benchmark/docs

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

### Analysis Document

Full analysis at: `.agent/workspace/2024-12-22T14-30-00_compile-time-data-structure-analysis.md`

### Related

- Task #238: Make EndpointCollection internal (prerequisite - establishes immutability)
