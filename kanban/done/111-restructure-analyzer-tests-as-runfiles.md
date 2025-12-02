# Restructure Analyzer Tests as Runfiles

## Description

Convert `tests/timewarp-nuru-analyzers-tests/` from a csproj-based project to runfile-based manual tests. This enables proper testing of the `MediatorDependencyAnalyzer` (NURU_D001) which requires testing both WITH and WITHOUT `Mediator.Abstractions` referenced.

## Parent

109-create-mediator-dependency-analyzer

## Requirements

- Remove the `.csproj` file
- Create standalone `Directory.Build.props` that does NOT import parent (to avoid automatic Mediator.Abstractions reference)
- Attach analyzer via project reference in Directory.Build.props
- Create clearly named runfiles for manual testing

## Checklist

### Implementation
- [x] Delete `timewarp-nuru-analyzers-tests.csproj`
- [x] Create `Directory.Build.props` (standalone, not importing parent)
- [x] Create `demo-missing-sourcegen-error.cs` - Demonstrates what happens without Mediator.SourceGenerator
- [x] Create `should-pass-map-generic-with-mediator.cs` - Uses `Map<T>()` with `#:package Mediator.SourceGenerator`, should compile and run
- [x] Create `should-pass-map-non-generic.cs` - Uses non-generic `Map()`, should compile and run
- [x] Remove `test-samples.cs` (obsolete class-based file)
- [x] Verify each runfile behaves as expected

## Implementation Notes

### NURU_D001 Limitation Discovered

The original task assumed NURU_D001 could be tested by not importing the parent `Directory.Build.props`. However, the actual limitation is more fundamental:

1. **Library design constraint**: Both `timewarp-nuru` and `timewarp-nuru-core` directly reference `Mediator.Abstractions` because the `Map<TCommand>` generic constraint requires `where TCommand : IRequest`.

2. **Transitive reference**: Any project referencing our library gets `Mediator.Abstractions` transitively. The analyzer checks `compilation.ReferencedAssemblyNames` which includes transitive references.

3. **Source generator != assembly**: The real user error is forgetting to add `Mediator.SourceGenerator` directly. Source generators are loaded as analyzers, not as referenced assemblies, so the current analyzer implementation cannot detect their absence.

### What the tests demonstrate

- `should-pass-map-non-generic.cs` - Delegate-based routing works without any Mediator setup
- `should-pass-map-generic-with-mediator.cs` - Generic Map works with both packages directly referenced
- `demo-missing-sourcegen-error.cs` - Shows the compile error when `Mediator.SourceGenerator` is missing (CS1061: AddMediator not found)
- `test-analyzer-patterns.cs` - Existing runfile for route pattern validation (NURU001-NURU009)

### Existing Files
- `test-analyzer-patterns.cs` - Kept as-is, tests route pattern validation

## Notes

The parent `tests/Directory.Build.props` automatically adds `Mediator.Abstractions` to all test projects. The standalone `Directory.Build.props` avoids this, but Mediator is still available transitively through the library itself.

For true NURU_D001 testing, consider:
1. Using Roslyn analyzer unit test infrastructure (`Microsoft.CodeAnalysis.CSharp.Testing`)
2. Or modifying the analyzer to check for source generator presence instead of assembly presence
