# Implement Unified Pipeline (Phase 3)

## Description

All routes flow through Mediator pipeline. Remove `DelegateExecutor` - no more direct delegate invocation. Single code path for `CompiledRoute` construction.

**Goal:** Consistent middleware behavior for ALL routes. Simplified internals.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete
- Task 150: Implement Attributed Routes (Phase 1) - must be complete
- Task 151: Implement Delegate Generation (Phase 2) - must be complete

## Checklist

### Remove DelegateExecutor
- [ ] Identify all usages of `DelegateExecutor`
- [ ] Verify all delegate routes now generate Command/Handler
- [ ] Remove `DelegateExecutor` class
- [ ] Remove delegate invocation code paths
- [ ] Update tests that relied on direct delegate execution

### Unify Compiler
- [ ] Refactor `Compiler` to use `CompiledRouteBuilder` internally
- [ ] Single mechanism for constructing `CompiledRoute` instances
- [ ] Remove duplicate route construction logic
- [ ] Verify all routes still work correctly

### Pipeline Consistency
- [ ] Verify logging middleware works for all routes
- [ ] Verify validation middleware works for all routes
- [ ] Verify telemetry middleware works for all routes
- [ ] Verify custom middleware works for all routes
- [ ] No "delegate routes skip middleware" behavior

### Testing
- [ ] All existing tests pass
- [ ] Test middleware execution for delegate-origin routes
- [ ] Test middleware execution for command-origin routes
- [ ] Test middleware execution for attributed routes
- [ ] Verify identical behavior regardless of route origin

### Cleanup
- [ ] Remove dead code paths
- [ ] Update internal documentation/comments
- [ ] Simplify route registration internals

## Notes

### Reference

- **Design doc:** `kanban/to-do/148-generate-command-and-handler-from-delegate-map-calls/fluent-route-builder-design.md` (lines 99-114)

### Benefits

| Benefit | Explanation |
|---------|-------------|
| **Unified middleware** | Logging, validation, telemetry work on ALL routes |
| **Consistent behavior** | No "delegate routes skip middleware" surprises |
| **AOT friendly** | No reflection for delegate invocation |
| **Testable** | Generated commands can be unit tested directly |
| **Debuggable** | Step through generated handler code |
| **Pipeline power** | Retry, circuit breaker, caching - all work everywhere |

### Runtime Model

```
At runtime, ALL routes become:

  args[] -> Route Match -> Command Instance -> Pipeline -> Handler

No delegates exist at runtime. Only commands through the pipeline.
```

### Key Insight

After this phase: **Delegates are developer convenience; commands are the runtime reality.**

### Releasable

Yes - internal simplification with consistent runtime behavior. No breaking API changes.
