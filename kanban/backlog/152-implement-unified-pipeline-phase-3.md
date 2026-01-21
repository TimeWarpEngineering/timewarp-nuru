# Implement Unified Pipeline (Phase 3)

## Description

All routes flow through Mediator pipeline. Remove `DelegateExecutor` - no more direct delegate invocation. Single code path for `CompiledRoute` construction.

**Goal:** Consistent middleware behavior for ALL routes. Simplified internals.

## Parent

148-generate-command-and-handler-from-delegate-map-calls

## Dependencies

- Task 149: Implement CompiledRouteBuilder (Phase 0) - must be complete
- Task 150: Implement Endpoints (Phase 1) - must be complete
- Task 151: Implement Delegate Generation (Phase 2) - must be complete ✅

## Status: BLOCKED - Performance Investigation Needed

**December 2025 Benchmark Results** revealed a critical performance issue that must be addressed before proceeding with Phase 3.

### Benchmark Results (December 20, 2025)

| Rank | Framework                | Mean        | Ratio  | Memory     |
| ---- | ------------------------ | ----------- | ------ | ---------- |
| 1    | ConsoleAppFramework v5   | 0.79 ms     | 1.00   | 288 B      |
| **2**    | **TimeWarp.Nuru.Direct**     | **3.98 ms**     | **5.03**   | **14,776 B**   |
| 3    | CommandLineParser        | 10.94 ms    | 13.82  | 46,368 B   |
| 4    | System.CommandLine       | 19.39 ms    | 24.50  | 15,240 B   |
| 5    | CliFx                    | 23.74 ms    | 30.00  | 76,192 B   |
| 6    | PowerArgs                | 32.11 ms    | 40.57  | 77,248 B   |
| 7    | McMaster.Extensions      | 38.88 ms    | 49.13  | 57,480 B   |
| 8    | Cocona.Lite              | 50.13 ms    | 63.34  | 62,528 B   |
| 9    | Spectre.Console.Cli      | 57.66 ms    | 72.86  | 75,016 B   |
| 10   | Cocona                   | 67.10 ms    | 84.78  | 690,240 B  |
| **11**   | **TimeWarp.Nuru (Mediator)** | **131.96 ms**   | **166.73** | **221,152 B**  |

### Key Findings

1. **Nuru.Direct (CreateEmptyBuilder) is 2nd place!** - 3.98 ms, only 5x slower than ConsoleAppFramework
2. **Nuru Mediator (CreateBuilder) is dead last** - 131.96 ms, **33x slower than Direct**
3. **Massive regression from July 2025** - Was 34.4 ms (6th place), now 131.96 ms (11th place)

### Comparison: Direct vs Full Builder

| Aspect | Direct (Empty) | Full (Mediator) | Difference |
| ------ | -------------- | --------------- | ---------- |
| Time   | 3.98 ms        | 131.96 ms       | **33x slower** |
| Memory | 14.8 KB        | 221.2 KB        | **15x more**   |

### Analysis

- **Nuru's core parsing/routing is fast** (Direct proves this - 2nd place!)
- **DI/Mediator setup is the bottleneck** (Full builder cold-start is terrible)
- **Source generators should have helped, not hurt** - Need to investigate why performance regressed
- Previous results had reflection and were still middle of pack (34 ms)
- Now with source gen we're worse (132 ms) - something is very wrong

### Architectural Decision

**Keep `CreateEmptyBuilder` and `CreateSlimBuilder`:**
- Empty provides 33x performance advantage for benchmarks and simple CLIs
- Slim is a middle ground matching .NET's Empty/Slim/Full pattern
- Full (CreateBuilder) needs serious performance investigation before Phase 3 proceeds

### Pre-requisite: New Task Needed

Before Phase 3 can proceed, we need to:
1. **Investigate Full builder startup regression** - Why did we go from 34ms to 132ms?
2. **Profile Mediator/DI initialization** - What's taking so long?
3. **Optimize or defer DI initialization** - Can it be lazy?

## Original Checklist (Deferred)

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
- [ ] Test middleware execution for endpoints
- [ ] Verify identical behavior regardless of route origin

### Cleanup
- [ ] Remove dead code paths
- [ ] Update internal documentation/comments
- [ ] Simplify route registration internals

## Notes

### Reference

- **Design doc:** `kanban/in-progress/148-epic-nuru-3-unified-route-pipeline/fluent-route-builder-design.md`

### Benefits (when performance is fixed)

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

But first: **Fix the 132ms cold-start problem!**
