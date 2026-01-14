# CI Test Coverage Analysis

## Executive Summary

Analysis of tests in `tests/timewarp-nuru-core-tests` that are NOT included in the CI test runner (`tests/ci-tests/run-ci-tests.cs`). Found **9 test files** excluded from CI across 4 categories.

## Progress

- **generator-14-options-validation.cs** - ✅ Added to CI (semantic model refactor enabled lambda interception)
- **generator-03-short-only-options.cs** - ✅ Deleted (redundant - functionality covered by routing-09, routing-12)
- **generator-04-static-service-injection.cs** - ✅ Rewritten to test functionality, added to CI
- **configuration-02-cli-overrides.cs** - ✅ Created and added to CI (7 tests for CLI config override filtering)
- **routing-08-end-of-options.cs** - ✅ Fixed bug #355, removed `UseDebugLogging()`, added to CI (4 tests)
- **routing-15-help-route-priority.cs** - ✅ Rewritten to use TestTerminal pattern (no closures), added to CI (4 pass, 1 skip for Task #356)
- **NURU_H002 suppression removed** - ✅ Closures in handlers are now errors (as designed), removed from `tests/Directory.Build.props`
- **generator-13-ioptions-parameter-injection.cs** - ✅ Added to CI (exclusion reason was outdated - test doesn't read generated files)
- **routing-20-version-route-override.cs** - ✅ Fixed Task #357 (user routes now emitted before built-ins), added to CI (2 tests)
- **routing-21-check-updates-version-comparison.cs** - ✅ Rewritten using TestTerminal pattern, added to CI
- **Task #358** - ✅ Fixed source generator chaining issues: replaced `[GeneratedRegex]` with runtime Regex, moved `CheckUpdatesGitHubRelease` and JSON context to core library
- **show-help-colors.cs** - ✅ Deleted (obsolete v1 demo using EndpointCollection, HelpProvider.GetHelpText - not a test)
- **message-type-02-help-output.cs** - ✅ Deleted (obsolete v1 API: EndpointCollection, PatternParser.Parse, HelpProvider.GetHelpText)
- **nuru-route-registry-01-basic.cs** - ✅ Fixed (IRequest → IMessage), added to CI (6 tests)

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

### Routing Tests (0 files excluded)

All routing tests are now in CI.

**Moved**: `dsl-example.cs` → `documentation/developer/design/dsl/fluent-api-example.cs` (design doc, not a test)

**Fixed**: routing-08 - removed `UseDebugLogging()`, fixed bug #355 (positional before `--`), added to CI

**Fixed**: routing-15 - removed `AddAutoHelp()` (now default), rewrote tests to use TestTerminal pattern (no closures), added to CI

**Fixed**: routing-20 - Fixed Task #357 (generator emits user routes before built-ins), added to CI

**Fixed**: routing-21 - Rewritten to use TestTerminal pattern, Task #358 fixed (source gen chaining)

### Configuration Tests (0 files excluded)

All configuration tests are now in CI.

**Rewritten**: configuration-01 - Removed obsolete `AddDependencyInjection()` API, rewrote to test lazy IOptions<T> evaluation (5 tests)

### Options Tests (0 files excluded)

All options tests are now in CI.

**Added**: options-01-mixed-required-optional (4 tests), options-02-optional-flag-optional-value (3 tests)

**Note**: Exclusion reason "Uses `CreateSlimBuilder()` API" was outdated - files didn't use that API

### Session Tests (0 files excluded)

**Renamed and moved**: `help-provider-03-session-context.cs` → `session/session-context-01-basic.cs` (5 tests)

Note: Original exclusion reason was wrong - file tests `SessionContext` class which still exists

### Root-level Tests (2 files excluded)

| File | Exclusion Reason |
|------|------------------|
| `capabilities-01-basic.cs` | Not included in any Compile directive |
| `capabilities-02-integration.cs` | Not included in any Compile directive |

**Added to CI**:
- `nuru-route-registry-01-basic.cs` - Fixed IRequest → IMessage (6 tests)

**Deleted**:
- `show-help-colors.cs` (obsolete v1 demo, not a test)
- `message-type-02-help-output.cs` (obsolete v1 API)

## What IS Included in CI

| Category | Pattern/Files |
|----------|---------------|
| Lexer | `lexer/*.cs` (all 15 files) |
| Parser | `parser/*.cs` (all 15 files) |
| Type Conversion | `type-conversion/*.cs` (1 file) |
| Generator | 4 specific files (01, 10, 11, 12) |
| Routing | All `routing/*.cs` files |
| Message Type | `message-type-01-fluent-api.cs` only |

## Summary by Category

| Category | Total Files | In CI | Excluded |
|----------|-------------|-------|----------|
| Generator | 6 | 6 | 0 |
| Routing | 18 | 18 | 0 |
| Configuration | 2 | 2 | 0 |
| Options | 2 | 2 | 0 |
| Session | 1 | 1 | 0 |
| Root-level | 4 | 2 | 2 |
| Lexer | 16 | 16 | 0 |
| Parser | 15 | 15 | 0 |
| Type Conversion | 1 | 1 | 0 |
| **Total** | **65** | **63** | **2** |

## Recommendations

### High Priority - API Migration Needed
None - all API migrations complete.

### Medium Priority - None
All extension API tests have been migrated to CI.

### Low Priority - None
All isolation-required tests have been migrated to CI.

### Consider Deletion
- `capabilities-01-basic.cs` / `capabilities-02-integration.cs` - Not included, status unclear

## References

- `tests/ci-tests/Directory.Build.props` - CI test configuration
- `tests/ci-tests/run-ci-tests.cs` - CI test runner entry point
