# Remove all CreateSlimBuilder and CreateEmptyBuilder references from codebase

## Results

**COMPLETE** - Removed all 52 references to `CreateSlimBuilder` and `CreateEmptyBuilder`.

Key finding: These methods **never existed** as actual implementations - they were only 
referenced in comments and test code. Tests calling these methods were failing to compile.

### Files Updated (13 files)
- 8 test files in `tests/timewarp-nuru-core-tests/`
- 3 test files in `tests/timewarp-nuru-analyzers-tests/`
- 1 source file (doc comments only)
- 1 benchmark file (comment only)

### Migration Applied
- `NuruCoreApp.CreateSlimBuilder()` → `NuruApp.CreateBuilder([])`
- `NuruCoreApp.CreateSlimBuilder([])` → `NuruApp.CreateBuilder([])`
- `NuruApp.CreateSlimBuilder(args)` → `NuruApp.CreateBuilder(args)`
- Doc comment `<see cref>` updated to reference `NuruApp.CreateBuilder`

### Test Adjustments
- `capabilities-02-integration.cs`: Consolidated duplicate tests, updated to use 
  `NuruAppOptions.DisableCapabilitiesRoute` instead of non-existent "slim builder" concept

## Description

Remove all references to `CreateSlimBuilder` and `CreateEmptyBuilder` from `.cs` files,
including code usage, comments, and triple-slash documentation. These obsolete factory
methods have been replaced by `NuruApp.CreateBuilder()`.

## Affected Files (52 matches)

### Tests - Core (needs migration to CreateBuilder or NuruApp.CreateBuilder)
- [x] `tests/timewarp-nuru-core-tests/routing/routing-23-multiple-map-same-handler.cs` (2 refs)
- [x] `tests/timewarp-nuru-core-tests/options/options-01-mixed-required-optional.cs` (4 refs)
- [x] `tests/timewarp-nuru-core-tests/options/options-02-optional-flag-optional-value.cs` (3 refs)
- [x] `tests/timewarp-nuru-core-tests/options/options-03-nuru-context.cs` (5 refs)
- [x] `tests/timewarp-nuru-core-tests/test-terminal-context-01-basic.cs` (2 refs)
- [x] `tests/timewarp-nuru-core-tests/help-provider-03-session-context.cs` (4 refs)
- [x] `tests/timewarp-nuru-core-tests/capabilities-02-integration.cs` (4 refs)
- [x] `tests/timewarp-nuru-core-tests/nested-compiled-route-builder-01-basic.cs` (1 ref)

### Tests - Analyzers (needs migration)
- [x] `tests/timewarp-nuru-analyzers-tests/manual/should-pass-map-non-generic.cs` (1 ref)
- [x] `tests/timewarp-nuru-analyzers-tests/auto/nuru-invoker-generator-01-basic.cs` (6 refs)
- [x] `tests/timewarp-nuru-analyzers-tests/auto/delegate-signature-01-models.cs` (16 refs)

### Source - Documentation only
- [x] `source/timewarp-nuru-core/builders/nuru-core-app-builder/nuru-core-app-builder.factory.cs` (3 refs - doc comments)

### Benchmarks
- [x] `benchmarks/timewarp-nuru-benchmarks/commands/nuru-direct-command.cs` (1 ref - comment)

## Checklist

- [x] Update all test files to use `NuruApp.CreateBuilder(args)` or appropriate builder
- [x] Update doc comments in source files
- [x] Update benchmark comments
- [x] Search for any remaining references (none found)

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
