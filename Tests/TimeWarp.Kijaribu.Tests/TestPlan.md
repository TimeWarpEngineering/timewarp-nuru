# Kijaribu Test Runner Test Plan

## Introduction
This test plan outlines the strategy for validating the design and implementation of the `TestRunner` class in the TimeWarp.Kijaribu project. Kijaribu is a lightweight test runner for single-file C# programs, focusing on discovering and executing public static async Task methods as tests. The plan covers unit tests, integration tests, and edge cases to ensure reliability, correctness, and adherence to the documented behavior.

The test runner supports:
- Automatic discovery of test methods via reflection.
- Async test execution.
- Parameterized tests using `[Input]` attributes.
- Tag-based filtering (`[TestTag]`) at class and method levels.
- Skipping tests with `[Skip]`.
- Optional cache clearing with `[ClearRunfileCache]`.
- Basic reporting and exit codes.

This plan will guide the creation of test classes under `Tests/TimeWarp.Kijaribu.Tests/` to self-validate the runner.

## Scope
- **In Scope**: Core functionality (discovery, execution, filtering, skipping, parameterization, cache management, reporting). Compatibility with .NET 10.0+ single-file scenarios.
- **Out of Scope**: Performance benchmarks (use Benchmarks/ project), security testing, cross-platform specifics beyond Linux/Windows/macOS basics, integration with external test frameworks (e.g., xUnit).
- **Assumptions**: Tests run in a development environment with .NET SDK installed. Custom attributes (`[Input]`, `[Skip]`, etc.) are defined in the Kijaribu project. Reflection-based discovery assumes trusted code.

## Test Objectives
- Verify test discovery and execution for valid/invalid methods.
- Ensure filtering skips correctly without affecting untagged tests.
- Validate parameterized tests run multiple times with correct args.
- Confirm exceptions are caught and reported properly.
- Test cache clearing doesn't delete current assembly's cache.
- Ensure static counters reset per run (or handle accumulation if by design).
- Check console output matches expected formats.
- Validate exit codes: 0 for all pass, 1 for any failure.

## Test Environment
- **Runtime**: .NET 10.0 (single-file publish mode where applicable).
- **Platform**: Linux (primary), with notes for Windows/macOS path differences in cache clearing.
- **Tools**: `dotnet build`, `dotnet run` for invoking `TestRunner.RunTests<T>()`.
- **Test Framework**: Self-hosted via Kijaribu (ironic self-testing); supplement with manual assertions or simple console checks.

## Test Categories and Cases

### 1. Test Discovery and Execution
**Objective**: Ensure only qualifying methods (public, static, async Task, not Setup/CleanUp) are discovered and executed.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| DISC-01 | Basic test method execution | Test class with one valid test method | Call `RunTests<BasicTests>()` | Method invoked, "✓ PASSED" output, PassCount=1, TotalTests=1, exit=0 | High |
| DISC-02 | Skip non-qualifying methods | Test class with public static void, private async Task, Setup/CleanUp | Call `RunTests<NonQualifyingTests>()` | No invocation, TotalTests=0 (or 1 if counting skipped implicitly) | High |
| DISC-03 | Multiple test methods | Test class with 3 valid tests (2 pass, 1 fails) | Call `RunTests<MultiTest>()` | All executed, correct pass/fail counts, exit=1 | High |
| DISC-04 | Async test with await | Test method with `await Task.Delay(1)` | Call `RunTests<AsyncTest>()` | Await completes, "✓ PASSED" | Medium |
| DISC-05 | ValueTask support (future) | Test method returning ValueTask | Call `RunTests<ValueTaskTest>()` | Executed successfully (if implemented) | Low |

### 2. Parameterized Tests
**Objective**: Validate `[Input]` attributes drive multiple invocations.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| PARAM-01 | Single [Input] with args | Method with 2 params, one [Input("arg1", 42)] | Call `RunTests<ParamTest>()` | Invoked once with args, display name shows "(arg1, 42)", PASS | High |
| PARAM-02 | Multiple [Input] | Two [Input] attrs with different args | Call `RunTests<MultiParamTest>()` | Invoked twice, TotalTests=2, correct display names | High |
| PARAM-03 | No [Input] - empty params | Method with no attrs | Call `RunTests<NoParamTest>()` | Invoked once with empty array, display name without params | High |
| PARAM-04 | Type mismatch in [Input] | [Input] string for int param | Call `RunTests<TypeMismatchTest>()` | Invocation fails with ArgumentException, reported as ✗ FAILED | Medium |
| PARAM-05 | Null params handling | [Input(nullValue)] for nullable param | Call `RunTests<NullParamTest>()` | Passed as null, "null" in display name | Medium |

