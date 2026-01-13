# CI Test Coverage Analysis

## Executive Summary

Analysis of tests in `tests/timewarp-nuru-core-tests` that are NOT included in the CI test runner (`tests/ci-tests/run-ci-tests.cs`). Found **15 test files** excluded from CI across 5 categories.

## Progress

- **generator-14-options-validation.cs** - ✅ Added to CI (semantic model refactor enabled lambda interception)
- **generator-03-short-only-options.cs** - ✅ Deleted (redundant - functionality covered by routing-09, routing-12)
- **generator-04-static-service-injection.cs** - ✅ Rewritten to test functionality, added to CI

## Scope

- **Analyzed**: All `.cs` files in `tests/timewarp-nuru-core-tests/`
- **Reference**: `tests/ci-tests/Directory.Build.props` which defines what's compiled into CI

## Methodology

Compared the glob patterns and explicit includes in `Directory.Build.props` against actual files in `tests/timewarp-nuru-core-tests/`.

## Tests NOT in CI

### Generator Tests (1 file)

| File | Exclusion Reason |
|------|------------------|
| `generator/generator-13-ioptions-parameter-injection.cs` | Reads generated file from path based on runfile name |

**Included in CI**: generator-01, generator-04, generator-10, generator-11, generator-12, generator-14

**Deleted**: generator-03 (redundant - short-only options tested in routing-09, routing-12)

**Rewritten**: generator-04 (now tests functionality instead of generated code content)

### Routing Tests (5 files)

| File | Exclusion Reason |
|------|------------------|
| `routing/routing-08-end-of-options.cs` | Uses `UseDebugLogging` API |
| `routing/routing-15-help-route-priority.cs` | Uses `AddAutoHelp` API |
| `routing/routing-20-version-route-override.cs` | Uses `UseAllExtensions` API |
| `routing/routing-21-check-updates-version-comparison.cs` | Uses `UseAllExtensions` API |
| `routing/dsl-example.cs` | Not a test (example/demo file) |

**Included in CI**: All other routing-*.cs files

### Configuration Tests (1 file)

| File | Exclusion Reason |
|------|------------------|
| `configuration/configuration-01-validate-on-start.cs` | Uses `AddDependencyInjection()` API |

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
| Routing | All except 08, 15, 20, 21, dsl-example |
| Message Type | `message-type-01-fluent-api.cs` only |

## Summary by Category

| Category | Total Files | In CI | Excluded |
|----------|-------------|-------|----------|
| Generator | 7 | 6 | 1 |
| Routing | 19 | 14 | 5 |
| Configuration | 1 | 0 | 1 |
| Options | 2 | 0 | 2 |
| Root-level | 6 | 1 | 5 |
| Lexer | 16 | 16 | 0 |
| Parser | 15 | 15 | 0 |
| Type Conversion | 1 | 1 | 0 |
| **Total** | **67** | **53** | **14** |

## Recommendations

### High Priority - API Migration Needed
These tests use deprecated/removed APIs and need updating:
- `help-provider-03-session-context.cs` - needs API update
- `nuru-route-registry-01-basic.cs` - needs `IRequest` replacement
- `options/options-*.cs` - needs `CreateSlimBuilder()` replacement
- `configuration/configuration-01-validate-on-start.cs` - needs `AddDependencyInjection()` replacement

### Medium Priority - Extension API Tests
These test extension methods that may need source generator support:
- `routing-08-end-of-options.cs` - `UseDebugLogging`
- `routing-15-help-route-priority.cs` - `AddAutoHelp`
- `routing-20-version-route-override.cs` - `UseAllExtensions`
- `routing-21-check-updates-version-comparison.cs` - `UseAllExtensions`

### Low Priority - Isolation Required
These work but must run in isolation (not multi-mode compatible):
- `generator-13-ioptions-parameter-injection.cs` - reads generated file content

**Note**: Testing generated file content is fragile. Consider rewriting to test functionality instead, then it can be added to CI.

### Consider Deletion
- `routing/dsl-example.cs` - Not a test, appears to be a demo
- `show-help-colors.cs` - Not included, may be obsolete
- `capabilities-01-basic.cs` / `capabilities-02-integration.cs` - Not included, status unclear

## References

- `tests/ci-tests/Directory.Build.props` - CI test configuration
- `tests/ci-tests/run-ci-tests.cs` - CI test runner entry point
