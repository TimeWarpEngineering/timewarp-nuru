# Epic: Compile-time endpoint generation - zero-cost Build()

## Description

Move all deterministic work from runtime `Build()` to compile-time source generation. The only runtime work should be:

1. **Match** - input args are only known at runtime
2. **Execute** - the actual handler work

Everything else can be pre-computed by source generators.

## Vision

```
args[] → Match (runtime) → Execute handler (runtime) → done
           ↑
    Pre-computed endpoint table (compile-time)
```

`Build()` becomes a near no-op - just returns a reference to pre-generated structures.

## What moves to compile-time

| Component                  | Today   | Compile-time approach                        |
| -------------------------- | ------- | -------------------------------------------- |
| Route pattern parsing      | Runtime | Source generator parses patterns             |
| CompiledRoute creation     | Runtime | Source generator emits pre-built instances   |
| Endpoint creation          | Runtime | Source generator emits Endpoint[]            |
| Sorting by specificity     | Runtime | Source generator pre-sorts                   |
| Help text                  | Runtime | Source generator emits strings               |
| Capabilities JSON          | Runtime | Source generator emits JSON                  |
| Completion scripts         | Runtime | Source generator emits scripts               |
| DI registrations           | Runtime | Source generator emits registration code     |
| Type converter registry    | Runtime | Source generator emits converter list        |
| Parameter binding metadata | Runtime | Source generator knows delegate signatures   |

## What remains at runtime (unavoidable)

- Configuration loading (appsettings, env vars, user secrets)
- Actual DI container building (though registrations are pre-generated)
- Service resolution during execution
- Route matching against user input
- Handler execution

## Checklist

- [ ] Design: Define source generator API and output format
- [ ] Design: Determine how compile-time and runtime paths coexist (migration strategy)
- [ ] Implement: Route pattern parsing in source generator
- [ ] Implement: CompiledRoute emission
- [ ] Implement: Pre-sorted Endpoint[] emission
- [ ] Implement: Help text generation
- [ ] Implement: Capabilities JSON generation
- [ ] Implement: Completion script generation
- [ ] Implement: DI registration generation
- [ ] Implement: Type converter registry generation
- [ ] Implement: Zero-cost Build() that wires up pre-generated structures
- [ ] Benchmark: Compare cold start times before/after
- [ ] Documentation: Update guides for source-generated approach

## Notes

### Already in place

- **Mediator**: Already uses martinothamar/Mediator source generator for AOT-compatible handler discovery
- **AOT**: `IsAotCompatible=true` already set
- **Attributed routes**: May already use source generation (verify)
- **Immutable after Build()**: Design assumption validated (task #238)

### Design questions to resolve

1. **Coexistence**: Support both runtime `Map()` and compile-time attributed routes?
2. **Incremental**: Can we do this incrementally (e.g., help first, then endpoints)?
3. **Debugging**: How to debug source-generated code?
4. **Versioning**: How to handle version info that may come from CI/build?

### Benefits

- **Zero cold start overhead** for route infrastructure
- **Full AOT compatibility** - no reflection
- **Smaller binary** - no runtime parsing code needed
- **Deterministic** - same input always produces same output

### Approach: Custom source generator (not comptime)

Evaluated [sebastienros/comptime](https://github.com/sebastienros/comptime) which provides `[Comptime]` attribute for compile-time execution via interceptors.

**Decision: Roll our own source generator**

Reasons:
- No external dependency
- Full control over emitted code structure
- Can emit optimized matching data structures, not just serialized data
- Already have source generator infrastructure (Mediator, attributed routes)
- Fits AOT-first philosophy
- `CompiledRoute` has complex nested types that would need custom serialization anyway

### Related

- Task #238: Make EndpointCollection internal (prerequisite - establishes immutability)
