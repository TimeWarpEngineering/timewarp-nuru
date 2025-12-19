# Implement Relaxed Constraints (Phase 5)

## Description

More flexible `MapGroup()` usage via data flow analysis within method scope. Relaxes the fluent chain constraint from Phase 4.

**Goal:** Quality of life improvement for `MapGroup()` users - allows code between group creation and Map calls.

## Status: BLOCKED

**Blocked by:** Task 153 (Phase 4) → Task 152 (Phase 3) → Task 201 (Performance Investigation)

**Reason:** December 2025 benchmarks revealed a 4x performance regression in the Full builder. The entire pipeline (Phases 3-5) is blocked until the performance regression is investigated and fixed.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 153: Implement Fluent Builder API (Phase 4) - **BLOCKED**
- Task 152: Implement Unified Pipeline (Phase 3) - **BLOCKED** (performance regression)
- Task 201: Investigate Full Builder 4x Performance Regression - **MUST COMPLETE FIRST**

## Checklist

### Data Flow Analysis
- [ ] Implement data flow analysis within method scope
- [ ] Track `MapGroup()` return value assignments
- [ ] Follow variable through method body
- [ ] Resolve group context for later `Map()` calls on same variable
- [ ] Handle reassignment cases

### Supported Patterns
- [ ] Variable declaration then later Map calls (same method)
- [ ] Variable used in conditional blocks (if group is unambiguous)
- [ ] Variable used in loops (if group is unambiguous)

### Still Unsupported (with diagnostics)
- [ ] Variable passed to another method - emit warning
- [ ] Variable stored in field - emit warning
- [ ] Variable returned from method - emit warning
- [ ] Cross-method variable usage - emit warning

### Diagnostics
- [ ] Update `NURU003` to be more specific about why resolution failed
- [ ] Add suggestions for how to fix unresolvable cases
- [ ] Suggest using `[NuruRouteGroup]` attributes as alternative
- [ ] Suggest using full pattern string as alternative

### Testing
- [ ] Test variable declaration + later Map calls
- [ ] Test code between group creation and Map calls
- [ ] Test conditional usage (if/else with Map calls)
- [ ] Test loop usage
- [ ] Test warning for method parameter
- [ ] Test warning for field storage
- [ ] Test warning for cross-method usage
- [ ] Verify diagnostic messages are helpful

## Notes

### Reference

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 144-165, 1118-1158)

### What Changes from Phase 4

```csharp
// Phase 4: This emits a warning
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // NURU003: Cannot resolve group context

// Phase 5: This works - generator tracks variable within method
var docker = builder.MapGroup("docker");
// ... other code ...
docker.Map("run {image}", handler);  // Resolved via data flow analysis
```

### Still Not Supported (Even in Phase 5)

```csharp
// Passed to another method - can't see inside
var docker = builder.MapGroup("docker");
RegisterDockerCommands(docker);  // Still emits warning

// Stored in a field - cross-method tracking too complex
private IRouteGroupBuilder _docker;

public void Setup(IEndpointCollectionBuilder builder)
{
    _docker = builder.MapGroup("docker");
}

public void RegisterCommands()
{
    _docker.Map("run {image}", handler);  // Still emits warning
}
```

### Recommendation for Unsupported Cases

For complex scenarios where data flow analysis can't resolve group context:
1. Use `[NuruRouteGroup]` attributes (always works)
2. Or specify the full pattern: `app.Map("docker run {image}", handler)`

### Implementation Approach

Data flow analysis in Roslyn:
1. Use `SemanticModel.AnalyzeDataFlow()` for method body
2. Track `ILocalSymbol` for group variable
3. Find all `InvocationExpressionSyntax` on that symbol
4. Build mapping of variable -> group configuration

### Releasable

Yes - quality of life improvement for `MapGroup()` users. No breaking changes.
