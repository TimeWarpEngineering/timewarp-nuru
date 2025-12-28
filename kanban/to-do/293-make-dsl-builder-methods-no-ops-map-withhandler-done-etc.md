# Make DSL builder methods no-ops - Map WithHandler Done etc

## Parent

#289 V2 Generator Phase 7: Zero-Cost Runtime

## Description

The DSL builder methods (`Map()`, `WithHandler()`, `Done()`, etc.) currently do real work at runtime - parsing patterns, creating endpoints, adding to collections. This is all dead code because the source generator handles everything at compile time.

Make these methods no-ops that just maintain the fluent chain for compilation.

## Current State (Runtime Work)

```csharp
public virtual EndpointBuilder<TSelf> Map(string pattern)
{
  // All this is dead code:
  Endpoint endpoint = new()
  {
    RoutePattern = pattern,
    CompiledRoute = PatternParser.Parse(pattern, LoggerFactory)  // Runtime parsing!
  };
  EndpointCollection.Add(endpoint);  // Never read!
  return new EndpointBuilder<TSelf>((TSelf)this, endpoint);
}
```

## Target State (No-Op)

```csharp
public virtual EndpointBuilder<TSelf> Map(string pattern)
{
  // Source gen parses pattern at compile time
  // Just return a shell builder for fluent chaining
  return new EndpointBuilder<TSelf>((TSelf)this);
}
```

## Checklist

### NuruCoreAppBuilder methods
- [ ] `Map(string pattern)` → no-op, return shell EndpointBuilder
- [ ] `Map(Func<...> configureRoute)` → no-op
- [ ] `Map<TCommand>(pattern)` → no-op (Mediator)
- [ ] `Map<TCommand, TResponse>(pattern)` → no-op (Mediator)
- [ ] `WithGroupPrefix(string prefix)` → no-op, return shell GroupBuilder
- [ ] `AddTypeConverter()` → no-op (source gen handles types)
- [ ] `AddReplOptions()` → keep or no-op? (REPL needs runtime?)

### EndpointBuilder methods
- [ ] `WithHandler(Delegate)` → no-op
- [ ] `WithDescription(string)` → no-op
- [ ] `AsQuery()` → no-op
- [ ] `AsCommand()` → no-op
- [ ] `Done()` → return parent builder

### GroupBuilder methods
- [ ] `Map()` → no-op
- [ ] `WithGroupPrefix()` → no-op
- [ ] `Done()` → return parent builder

### Delete dead code
- [ ] `EndpointCollection` class - never read by generated code
- [ ] `Endpoint` class - only used by EndpointCollection
- [ ] Private helper methods (`MapPatternTyped`, `MapInternalTyped`, etc.)
- [ ] Logging calls in Map methods

## What Stays

**Parser code stays** - used by source generator at compile time:
- `PatternParser.Parse()` 
- `CompiledRoute`, `RouteMatcher`, `LiteralMatcher`, `ParameterMatcher`, `OptionMatcher`
- All of `timewarp-nuru-parsing` package

## Expected Impact

- **Faster startup** - No runtime pattern parsing
- **Smaller binary** - Dead code tree-shaken
- **Simpler code** - Less runtime infrastructure

## Notes

The builders become thin shells that exist only to:
1. Make user DSL code compile
2. Maintain fluent method chaining
3. Allow source generator to extract the DSL at compile time

All actual work happens in the source generator.

## Lessons Learned from #292 (Static Service Injection)

### ConfigureServices() Already Converted

`ConfigureServices(Action<IServiceCollection>)` and `ConfigureServices(Action<IServiceCollection, IConfiguration?>)` have already been converted to no-ops in `nuru-core-app-builder.configuration.cs`:

```csharp
public virtual TSelf ConfigureServices(Action<IServiceCollection> configure)
{
  // This method is interpreted by the source generator at compile time.
  // The generated code handles service registration via static instantiation.
  // This stub exists for API compatibility - it's a no-op at runtime.
  return (TSelf)this;
}
```

**Key insight:** The old implementation accessed `Services` property which threw if `AddDependencyInjection()` wasn't called. Making it a no-op removed this runtime check requirement.

### AddConfiguration() Already a No-Op

`AddConfiguration()` was already a no-op stub - the source generator's `ConfigurationEmitter` handles the actual work.

### Pattern for Converting Methods

Follow this pattern for converting DSL methods:

1. **Remove all runtime work** - no parsing, no collection building
2. **Return appropriate builder type** - maintain fluent chain
3. **Add explanatory comment** - document that source gen handles it
4. **No validation** - source gen validates at compile time

### Watch for Property Access

Some methods may access properties that have guards (like `Services` requiring `AddDependencyInjection()`). When making methods no-ops, ensure they don't trigger these guards.

### Runfile Cache

When testing changes, remember to clear the runfile cache:
```bash
ganda runfile cache --clear
```
Otherwise you may see old behavior.

### Related Bug Found

Task #295 was created: Source generator doesn't intercept chained `.Build().RunAsync()` - only works when `app` is assigned to a variable first. This is orthogonal to making DSL methods no-ops but worth noting.
