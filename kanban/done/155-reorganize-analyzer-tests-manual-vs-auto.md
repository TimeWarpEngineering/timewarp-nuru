# Task 155: Reorganize Analyzer Tests - Manual vs Auto

## Description

Reorganize the `tests/timewarp-nuru-analyzers-tests/` folder to clearly separate:
- **Automated tests** that run in CI and must pass
- **Manual tests** for validation/documentation purposes

Currently the test runner uses filename patterns to exclude tests, which is fragile. A folder-based approach is cleaner.

## Problem

1. Tests like `should-fail-*` are expected compilation failures - not automated tests
2. Tests like `should-pass-*` are manual validation tests
3. Tests like `test-analyzer-patterns.cs` are documentation examples
4. `nuru-invoker-generator-01-basic.cs` has assembly path issues
5. All automated analyzer tests should pass before merging

## Proposed Structure

```
tests/timewarp-nuru-analyzers-tests/
├── auto/                    # Automated tests (run by test runner)
│   ├── attributed-route-generator-01-basic.cs
│   ├── delegate-signature-01-models.cs
│   └── nuru-invoker-generator-01-basic.cs  # Fix path issues
├── manual/                  # Manual validation tests (not run automatically)
│   ├── should-fail-map-generic-no-sourcegen.cs
│   ├── should-pass-map-generic-with-mediator.cs
│   ├── should-pass-map-non-generic.cs
│   └── test-analyzer-patterns.cs
└── Directory.Build.props
```

## Tasks

- [x] Create `auto/` and `manual/` subfolders
- [x] Move tests to appropriate folders
- [x] Update `run-all-tests.cs` to use `auto/` subfolder for Analyzers
- [x] Remove filename-based exclusion patterns (now unnecessary)
- [x] Fix `nuru-invoker-generator-01-basic.cs` assembly path resolution
- [x] Ensure all tests in `auto/` pass
- [ ] Update any documentation referencing test locations

## Acceptance Criteria

- [x] `dotnet run run-all-tests.cs -- --category Analyzers` passes 100%
- [x] Manual tests remain accessible but don't run automatically
- [x] Clear folder structure documents intent

## Priority

Medium - Important for ongoing source generator work

## Labels

- tests
- infrastructure
- analyzers
