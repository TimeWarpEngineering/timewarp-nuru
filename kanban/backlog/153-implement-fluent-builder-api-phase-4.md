# Implement Fluent Builder API (Phase 4)

## Description

Expose fluent builder to consumers. Add `MapGroup()` API for delegate-based grouped routes with shared prefix and options.

**Goal:** Advanced consumer API for complex CLIs with full IntelliSense support.

## Status: BLOCKED

**Blocked by:** Task 152 (Phase 3) which is blocked by Task 201 (Performance Regression Investigation)

**Reason:** December 2025 benchmarks revealed a 4x performance regression in the Full builder (34ms → 132ms). Until this is investigated and fixed, proceeding with later phases would compound the performance problem.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete ✅
- Task 150: Implement Endpoints (Phase 1) - must be complete ✅
- Task 151: Implement Delegate Generation (Phase 2) - must be complete ✅
- Task 152: Implement Unified Pipeline (Phase 3) - **BLOCKED** (performance regression)
- Task 201: Investigate Full Builder 4x Performance Regression - **MUST COMPLETE FIRST**

## Checklist

### Make Builder Public
- [ ] Change `CompiledRouteBuilder` from `internal` to `public`
- [ ] Review and finalize public API surface
- [ ] Add XML documentation to all public members
- [ ] Ensure API is intuitive and discoverable

### New Map Overloads
- [ ] Add `Map(Action<CompiledRouteBuilder>, Delegate)` - fluent + delegate
- [ ] Add `Map<TCommand>(Action<CompiledRouteBuilder>)` - fluent + command
- [ ] Add `Map(CompiledRoute, Delegate)` - pre-built route + delegate
- [ ] Add `Map<TCommand>(CompiledRoute)` - pre-built route + command

### MapGroup API
- [ ] Create `IRouteGroupBuilder` interface
- [ ] Implement `MapGroup(string prefix)` method
- [ ] Implement `WithDescription(string)` on group
- [ ] Implement `WithGroupOptions(string pattern)` on group
- [ ] Implement `WithGroupOptions(Action<CompiledRouteBuilder>)` on group
- [ ] Support nested groups (prefix accumulation)
- [ ] Support options accumulation in nested groups
- [ ] Update help generation to display group structure and group options
- [ ] Update shell completion to include group options

### Source Generator Updates
- [ ] Walk fluent builder expression tree
- [ ] Extract builder method calls at compile time
- [ ] Handle `MapGroup` variable tracking (same statement block)
- [ ] Emit combined routes for grouped routes

### Fluent Chain Constraint
- [ ] Implement constraint: group routes in fluent chain or immediate Map calls
- [ ] Emit diagnostic `NURU003` for unresolvable group context
- [ ] Document constraint clearly

### Testing
- [ ] Test `Map(Action<CompiledRouteBuilder>, delegate)`
- [ ] Test `Map<TCommand>(Action<CompiledRouteBuilder>)`
- [ ] Test `MapGroup` with single route
- [ ] Test `MapGroup` with multiple routes
- [ ] Test `MapGroup` with `WithGroupOptions`
- [ ] Test nested `MapGroup`
- [ ] Test diagnostic for unresolvable group context
- [ ] Verify IntelliSense works correctly

## Notes

### Reference

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 118-141, 449-513, 1019-1108)

### Consumer API

```csharp
public interface IEndpointCollectionBuilder
{
    // Fluent builder + delegate
    void Map(Action<CompiledRouteBuilder> configure, Delegate handler, string? description = null);
    
    // Fluent builder + command (TCommand : IRequest<TResponse>)
    void Map<TCommand>(Action<CompiledRouteBuilder> configure, string? description = null) 
        where TCommand : IBaseRequest;
    
    // Pre-built route + delegate
    void Map(CompiledRoute compiledRoute, Delegate handler, string? description = null);
    
    // Pre-built route + command (TCommand : IRequest<TResponse>)
    void Map<TCommand>(CompiledRoute compiledRoute, string? description = null) 
        where TCommand : IBaseRequest;
    
    // Grouped routes
    IRouteGroupBuilder MapGroup(string prefix);
}

public interface IRouteGroupBuilder : IEndpointCollectionBuilder
{
    IRouteGroupBuilder WithDescription(string description);
    IRouteGroupBuilder WithGroupOptions(string optionsPattern);
    IRouteGroupBuilder WithGroupOptions(Action<CompiledRouteBuilder> configure);
}
```

**Note:** `IBaseRequest` is the non-generic base interface for `IRequest<TResponse>` from Mediator.

### Example Usage

```csharp
// Fluent builder + delegate
app.Map(r => r
    .WithLiteral("deploy")
    .WithParameter("env")
    .WithOption("force"),
    (string env, bool force) => { ... });

// MapGroup with shared options
var docker = builder.MapGroup("docker")
    .WithDescription("Container management commands")
    .WithGroupOptions("--debug,-D --log-level {level?}");

docker.Map("run {image}", (string image, bool debug, string? logLevel) => { ... });
docker.Map("build {path}", (string path, bool debug, string? logLevel) => { ... });
// Effective: "docker run {image} --debug,-D? --log-level {level?}"
// Effective: "docker build {path} --debug,-D? --log-level {level?}"

// Nested groups
var compose = docker.MapGroup("compose")
    .WithGroupOptions("--file,-f {path?}");

compose.Map("up", (bool debug, string? file) => { ... });
// Effective: "docker compose up --debug,-D? --file,-f {path?}"
```

### Fluent Chain Constraint (Phase 4)

```csharp
// SUPPORTED - fluent chain
builder.MapGroup("docker")
    .WithGroupOptions("--debug")
    .Map("run {image}", handler);

// SUPPORTED - variable but immediate Map calls
var docker = builder.MapGroup("docker").WithGroupOptions("--debug");
docker.Map("run {image}", handler);  // Same statement block, trackable

// WARNING - cannot resolve (relaxed in Phase 5)
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // NURU003: Cannot resolve group context
```

### Why Support Fluent Builder?

| Use Case | String Pattern | Fluent Builder |
|----------|---------------|----------------|
| Quick prototyping | Faster to type | |
| Complex patterns | Harder to read | Self-documenting |
| IDE support | No autocomplete | Full IntelliSense |
| Refactoring | Find/replace | Rename symbol |
| Validation timing | Runtime parse errors | Compile-time |

### Releasable

Yes - advanced consumer API for complex CLIs.
