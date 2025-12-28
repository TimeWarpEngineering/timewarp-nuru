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
