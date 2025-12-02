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
- [ ] Delete `timewarp-nuru-analyzers-tests.csproj`
- [ ] Create `Directory.Build.props` (standalone, not importing parent)
- [ ] Create `should-fail-map-generic-no-mediator.cs` - Uses `Map<T>()` without Mediator, should report NURU_D001
- [ ] Create `should-pass-map-generic-with-mediator.cs` - Uses `Map<T>()` with `#:package Mediator.Abstractions`, should NOT report
- [ ] Create `should-pass-map-non-generic.cs` - Uses non-generic `Map()`, should NOT report
- [ ] Verify each runfile behaves as expected

## Notes

The parent `tests/Directory.Build.props` automatically adds `Mediator.Abstractions` to all test projects. For analyzer tests, we need to NOT inherit this so we can test scenarios where Mediator is missing.

Runfile pattern from other tests:
```csharp
#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.SourceGenerator  // only for tests that need it
```

Existing files to consider:
- `test-analyzer-patterns.cs` - existing runfile for route pattern validation
- `test-samples.cs` - sample code (may need conversion or removal)
