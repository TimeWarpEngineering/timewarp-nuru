# Remove all CreateSlimBuilder and CreateEmptyBuilder references from codebase

## Description

Remove all references to `CreateSlimBuilder` and `CreateEmptyBuilder` from `.cs` files,
including code usage, comments, and triple-slash documentation. These obsolete factory
methods have been replaced by `NuruApp.CreateBuilder()`.

## Affected Files (52 matches)

### Tests - Core (needs migration to CreateBuilder or NuruApp.CreateBuilder)
- [ ] `tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs` (2 refs)
- [ ] `tests/timewarp-nuru-core-tests/options/options-01-mixed-required-optional.cs` (4 refs)
- [ ] `tests/timewarp-nuru-core-tests/options/options-02-optional-flag-optional-value.cs` (3 refs)
- [ ] `tests/timewarp-nuru-core-tests/options/options-03-nuru-context.cs` (5 refs)
- [ ] `tests/timewarp-nuru-core-tests/test-terminal-context-01-basic.cs` (2 refs)
- [ ] `tests/timewarp-nuru-core-tests/help-provider-03-session-context.cs` (4 refs)
- [ ] `tests/timewarp-nuru-core-tests/capabilities-02-integration.cs` (4 refs)
- [ ] `tests/timewarp-nuru-core-tests/nested-compiled-route-builder-01-basic.cs` (1 ref)

### Tests - Analyzers (needs migration)
- [ ] `tests/timewarp-nuru-analyzers-tests/manual/should-pass-map-non-generic.cs` (1 ref)
- [ ] `tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs` (6 refs)
- [ ] `tests/timewarp-nuru-analyzers-tests/auto/delegate-signature-01-models.cs` (16 refs)

### Source - Documentation only
- [ ] `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.factory.cs` (3 refs - doc comments)

### Benchmarks
- [ ] `benchmarks/timewarp-nuru-benchmarks/commands/nuru-direct-command.cs` (1 ref - comment)

## Checklist

- [ ] Update all test files to use `NuruApp.CreateBuilder(args)` or appropriate builder
- [ ] Update doc comments in source files
- [ ] Update benchmark comments
- [ ] Verify all tests pass after migration
- [ ] Search for any remaining references

## Notes

### Migration Pattern
Replace:
```csharp
NuruCoreApp.CreateSlimBuilder()
NuruCoreApp.CreateEmptyBuilder()
NuruApp.CreateSlimBuilder(args)
```

With:
```csharp
NuruApp.CreateBuilder(args)
```

### Related
- Follows completion of #312 (Update samples to use NuruApp.CreateBuilder)
