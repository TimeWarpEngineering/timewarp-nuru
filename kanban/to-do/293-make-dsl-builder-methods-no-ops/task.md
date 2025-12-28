# Epic #293: Make DSL Builder Methods No-Ops

## Parent

#289 V2 Generator Phase 7: Zero-Cost Runtime

## Description

The DSL builder methods (`Map()`, `WithHandler()`, `Done()`, etc.) currently do real work at runtime - parsing patterns, creating endpoints, adding to collections. This is all dead code because the source generator handles everything at compile time.

This epic converts these methods to no-ops that just maintain the fluent chain for compilation.

## Sub-Tasks

| Task | Description | Dependencies |
|------|-------------|--------------|
| #293.1 | Convert EndpointBuilder methods to no-ops | None |
| #293.2 | Convert GroupEndpointBuilder methods to no-ops | None |
| #293.3 | Convert NuruCoreAppBuilder.Map() methods to no-ops | #293.1 |
| #293.4 | Convert GroupBuilder methods to no-ops | #293.2 |
| #293.5 | Convert AddTypeConverter() to no-op | None |
| #293.6 | Delete dead code | #293.1-5 |
| #293.7 | Move REPL code to reference-only | None |

## Execution Order

```
#293.1 ──┬──> #293.3 ──┐
         │             │
#293.2 ──┤             ├──> #293.6
         │             │
#293.4 <─┴─────────────┤
                       │
#293.5 ────────────────┘

#293.7 (independent)
```

## Checklist

- [ ] #293.1 - EndpointBuilder no-ops
- [ ] #293.2 - GroupEndpointBuilder no-ops
- [ ] #293.3 - NuruCoreAppBuilder.Map() no-ops
- [ ] #293.4 - GroupBuilder no-ops
- [ ] #293.5 - AddTypeConverter() no-op
- [ ] #293.6 - Delete dead code
- [ ] #293.7 - Move REPL to reference-only
- [ ] Generator test file: `generator-05-builder-no-ops.cs`

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

### Pattern for Converting Methods

Follow this pattern (established in #292):

1. **Remove all runtime work** - no parsing, no collection building
2. **Return appropriate builder type** - maintain fluent chain
3. **Add explanatory comment** - document that source gen handles it
4. **No validation** - source gen validates at compile time

### Already Converted (from #292)

- `ConfigureServices(Action<IServiceCollection>)` - no-op
- `ConfigureServices(Action<IServiceCollection, IConfiguration?>)` - no-op
- `AddConfiguration()` - was already no-op
- `EndpointCollection.Add()` - already no-op

### Runfile Cache

When testing changes, remember to clear the runfile cache:
```bash
ganda runfile cache --clear
```
