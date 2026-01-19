# Create sequential test diagnostic runner to find hanging tests

## Parent

#261 Baseline Test Run and V2 Gap Analysis

## Description

Create a diagnostic runfile (`tests/scripts/run-tests-sequential.cs`) that runs each test file individually with timeouts to identify which tests cause the multi-mode runner to hang after the DynamicInvoke fallback was disabled.

## Checklist

- [x] Create `tests/scripts/run-tests-sequential.cs` runfile
- [x] Clear runfile cache at start (`ganda runfile cache --clear`)
- [x] Discover all test runfiles (files with `#!/usr/bin/dotnet` shebang)
- [x] Compile Phase: Build each file with 60s timeout, track compile failures
- [x] Run Phase: Run each compiled file with 30s timeout, track run failures/timeouts
- [x] Write results to `tests/scripts/test-results.md` (clear at start, append as we go)
- [x] Explicit stdout flush before each test to capture last test before hang
- [x] Kill hung processes on timeout
- [x] Run the diagnostic and identify hanging test(s)
- [x] Document findings
- [x] Add --v1/--v2 flags to set UseNewGen before running

## Usage

```bash
# Use current UseNewGen value
dotnet tests/scripts/run-tests-sequential.cs

# Set UseNewGen=false (V1 generators)
dotnet tests/scripts/run-tests-sequential.cs --v1

# Set UseNewGen=true (V2 generators)
dotnet tests/scripts/run-tests-sequential.cs --v2
```

## Results

### Findings

**No hanging tests found.** All tests run to completion (pass or fail) when executed individually.

With UseNewGen=true (V2 generators):
- Compile: 136 success, 17 failed, 0 timeout
- Run: 105 success, 31 failed, 0 timeout

The 17 compile failures are lexer tests that require the multi-mode helper (`CreateLexer` from `lexer-test-helper.cs`).

The 31 run failures are tests that execute handlers and get "No generated invoker found" error - expected with V2 since invoker generation isn't implemented yet.

### Conclusion

The hang occurs specifically in the **multi-mode test runner** (`runfiles/test.cs` / `tests/ci-tests/`), not in any individual test. This is likely due to how exceptions are handled when all tests are compiled and run together in a single process.

For now, this sequential runner can be used as an alternative test runner that avoids the multi-mode hang issue.

## Notes

- Results written to `tests/scripts/test-results.md`
- Duration: ~16 minutes for full run
- The script modifies `Directory.Build.props` when --v1 or --v2 flags are used
