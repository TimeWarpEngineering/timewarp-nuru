# Baseline Test Run and V2 Gap Analysis

## Parent

#239 Epic: Compile-time endpoint generation

## Description

Run test suite with both V1 and V2 to identify what V2 needs to generate and which tests are V1-specific.

## Checklist

- [x] Run `dotnet runfiles/test.cs` with V1 (baseline - all should pass)
- [x] Run `dotnet runfiles/test.cs` with `UseNewGen=true`
- [x] Document which tests fail and why
- [x] Categorize failures:
  - Missing generated code (V2 needs to emit this)
  - V1-specific tests (candidates for deletion/update)
  - Behavior changes (needs investigation)
- [x] Create summary report of test compatibility
- [x] Disable DynamicInvoke fallback to expose actual V2 gaps
- [x] Run targeted tests with fallback disabled
- [ ] Investigate multi-mode runner hang issue
- [ ] Create diagnostic runfile for sequential test execution

## Results

### Test Results Summary

| Mode | Command | Result |
|------|---------|--------|
| V1 (default) | `dotnet runfiles/test.cs` | All tests pass |
| V2 | `dotnet runfiles/test.cs -p:UseNewGen=true` | All tests pass |

### Analysis

**No test failures occurred** because the CI test suite (`tests/ci-tests/`) tests the **runtime library** code (lexer, parser, help provider, ANSI utilities), not the source-generated code.

### Test Architecture

The test infrastructure is split into two areas:

1. **CI Tests** (`tests/ci-tests/run-ci-tests.cs`)
   - Tests: tokenization, parsing, help provider, ANSI string utilities
   - These test the `timewarp-nuru-core` and `timewarp-nuru-repl` runtime code
   - **Not affected by V1/V2 toggle** - same runtime code is used regardless

2. **Analyzer Tests** (`tests/timewarp-nuru-analyzers-tests/`)
   - Tests: source generator output verification
   - Standalone - not included in CI suite
   - These **would** be affected by V1/V2 toggle
   - Currently test V1 generators only

### V2 Gap Analysis

Since CI tests pass with V2 (no generated code), this confirms:

1. **The toggle infrastructure works correctly** - V1 generators are skipped
2. **Runtime library is generator-agnostic** - works with any generated code (or none)
3. **V2 needs to emit code for consuming projects** - samples, test-apps, benchmarks

### What V2 Must Generate

To make consuming projects work with `UseNewGen=true`:

| V1 Generator | Output File | V2 Equivalent Needed |
|-------------|-------------|---------------------|
| `NuruInvokerGenerator` | `GeneratedRouteInvokers.g.cs` | Typed invoker methods |
| `NuruDelegateCommandGenerator` | `GeneratedDelegateCommands.g.cs` | Command/Query classes from delegates |
| `NuruAttributedRouteGenerator` | `GeneratedAttributedRoutes.g.cs` | Route registration from attributes |

### Recommendation

The CI tests don't reveal V2 gaps because they test runtime code. To identify actual gaps:

1. Build a sample app with `UseNewGen=true`
2. Observe compilation errors (missing generated types)
3. Implement V2 generator to emit those types

---

## Updated Findings (DynamicInvoke Fallback Disabled)

### Test Results with DynamicInvoke Fallback Disabled

We modified `source/timewarp-nuru-core/execution/delegate-executor.cs` to throw an exception instead of falling back to `DynamicInvoke` when no generated invoker is found. This exposed the following:

| Mode | Test File | Result |
|------|-----------|--------|
| V1 (UseNewGen=false) | routing-01-basic-matching.cs | 9/9 passed |
| V2 (UseNewGen=true) | routing-01-basic-matching.cs | 6/9 passed, 3 failed |

### Key Error Message
```
No generated invoker found for signature '_Returns_Int'. Ensure the source generator is running and emitting invokers. DynamicInvoke fallback has been disabled to enforce AOT compatibility.
```

### Why CI Tests Passed Before (Gap Identified)

1. The runtime had a silent `DynamicInvoke` fallback that masked generator failures
2. Tests would "pass" even when source generators weren't emitting required invokers
3. This defeats the purpose of testing AOT compatibility

### Multi-Mode Runner Hang Issue

When running the full test suite with V2 (`UseNewGen=true`), the multi-mode runner hangs instead of cleanly reporting failures. We need to investigate which test causes the hang.

### What V2 Must Generate

V2 generator needs to emit typed invokers for delegate signatures like:
- `_Returns_Int` - `() => int`
- (More signatures to be collected by running full test suite)

### Files Modified

- `source/timewarp-nuru-core/execution/delegate-executor.cs` - Disabled DynamicInvoke fallback, now throws exception with helpful message

### Next Steps

1. Create diagnostic runfile to run tests sequentially and find which test causes the hang
2. Fix the hang issue
3. Collect full list of required invoker signatures
4. Implement V2 generator to emit those signatures (task #262)
