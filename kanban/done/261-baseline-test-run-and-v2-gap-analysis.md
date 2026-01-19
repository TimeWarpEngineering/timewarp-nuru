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
- [x] Investigate multi-mode runner hang issue
- [x] Create diagnostic runfile for sequential test execution

## Results

### Final V2 Test Results (Sequential Runner)

| Phase   | Success | Failed | Timeout |
|---------|---------|--------|---------|
| Compile | 151     | 2      | 0       |
| Run     | 120     | 31     | 0       |

### Compile Failures (2 - Known Issues)

1. `ansi-string-utils-01-basic.cs` - **Fixed**: Moved to timewarp-terminal repo (tests TimeWarp.Terminal, not Nuru)
2. `options-03-nuru-context.cs` - Uses `NuruContext` which isn't implemented yet (excluded from CI)

### Run Failures (31) - What V2 Generator Must Emit

All 31 failures are due to **"No generated invoker found"** - tests that execute handlers via `NuruApp.RunAsync()`:

**Routing Tests (20 failures):**
- `routing-01-basic-matching` through `routing-18-option-alias-with-description`
- `routing-22-async-task-int-return`
- `routing-23-multiple-map-same-handler`
- (Note: `routing-19`, `routing-20`, `routing-21` pass - don't execute handlers)

**Options Tests (2 failures):**
- `options-01-mixed-required-optional`
- `options-02-optional-flag-optional-value`

**Completion Dynamic Tests (5 failures):**
- `completion-21-integration-enabledynamic`
- `completion-22-callback-protocol`
- `completion-23-custom-sources`
- `completion-24-context-aware`
- `completion-25-output-format`

**REPL Tests (3 failures):**
- `repl-23-key-binding-profiles`
- `repl-32-multiline-editing`
- `repl-33-yank-arguments`

**Other (1 failure):**
- `test-terminal-context-01-basic`

### Tests That Pass (120)

Tests that don't execute handlers work fine:
- All 15 lexer tests
- All 15 parser tests
- Widget tests (panel, table, rule, hyperlink)
- Help provider tests
- Completion static tests
- Completion engine tests
- Most REPL tests
- MCP tests

### Multi-Mode Runner Issue

The multi-mode runner (`dotnet runfiles/test.cs`) hangs when running with V2. The sequential runner (`tests/scripts/run-tests-sequential.cs`) works and found **no individual test causes a hang**. The hang occurs in the multi-mode compilation/execution context, not in any specific test.

**Workaround**: Use sequential runner for V2 testing:
```bash
dotnet tests/scripts/run-tests-sequential.cs --v2
```

### What V2 Generator Must Implement (Task #262)

The V2 generator needs to emit **typed invokers** for delegate signatures used in Map() calls:

| Signature Pattern | Example Delegate | Count |
|-------------------|------------------|-------|
| `_Returns_Int` | `() => int` | Many |
| `_String_Returns_Int` | `(string) => int` | Several |
| `_String_String_Returns_Int` | `(string, string) => int` | Several |
| (async variants) | `() => Task<int>` | Several |

The invokers must:
1. Be registered in `InvokerRegistry` at startup
2. Handle the specific parameter binding for each signature
3. Support both sync and async return types

### Files Modified

- `source/timewarp-nuru-core/execution/delegate-executor.cs` - Disabled DynamicInvoke fallback
- `tests/scripts/run-tests-sequential.cs` - Created sequential runner with --v1/--v2 flags
- `tests/timewarp-nuru-core-tests/lexer/Directory.Build.props` - Fixed standalone lexer test execution

### Cleanup Completed

- Moved `ansi-string-utils-*.cs` tests to `timewarp-terminal` repo
- Removed orphaned `tests/timewarp-nuru-tests/` directory
- Moved `test-plan-overview.md` to `tests/timewarp-nuru-core-tests/`

## Notes

- Results saved to `tests/scripts/test-results-v1.md` and `tests/scripts/test-results-v2.md`
- Sequential runner takes ~16 minutes for full suite
- V1 tests should all pass (120+ tests) - this is the baseline
