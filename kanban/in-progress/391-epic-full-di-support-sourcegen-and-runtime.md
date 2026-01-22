# Epic: Full DI Support - Source-Gen and Runtime Options

## Description

Provide comprehensive dependency injection support for Nuru applications with two modes:

1. **Source-Gen DI (Default)**: AOT-compatible, fast startup, compile-time resolution
2. **Runtime DI (Opt-in)**: Full MS DI container compatibility via `UseMicrosoftDependencyInjection()`

This epic addresses Bug #390 where the source generator cannot resolve services with constructor dependencies or services registered via extension methods.

## Background

The source generator's compile-time DI has fundamental limitations:
- Cannot follow extension methods into external assemblies
- Cannot resolve constructor dependencies (only emits parameterless `new T()`)
- No workaround exists for services with dependencies

Rather than abandoning compile-time DI (which provides AOT and performance benefits), we provide:
1. An opt-in escape hatch for full MS DI compatibility
2. Clear diagnostics when source-gen DI cannot handle a pattern
3. Progressive enhancement of source-gen capabilities

## Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Nuru DI Strategy                            │
├─────────────────────────────────┬───────────────────────────────────┤
│  DEFAULT: Source-Gen DI         │  OPT-IN: Runtime DI               │
│                                 │  .UseMicrosoftDependencyInjection()│
├─────────────────────────────────┼───────────────────────────────────┤
│  ✅ AOT compatible              │  ❌ AOT limitations               │
│  ✅ Fast startup                │  ⚠️ +2-10ms startup              │
│  ✅ Small binary                │  ⚠️ Larger binary                │
│  ⚠️ Limited patterns           │  ✅ Everything works              │
├─────────────────────────────────┼───────────────────────────────────┤
│  Phase 3+4 expand capabilities  │  Escape hatch for complex cases   │
└─────────────────────────────────┴───────────────────────────────────┘
```

## Implementation Phases

### Phase 1: UseMicrosoftDependencyInjection (#392)
Add opt-in runtime DI support. Users get unblocked immediately.

### Phase 2: DI Diagnostics (#393)
Clear compile-time errors for unsupported source-gen DI patterns.

### Phase 3: Constructor Dependency Resolution (#394)
Enhance source-gen DI to resolve constructor dependencies via topological sorting.

### Phase 4: Execute and Inspect (#395)
Execute ServiceCollection at compile time to see extension method registrations.

## Success Criteria

- [x] Users can opt into full MS DI with single method call
- [ ] Source-gen DI provides clear errors for unsupported patterns
- [ ] Constructor dependencies work in source-gen DI (Phase 3)
- [ ] Extension methods work in source-gen DI (Phase 4)
- [x] AOT compatibility maintained for default path
- [x] No breaking changes to existing API

## Related

- **Bug #390**: Source generator cannot resolve services with constructor dependencies
- **Analysis**: `.agent/workspace/2026-01-21T12-00-00_bug-390-comprehensive-analysis.md`

## Checklist

- [x] Phase 1: UseMicrosoftDependencyInjection (#392) - DONE
- [ ] Phase 2: DI Diagnostics (#393)
- [ ] Phase 3: Constructor Dependency Resolution (#394)
- [ ] Phase 4: Execute and Inspect (#395)
- [ ] Phase 5: Per-App ServiceProvider Isolation (future)
- [ ] Documentation updates
- [ ] Migration guide for users hitting limitations

## Lessons Learned (Phase 1)

### Mixed DI Strategies

When apps in the same assembly use different DI strategies, the generator must emit infrastructure for BOTH strategies. The `interceptor-emitter.cs` now filters services by each app's DI strategy.

### Shared ServiceProvider Limitation

The runtime DI infrastructure uses a single static `__serviceProvider` shared across all apps that call `UseMicrosoftDependencyInjection()`. This creates potential issues:

1. **Service Conflicts**: If two apps register the same interface with different lifetimes, only one wins
2. **Test Isolation**: Tests must use unique service types to avoid interference
3. **No Per-App Scopes**: Scoped services behave like singletons across the shared provider

**Recommendation for Phase 5**: Consider per-app ServiceProvider isolation using app index in field names (e.g., `__serviceProvider_0`, `__serviceProvider_1`).

### Testing Pattern

When testing runtime DI features:
- Use unique type names per test (e.g., `IRdi15TransientCounter`, `IRdi15SingletonCounter`)
- Clear runfile cache (`dotnet clean <runfile>`) when changing service registrations
- Static counters need `Reset()` methods but won't help if ServiceProvider is cached
