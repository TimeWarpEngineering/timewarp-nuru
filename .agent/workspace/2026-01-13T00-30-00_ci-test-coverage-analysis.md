# CI Test Coverage Analysis

## Executive Summary

Analysis of tests in `tests/timewarp-nuru-core-tests` that are NOT included in the CI test runner (`tests/ci-tests/run-ci-tests.cs`). **All test files are now in CI.** CI has 534 tests (527 pass, 6 fail, 1 skip).

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
- **nuru-route-registry-01-basic.cs** - ✅ Deleted (dead code - NuruRouteRegistry was never integrated into runtime)
- **NuruRouteRegistry + RegisteredRoute** - ✅ Deleted from source (dead code - generator uses InvokerRegistry instead)
- **capabilities-01-basic.cs** - ✅ Moved to `capabilities/` folder, added to CI (7 tests pass - serialization)
- **capabilities-02-integration.cs** - ✅ Moved to `capabilities/` folder, added to CI (3 pass, 6 fail - route not implemented, task #157)

## Blocking Issues

**6 failing tests** in `CapabilitiesIntegration` block shipping until task #157 is complete:
- Tests require `--capabilities` route which is not yet implemented
- Route infrastructure exists (`DisableCapabilitiesRoute` option, response models) but route registration missing

## Scope

- **Analyzed**: All `.cs` files in `tests/timewarp-nuru-core-tests/`
- **Reference**: `tests/ci-tests/Directory.Build.props` which defines what's compiled into CI

## Tests NOT in CI

**None** - all test files are now in CI.

## What IS Included in CI

| Category | Pattern/Files |
|----------|---------------|
| Lexer | `lexer/*.cs` (16 files) |
| Parser | `parser/*.cs` (15 files) |
| Type Conversion | `type-conversion/*.cs` (1 file) |
| Generator | 7 specific files (01, 04, 10, 11, 12, 13, 14) |
| Routing | All `routing/*.cs` files (18 files) |
| Message Type | `message-type-01-fluent-api.cs` |
| Configuration | `configuration/*.cs` (2 files) |
| Options | `options/*.cs` (2 files) |
| Session | `session/*.cs` (1 file) |
| Capabilities | `capabilities/*.cs` (2 files) |

## Summary by Category

| Category | Total Files | In CI | Excluded |
|----------|-------------|-------|----------|
| Generator | 7 | 7 | 0 |
| Routing | 18 | 18 | 0 |
| Configuration | 2 | 2 | 0 |
| Options | 2 | 2 | 0 |
| Session | 1 | 1 | 0 |
| Capabilities | 2 | 2 | 0 |
| Lexer | 16 | 16 | 0 |
| Parser | 15 | 15 | 0 |
| Type Conversion | 1 | 1 | 0 |
| Root-level | 1 | 1 | 0 |
| **Total** | **65** | **65** | **0** |

## CI Test Results

- **Total**: 534 tests
- **Passed**: 527
- **Failed**: 6 (CapabilitiesIntegration - task #157)
- **Skipped**: 1 (HelpRoutePriority - task #356)

## Deleted Files (Dead Code)

- `source/timewarp-nuru-core/nuru-route-registry.cs` - Never integrated
- `source/timewarp-nuru-core/registered-route.cs` - Only used by NuruRouteRegistry
- `tests/timewarp-nuru-core-tests/nuru-route-registry-01-basic.cs` - Tested dead code
- `tests/timewarp-nuru-core-tests/show-help-colors.cs` - Obsolete v1 demo
- `tests/timewarp-nuru-core-tests/message-type-02-help-output.cs` - Obsolete v1 API

## References

- `tests/ci-tests/Directory.Build.props` - CI test configuration
- `tests/ci-tests/run-ci-tests.cs` - CI test runner entry point
- `kanban/in-progress/157-add-capabilities-flag-for-ai-tool-discovery.md` - Blocking task
