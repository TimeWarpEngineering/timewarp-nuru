# CI Test Coverage Analysis

## Executive Summary

Analysis of tests in `tests/timewarp-nuru-core-tests` that are NOT included in the CI test runner (`tests/ci-tests/run-ci-tests.cs`). Found **11 test files** excluded from CI across 4 categories.

## Progress

- **generator-14-options-validation.cs** - ✅ Added to CI (semantic model refactor enabled lambda interception)
- **generator-03-short-only-options.cs** - ✅ Deleted (redundant - functionality covered by routing-09, routing-12)
- **generator-04-static-service-injection.cs** - ✅ Rewritten to test functionality, added to CI
- **configuration-02-cli-overrides.cs** - ✅ Created and added to CI (7 tests for CLI config override filtering)
- **routing-08-end-of-options.cs** - ✅ Fixed bug #355, removed `UseDebugLogging()`, added to CI (4 tests)
- **routing-15-help-route-priority.cs** - ✅ Rewritten to use TestTerminal pattern (no closures), added to CI (4 pass, 1 skip for Task #356)
- **NURU_H002 suppression removed** - ✅ Closures in handlers are now errors (as designed), removed from `tests/Directory.Build.props`
- **generator-13-ioptions-parameter-injection.cs** - ✅ Added to CI (exclusion reason was outdated - test doesn't read generated files)

## Scope

- **Analyzed**: All `.cs` files in `tests/timewarp-nuru-core-tests/`
- **Reference**: `tests/ci-tests/Directory.Build.props` which defines what's compiled into CI

## Methodology

Compared the glob patterns and explicit includes in `Directory.Build.props` against actual files in `tests/timewarp-nuru-core-tests/`.

## Tests NOT in CI

### Generator Tests (0 files excluded)

All generator tests are now in CI.

**Included in CI**: generator-01, generator-04, generator-10, generator-11, generator-12, generator-13, generator-14

**Deleted**: generator-03 (redundant - short-only options tested in routing-09, routing-12)

**Rewritten**: generator-04 (now tests functionality instead of generated code content)

### Routing Tests (3 files)

| File | Exclusion Reason |
|------|------------------|
| `routing/routing-20-version-route-override.cs` | Uses `UseAllExtensions` API |
| `routing/routing-21-check-updates-version-comparison.cs` | Uses `UseAllExtensions` API |
| `routing/dsl-example.cs` | Not a test (example/demo file) |

**Included in CI**: All routing-*.cs files except 20, 21, dsl-example

**Fixed**: routing-08 - removed `UseDebugLogging()`, fixed bug #355 (positional before `--`), added to CI

**Fixed**: routing-15 - removed `AddAutoHelp()` (now default), rewrote tests to use TestTerminal pattern (no closures), added to CI

### Configuration Tests (1 file excluded)

| File | Exclusion Reason |
|------|------------------|
| `configuration/configuration-01-validate-on-start.cs` | Uses `AddDependencyInjection()` API |

**Included in CI**: configuration-02-cli-overrides.cs (7 tests)

### Options Tests (2 files)

| File | Exclusion Reason |
|------|------------------|
| `options/options-01-mixed-required-optional.cs` | Uses `CreateSlimBuilder()` API |
| `options/options-02-optional-flag-optional-value.cs` | Uses `CreateSlimBuilder()` API |

### Root-level Tests (5 files)

| File | Exclusion Reason |
|------|------------------|
| `capabilities-01-basic.cs` | Not included in any Compile directive |
| `capabilities-02-integration.cs` | Not included in any Compile directive |
| `help-provider-03-session-context.cs` | Uses old APIs (HelpProvider.GetHelpText, CreateSlimBuilder, SessionContext) |
| `message-type-02-help-output.cs` | Disabled (commented out) |
| `nuru-route-registry-01-basic.cs` | Uses `IRequest` which no longer exists |
| `show-help-colors.cs` | Not included in any Compile directive |

## What IS Included in CI

| Category | Pattern/Files |
|----------|---------------|
| Lexer | `lexer/*.cs` (all 15 files) |
| Parser | `parser/*.cs` (all 15 files) |
| Type Conversion | `type-conversion/*.cs` (1 file) |
| Generator | 4 specific files (01, 10, 11, 12) |
| Routing | All except 15, 20, 21, dsl-example |
| Message Type | `message-type-01-fluent-api.cs` only |

## Summary by Category

| Category | Total Files | In CI | Excluded |
|----------|-------------|-------|----------|
| Generator | 6 | 6 | 0 |
| Routing | 19 | 16 | 3 |
| Configuration | 2 | 1 | 1 |
| Options | 2 | 0 | 2 |
| Root-level | 6 | 1 | 5 |
| Lexer | 16 | 16 | 0 |
| Parser | 15 | 15 | 0 |
| Type Conversion | 1 | 1 | 0 |
| **Total** | **67** | **56** | **11** |

## Recommendations

### High Priority - API Migration Needed
These tests use deprecated/removed APIs and need updating:
- `help-provider-03-session-context.cs` - needs API update
- `nuru-route-registry-01-basic.cs` - needs `IRequest` replacement
- `options/options-*.cs` - needs `CreateSlimBuilder()` replacement
- `configuration/configuration-01-validate-on-start.cs` - needs `AddDependencyInjection()` replacement

### Medium Priority - Extension API Tests
These test extension methods that may need source generator support:
- `routing-20-version-route-override.cs` - `UseAllExtensions`
- `routing-21-check-updates-version-comparison.cs` - `UseAllExtensions`

### Low Priority - None
All isolation-required tests have been migrated to CI.

### Consider Deletion
- `routing/dsl-example.cs` - Not a test, appears to be a demo
- `show-help-colors.cs` - Not included, may be obsolete
- `capabilities-01-basic.cs` / `capabilities-02-integration.cs` - Not included, status unclear

## References

- `tests/ci-tests/Directory.Build.props` - CI test configuration
- `tests/ci-tests/run-ci-tests.cs` - CI test runner entry point
