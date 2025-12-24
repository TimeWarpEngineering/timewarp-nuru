# Create sequential test diagnostic runner to find hanging tests

## Parent

#261 Baseline Test Run and V2 Gap Analysis

## Description

Create a diagnostic runfile (`tests/scripts/run-tests-sequential.cs`) that runs each test file individually with timeouts to identify which tests cause the multi-mode runner to hang after the DynamicInvoke fallback was disabled.

## Background

After disabling the DynamicInvoke fallback in `delegate-executor.cs`:
- Individual test files run fine (pass or fail cleanly with error messages)
- The multi-mode runner (`run-ci-tests.cs`) hangs
- We need to identify which specific test(s) cause the hang
- Output buffer doesn't flush before hang, so we can't see which test is problematic

## Checklist

- [ ] Create `tests/scripts/run-tests-sequential.cs` runfile
- [ ] Clear runfile cache at start (`ganda runfile cache --clear`)
- [ ] Discover all test runfiles (files with `#!/usr/bin/dotnet` shebang)
- [ ] Compile Phase: Build each file with 60s timeout, track compile failures
- [ ] Run Phase: Run each compiled file with 30s timeout, track run failures/timeouts
- [ ] Write results to `tests/scripts/test-results.md` (clear at start, append as we go)
- [ ] Explicit stdout flush before each test to capture last test before hang
- [ ] Kill hung processes on timeout
- [ ] Run the diagnostic and identify hanging test(s)
- [ ] Document findings

## Implementation Details

### Test File Discovery

Glob patterns matching `Directory.Build.props` order:
```
tests/timewarp-nuru-core-tests/lexer/*.cs
tests/timewarp-nuru-core-tests/ansi-string-utils-*.cs
tests/timewarp-nuru-core-tests/help-provider-*.cs
tests/timewarp-nuru-core-tests/hyperlink-*.cs
tests/timewarp-nuru-core-tests/panel-widget-*.cs
tests/timewarp-nuru-core-tests/rule-widget-*.cs
tests/timewarp-nuru-core-tests/table-widget-*.cs
tests/timewarp-nuru-core-tests/invoker-registry-*.cs
tests/timewarp-nuru-core-tests/message-type-*.cs
tests/timewarp-nuru-core-tests/nuru-route-registry-*.cs
tests/timewarp-nuru-core-tests/test-terminal-context-*.cs
tests/timewarp-nuru-core-tests/parser/*.cs
tests/timewarp-nuru-core-tests/routing/*.cs
tests/timewarp-nuru-core-tests/configuration/*.cs
tests/timewarp-nuru-core-tests/options/*.cs
tests/timewarp-nuru-core-tests/type-conversion/*.cs
tests/timewarp-nuru-completion-tests/static/*.cs
tests/timewarp-nuru-completion-tests/dynamic/*.cs
tests/timewarp-nuru-completion-tests/engine/*.cs
tests/timewarp-nuru-repl-tests/**/*.cs
tests/timewarp-nuru-mcp-tests/*.cs
```

### Filter Criteria

- First line must start with `#!/usr/bin/dotnet` (excludes helper files)
- Exclude `**/obj/**`, `**/bin/**`

### Output Format (`test-results.md`)

```markdown
# Sequential Test Run Results

**Started:** 2024-12-24 10:30:00
**UseNewGen:** false

## Compile Phase

✓ tests/timewarp-nuru-core-tests/lexer/lexer-01-basic.cs (1.2s)
✗ COMPILE FAIL: tests/... - Error message
⏱ COMPILE TIMEOUT: tests/... (killed after 60s)

## Run Phase

✓ tests/timewarp-nuru-core-tests/lexer/lexer-01-basic.cs (0.5s)
✗ RUN FAIL: tests/... (exit code 1)
⏱ RUN TIMEOUT: tests/... (killed after 30s) ← LIKELY HANG

## Summary

| Phase   | Success | Failed | Timeout |
|---------|---------|--------|---------|
| Compile | 45      | 2      | 0       |
| Run     | 40      | 3      | 2       |

**Completed:** 2024-12-24 10:45:00
**Duration:** 15m 30s
```

## Notes

- Once we identify the hanging test(s), we can investigate why they hang vs fail cleanly
- This will help determine if the issue is in the test harness exception handling or specific test patterns
- Results can also be used to identify all tests that need generated invokers (run failures)