### 3. Tag Filtering
**Objective**: Confirm class/method-level filtering via param or env var.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| TAG-01 | Class-level match | Class with [TestTag("feature1")], filter="feature1" | Call `RunTests<ClassTagged>()` with filter | All methods run | High |
| TAG-02 | Class-level mismatch | Class with [TestTag("other")], filter="feature1" | Call `RunTests<ClassMismatched>()` with filter | No methods run, TotalTests=0, exit=0 | High |
| TAG-03 | Method-level match | Untagged class, method with [TestTag("feature1")], filter="feature1" | Call `RunTests<MethodTagged>()` with filter | Tagged method runs, others skipped if tagged differently | High |
| TAG-04 | Method-level mismatch | Method with [TestTag("other")], filter="feature1" | Call `RunTests<MethodMismatched>()` with filter | Method skipped, "⚠ SKIPPED: No matching tag", TotalTests=1 | High |
| TAG-05 | Untagged method with filter | No tags, filter="feature1" | Call `RunTests<Untagged>()` with filter | Method runs (implicit match) | High |
| TAG-06 | Env var filtering | Set KIJARIBU_FILTER_TAG="feature1", no param | Call `RunTests<EnvFilterTest>()` | Filters as per env var | Medium |
| TAG-07 | Case-insensitive matching | Tags "Feature1" vs filter "feature1" | Call with filter | Matches | Medium |

### 4. Skipping and Exceptions
**Objective**: Handle [Skip] and runtime errors gracefully.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| SKIP-01 | [Skip] with reason | Method with [Skip("WIP")] | Call `RunTests<SkippedTest>()` | "⚠ SKIPPED: WIP", TotalTests=1, PassCount=0, exit=1? (debate: skip as non-failure) | High |
| SKIP-02 | Runtime exception in test | Test throws ArgumentException | Call `RunTests<ExceptionTest>()` | "✗ FAILED: ArgumentException", message shown, exit=1 | High |
| SKIP-03 | TargetInvocationException unwrapping | Reflection-invoked exception | Call `RunTests<InvocationTest>()` | Inner exception type/message shown | High |
| SKIP-04 | Async exception | Await throws after delay | Call `RunTests<AsyncExceptionTest>()` | Caught and reported | Medium |

### 5. Cache Clearing
**Objective**: Ensure safe deletion of .NET runfile cache.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| CACHE-01 | Clear with [ClearRunfileCache] | Class with attribute Enabled=true | Call `RunTests<CacheClearTest>()` | Cache dirs deleted (except current), "✓ Clearing runfile cache:" output | High |
| CACHE-02 | No clear (default) | No attribute, clearCache=false | Call `RunTests<NoCacheTest>()` | No deletion/output | High |
| CACHE-03 | Skip current assembly | Run in temp dir with cache | Call with clear=true | Current exe dir not deleted | High |
| CACHE-04 | Empty cache dir | No runfile cache exists | Call with clear=true | No error, silent return | Medium |
| CACHE-05 | Path differences (Windows) | Simulate Windows paths | Manual verification | Handles backslashes correctly | Low |

### 6. Reporting and Cleanup
**Objective**: Validate output, counters, and post-run actions.

| Test Case ID | Description | Preconditions | Steps | Expected Result | Priority |
|--------------|-------------|---------------|-------|-----------------|----------|
| REPORT-01 | Summary output | 2/3 tests pass | Call `RunTests<ReportTest>()` | "Results: 2/3 tests passed", correct emojis | High |
| REPORT-02 | Filtered summary | Filter skips 1, 2 pass | Call with filter | TotalTests=3 (including skipped?), accurate count | High |
| REPORT-03 | CleanUp invocation | Static void CleanUp method | Call `RunTests<CleanupTest>()` | Invoked after tests (verify via log/assert) | Medium |
| REPORT-04 | Async CleanUp (future) | If Task-returning CleanUp | Call | Awaited properly | Low |
| REPORT-05 | Counter reset (if implemented) | Multiple RunTests calls | Sequential calls | Counters reset per call | Medium |

### 7. Edge Cases
- **DISC-EDGE-01**: Generic test methods – Ensure reflection handles.
- **PARAM-EDGE-01**: [Input] with 0 params for multi-param method – Fail gracefully.
- **TAG-EDGE-01**: Multiple tags per method/class – Any match suffices.
- **CACHE-EDGE-01**: Permission denied on cache dir – Handle IOException.
- **REPORT-EDGE-01**: 0 tests – "Results: 0/0", exit=0.

## Test Execution Strategy
- **Automation**: Write test classes (e.g., `DiscoveryTests.cs`) using Kijaribu itself. Use a meta-runner script (e.g., `run-kijaribu-tests.cs`) to invoke and assert outputs/exit codes.
- **Manual Verification**: For cache clearing and console output, inspect logs/files.
- **Coverage**: Aim for 90%+ code coverage (use `dotnet test --collect:"XPlat Code Coverage"` if integrating with MSTest).
- **Pass/Fail Criteria**: All high-priority cases pass; no regressions in existing Nuru tests using Kijaribu.

## Risks and Mitigations
- **Risk**: Self-testing circularity – **Mitigation**: Use simple assertions in tests; fallback to manual runs.
- **Risk**: Cache clearing affects other projects – **Mitigation**: Run in isolated temp dirs.
- **Risk**: Reflection changes in future .NET – **Mitigation**: Pin to .NET 10.0; monitor updates.

## Next Steps
1. Create test class files based on this plan (e.g., `DiscoveryTests.cs`).
2. Implement meta-runner for automated validation.
3. Run plan and document results in `TestResults.md`.
4. Iterate on TestRunner based on findings.

*Document Version: 1.0 | Date: 2025-10-05*