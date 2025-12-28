# V2 Generator Phase 7: Zero-Cost Runtime

## Parent

#265 Epic: V2 Source Generator Implementation

## Description

Eliminate runtime builder overhead when source generator is active. Currently, the DSL methods (`Map()`, `WithHandler()`, `Build()`, etc.) still execute at runtime even though the source generator extracts all information at compile time.

**Current state:** 8ms startup (bench-nuru-full)
**Target:** <3ms startup (competitive with ConsoleAppFramework/System.CommandLine)

## Problem

The V2 generator intercepts `RunAsync()` and generates optimal routing code, but:

1. `NuruApp.CreateBuilder(args)` still sets up DI container, loads configuration
2. `UseAllExtensions()` still wires telemetry, completion, etc.
3. DSL methods (`Map()`, `WithHandler()`, etc.) still build runtime endpoint collections
4. `Build()` still finalizes the app

All of this is **dead code** because the generated interceptor bypasses it entirely.

## Solution

Make DSL methods no-ops (or near no-ops) when source generation is active:

### Option A: Conditional Compilation
```csharp
public TSelf Map(string pattern)
{
#if !NURU_SOURCEGEN
    // Runtime path (fallback for REPL, dynamic scenarios)
    _endpoints.Add(CreateEndpoint(pattern));
#endif
    return Self;
}
```

### Option B: Runtime Detection
```csharp
public TSelf Map(string pattern)
{
    if (!GeneratedInterceptor.IsActive)
    {
        _endpoints.Add(CreateEndpoint(pattern));
    }
    return Self;
}
```

### Option C: Separate Builder Types
- `NuruApp.CreateBuilder()` → Full runtime builder (for REPL/dynamic)
- Source gen emits direct construction, no builder at all

## Checklist

### Phase 7.1: Analyze Current Overhead
- [ ] Profile bench-nuru-full startup to identify exact costs
- [ ] Identify which builder methods contribute most to overhead
- [ ] Document the call chain from CreateBuilder to RunAsync

### Phase 7.2: Make Builder Methods No-Ops
- [ ] `Map()` → no-op (source gen has the pattern)
- [ ] `WithHandler()` → no-op (source gen has the delegate)
- [ ] `AsQuery()`/`AsCommand()` → no-op
- [ ] `Done()` → no-op
- [ ] `WithGroupPrefix()` → no-op
- [ ] `AddHelp()` → no-op (source gen emits help)
- [ ] `Build()` → minimal (just create app instance)

### Phase 7.3: Eliminate DI/Config Overhead
- [ ] Skip DI container setup when source gen active
- [ ] Skip configuration loading when source gen active
- [ ] Skip extension wiring when source gen active

### Phase 7.4: Simplify App Construction
- [ ] `NuruCoreApp` constructor takes minimal args
- [ ] No endpoint collection needed (generated code handles routing)
- [ ] No help provider needed (generated code handles help)

### Phase 7.5: Verify and Benchmark
- [ ] Run bench-nuru-full, target <3ms
- [ ] Verify all existing tests pass
- [ ] Verify REPL mode still works (needs runtime builder)
- [ ] Document final performance numbers

## Files to Modify

| File | Change |
|------|--------|
| `NuruCoreAppBuilder<TSelf>` | Make DSL methods conditional no-ops |
| `NuruAppBuilder` | Skip extension wiring when source gen active |
| `NuruCoreApp` | Simplify constructor |
| `BuilderMode` | Possibly add `SourceGenerated` mode |

## Success Criteria

1. bench-nuru-full startup < 3ms
2. Binary size reduced (tree-shaking removes dead code)
3. All existing tests pass
4. REPL mode continues to work

## References

- Archived #248 (Zero-cost Build): `kanban/archived/248-zero-cost-build-implementation.md`
- Archived #239 (Epic): `kanban/archived/239-epic-compile-time-endpoint-generation-zero-cost-build.md`
- Benchmark results: `benchmarks/aot-benchmarks/results/2025-12-28-aot-benchmark.md`

## Notes

### Why This Was Missed

Epic #239 was superseded by #265 when we switched from intercepting `Build()` to intercepting `RunAsync()`. The tasks for eliminating runtime overhead (#248, #249) were archived but never recreated for the new approach.

### REPL Compatibility

REPL mode dynamically adds commands at runtime, so it needs the full runtime builder. The solution must preserve this capability while making the common case (source-gen CLI) zero-cost.

### Binary Size

Current: 11.3 MB (bench-nuru-full)
Target: <5 MB

The large binary size suggests dead code is being linked in. Once DSL methods are no-ops, tree-shaking should eliminate:
- `EndpointCollection` and related types
- `EndpointResolver` runtime matching
- `HelpProvider` runtime generation
- Unused DI/Config infrastructure
