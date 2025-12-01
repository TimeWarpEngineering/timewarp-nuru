# Hard Deprecate NuruAppBuilder Constructors - Force Factory Method Usage

## Description

Make `NuruAppBuilder()` and `NuruCoreAppBuilder()` constructors internal to force usage of factory methods:
- `NuruApp.CreateBuilder(args)` - Full-featured (DI, Config, Telemetry, REPL, Completion)
- `NuruCoreApp.CreateSlimBuilder(args)` - Minimal (auto-help only)
- `NuruCoreApp.CreateEmptyBuilder(args)` - Bare minimum (testing)

**Breaking Change:** Yes - users must migrate to factory methods

## Requirements

- All samples must use factory methods before this change (prerequisite: task 204)
- Tests should continue working via InternalsVisibleTo
- Documentation must be updated to reflect the required usage pattern

## Checklist

### Prerequisites
- [ ] Complete task 204 (standardize samples to use CreateBuilder factory methods)

### Implementation
- [ ] Change `public NuruAppBuilder()` to `internal NuruAppBuilder()` in `nuru-app-builder.cs`
- [ ] Change `public NuruCoreAppBuilder()` to `internal NuruCoreAppBuilder()` in `nuru-core-app-builder.factory.cs`
- [ ] Verify tests still compile (InternalsVisibleTo already configured)
- [ ] Run full test suite

### Documentation
- [ ] Update user documentation examples to use factory methods
- [ ] Add migration guide for the breaking change
- [ ] Update changelog with breaking change notice

## Notes

- Reference: `.agent/workspace/2025-12-01T21-00-00_hard-deprecation-implementation-plan.md`
- Tests already have InternalsVisibleTo configured, so no test changes should be needed
- This enforces the intended API surface and ensures users get properly configured builders

## Tags

breaking-change, api, refactoring
