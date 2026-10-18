# Round 1 — tests
**Date:** 2026-10-18
**Scope reviewed:** new and changed tests in `git diff 5e512c0a..f522c6e2`; build, unit tests, full CI suite and sample smoke run on `f522c6e2`

## Summary

Run on their own, all three new test files pass: derivation 19/19, dispatch 4/4 and generator-55 2/2. The solution builds with 0 warnings and 0 errors, and the sample smoke runs work. **The full CI suite fails with exit 1.** Multi-mode ran 1909 tests: 1897 passed, 6 failed and 6 were skipped. All 6 failures are in `OperationEndpointDerivationTests`. That file uses `.DiscoverEndpoints()`, so in multi-mode it picks up a `[NuruRoute("")]` endpoint from another test file, and that endpoint swallows the inputs the tests expect to be rejected. The standalone phase passed: 29 files, 0 failed.

## Test runs

- `ganda runfile cache --clear` → cleared 4808 entries
- `dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs` → 19/19 passed
- `dotnet run tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs` → 4/4 passed
- `dotnet run tests/timewarp-nuru-tests/generator/generator-55-missing-operation-handler.cs` → 2/2 passed
- `dotnet build` of `timewarp-nuru`, the analyzers, `timewarp-nuru-build` (`--no-incremental`) and `timewarp-nuru.slnx` → succeeded, 0 warnings, 0 errors
- `dotnet run tests/ci-tests/run-ci-tests.cs` (about 3m13s) → **exit 1**
  - Multi-mode: 1909 total, 1897 passed, **6 failed**, 6 skipped. The skips are pre-existing.
  - Standalone: 29 files, 0 failed.
  - The 6 failures: `Should_reject_a_missing_required_member`, `Should_require_a_value_for_a_non_nullable_bool_without_an_initializer`, `Should_keep_a_non_nullable_enum_initializer_and_require_one_without`, `Should_prefer_a_route_group_over_nuru_cli_binding`, `Should_let_nuru_route_override_the_operation_name`, `Should_filter_derived_groups`.
- `calculator-contracts.cs -- calc add --X 2 --Y 3` → prints `5`, exit 0
- `calculator-contracts.cs -- --capabilities --group-filter calc` → exit 0, with three routes under groupPath `calc`. `IncludeAverage` has default `true` and is not a flag. `Notes` has type `json global::CalculatorContracts.Endpoints.LabeledAmount[]`.

Acceptance coverage:
- **Covered:** route literal, option names, required members and defaults, enums including non-nullable ones, repeated scalar arrays, JSON members, the bool `true` default, all three group sources, NURU_H008, behavior wrapping, the renderer, and the problem exit code.
- **Weakly covered:** `UseDirectInvoke`, the problem first line, and capabilities parity (Issues 2–4).

## Issues

### Issue 1 — Severity: bug
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:33
- Description: `CreateApp` uses `.DiscoverEndpoints()`. CI compiles every test file into one assembly, so this app also gets every `[NuruRoute]` endpoint from other files. That includes a top-level `[NuruRoute("")]` endpoint with a required `{input}` and `--verbose`, which matches inputs such as `t508quote`, `t508force` and `t508enum` and returns 0. As a result, every "should be rejected (exit 1)" assertion fails in CI, and so does `Should_filter_derived_groups`. The full CI run exits 1, so the PR pipeline will be red. This breaks the repo rule in `.agent/local/nuru-specific.md`.
- Suggestion: Build the apps with explicit `.Map<T508…Query>()` for each needed type (`Map<TOperation>` matches on `Handler.FullTypeName`). Apply the same change to `operation-endpoint-dispatch.cs` (lines 31, 61, 87, 112, 127).
- Status: open

### Issue 2 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:41-105
- Description: The `UseDirectInvoke` test runs against a `[NuruRoute]` endpoint, not an `[Operation]` endpoint. It only asserts that both modes print `direct:ok`, so it would still pass if `UseDirectInvoke` did nothing. It also writes `/tmp/t508-overhead.txt` and never asserts on it.
- Suggestion: Assert something that differs between the two paths, for example a mediator pipeline behavior that runs only under mediator dispatch, and drop the `/tmp` write.
- Status: open

### Issue 3 — Severity: suggestion
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-derivation.cs:295-300
- Description: The capabilities parity check compares only the sorted key names of one option. It does not compare values (`type`, `required`, `isFlag`, `isRepeated`, `defaultValue`, `description`), and it does not compare endpoint-level shape.
- Suggestion: Compare all option properties except `name`, and compare the endpoint-level key sets.
- Status: open

### Issue 4 — Severity: nit
- File: tests/timewarp-nuru-tests/auto/operation-endpoint-dispatch.cs:132
- Description: The acceptance criteria require the problem type and title on the first stderr line. The test uses `ErrorContains(...)`, which passes no matter which line the text is on.
- Suggestion: Assert that line 0 of stderr equals `https://example.test/t508 Bad input`.
- Status: open
