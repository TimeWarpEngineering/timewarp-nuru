# Task 190: Fix Duplicate Parameters in Route Group Generation

## Parent Epic
- Epic 150: Implement Attributed Routes (Phase 1)

## Problem
When using `[NuruRouteGroup]` with routes that have parameters, the parameters are duplicated in the generated route pattern.

### Example
```csharp
[NuruRouteGroup("docker")]
public abstract class DockerGroupBase;

[NuruRoute("run {image:string}")]
public class DockerRunCommand : DockerGroupBase, ICommand<Unit> { ... }
```

**Expected route:** `docker run {image:string}`
**Actual route:** `docker run <image> <image>` (duplicated)

### Symptoms
- Help output shows duplicated parameters: `docker tag <source> <target> <source> <target>`
- Route matching fails because it expects more parameters than provided
- Commands like `docker run nginx:latest` show "No matching command found"

## Root Cause
The attributed route generator in `timewarp-nuru-analyzers` is combining the group prefix with the route pattern incorrectly, causing parameters to be added twice.

## Files to Investigate
- `source/timewarp-nuru-analyzers/generators/attributed-route-generator.cs`

## Acceptance Criteria
- [x] Routes with `[NuruRouteGroup]` correctly combine prefix and pattern
- [x] Parameters appear only once in generated routes
- [x] `docker run nginx:latest` executes successfully
- [x] `docker tag source target` executes successfully
- [x] All existing attributed route tests pass

## Testing
- Run attributed-routes sample with docker commands
- Verify help output shows correct parameter count

## Results

### Changes Made

1. **Changed design: Parameters come from `[Parameter]` attributes, not pattern string**
   - Route pattern (e.g., `"tag"`) contains only the literal command name
   - Parameters are defined via `[Parameter]` attributes on properties
   - Generator extracts parameter info from attributes

2. **Added `NURU_A001` analyzer** - Enforces single-literal patterns
   - Multi-word patterns like `"config set"` require `[NuruRouteGroup]`

3. **Added `NURU_A002` analyzer** - Enforces explicit parameter ordering
   - When multiple `[Parameter]` attributes exist, each must specify `Order`
   - Prevents non-deterministic ordering from source file layout

4. **Added `Order` property to `[Parameter]` attribute**
   - `[Parameter(Order = 0, Description = "...")]`
   - Parameters sorted by `Order` before route generation

5. **Added `?` to boolean flag options in pattern strings**
   - Pattern displays as `--all,-a?` to indicate optional

6. **Made `EndpointCollection.Sort()` stable**
   - Changed from unstable `List.Sort()` to LINQ-based stable sort
   - Preserves insertion order as tie-breaker for equal specificity
   - User routes registered before help routes now correctly win

7. **Created config group structure**
   - Added `ConfigGroupBase` with `[NuruRouteGroup("config")]`
   - Moved `get-config-query.cs` and `set-config-command.cs` to use groups

### Files Modified

**Generator:**
- `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-generator.cs`
- `source/timewarp-nuru-analyzers/analyzers/nuru-attributed-route-pattern-analyzer.cs`
- `source/timewarp-nuru-analyzers/diagnostics/diagnostic-descriptors.attributed.cs`
- `source/timewarp-nuru-analyzers/AnalyzerReleases.Unshipped.md`

**Core:**
- `source/timewarp-nuru-core/attributes/parameter-attribute.cs`
- `source/timewarp-nuru-core/endpoints/endpoint-collection.cs`
- `source/timewarp-nuru-core/nuru-core-app-builder.cs`

**Samples:**
- `samples/attributed-routes/attributed-routes.cs`
- `samples/attributed-routes/messages/config/config-group-base.cs` (new)
- `samples/attributed-routes/messages/queries/greet-query.cs`
- `samples/attributed-routes/messages/queries/get-config-query.cs`
- `samples/attributed-routes/messages/idempotent/set-config-command.cs`
- `samples/attributed-routes/messages/docker/commands/docker-build-command.cs`
- `samples/attributed-routes/messages/docker/commands/docker-run-command.cs`
- `samples/attributed-routes/messages/docker/idempotent/docker-tag-command.cs`

### Test Results

All commands work correctly:
- ✅ `docker ps` - executes (stable sort fixed help route priority)
- ✅ `docker ps --all` - executes with flag
- ✅ `docker run nginx:latest` - executes
- ✅ `docker tag myapp:latest myapp:v1.0` - executes with correct parameter order
- ✅ `config get mykey` - executes
- ✅ `config set mykey myvalue` - executes with correct parameter order
- ✅ `greet Steven` - executes
