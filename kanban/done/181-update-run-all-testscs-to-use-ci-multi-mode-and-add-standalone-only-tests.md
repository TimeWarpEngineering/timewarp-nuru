# Update run-all-tests.cs to use CI multi-mode and add standalone-only tests

## Description

Refactor `tests/scripts/run-all-tests.cs` to:
1. Run the CI multi-mode test runner (`tests/ci-tests/run-ci-tests.cs`) as the primary test execution
2. Run standalone-only tests that cannot be included in multi-mode compilation

## Background

The CI multi-mode runner compiles all migrated tests into a single assembly for faster execution (1702 tests in ~12s). However, some tests must run standalone due to conflicts:

### Standalone-Only Tests (cannot run in multi-mode)

| File | Reason |
|------|--------|
| `routing/routing-11-delegate-mediator.cs` | Mediator source generator conflicts |
| `routing/routing-22-async-task-int-return.cs` | Mediator source generator conflicts |
| `factory/factory-01-static-methods.cs` | Mediator source generator conflicts |
| `options/options-03-nuru-context.cs` | NuruContext feature not implemented |

### Analyzer Tests (special handling)

| File | Reason |
|------|--------|
| `analyzers-tests/auto/*.cs` (3 files) | Roslyn compilation API, needs investigation |
| `analyzers-tests/manual/*.cs` (4 files) | Manual verification, not Jaribu format |

## Checklist

- [ ] Modify run-all-tests.cs to first run CI multi-mode tests
- [ ] Add standalone execution for routing-11, routing-22, factory-01, options-03
- [ ] Decide how to handle analyzer tests (include or skip)
- [ ] Update summary output to show both multi-mode and standalone results
- [ ] Test the updated script
- [ ] Commit changes

## Notes

Current run-all-tests.cs runs each test file individually which is slow. The new approach:
1. Run `dotnet tests/ci-tests/run-ci-tests.cs` (1702 tests, ~12s)
2. Run 4 standalone tests individually (~4 tests, ~20s)
3. Optionally run analyzer tests

This should significantly speed up the full test suite while ensuring all tests are executed.
