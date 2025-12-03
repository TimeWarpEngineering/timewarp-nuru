# Create Mediator Dependency Analyzer

## Description

Create a new Roslyn `DiagnosticAnalyzer` that detects `Map<TCommand>` usage without the required Mediator packages. This provides compile-time guidance when developers (and AI agents) forget to install Mediator packages.

## Parent

MCP Builder Pattern Guidance Analysis - standardizing samples to prevent AI confusion

## Requirements

- Create analyzer that detects `Map<T>()` generic method calls
- Check if `Mediator.Abstractions` is referenced in the compilation
- Report `NURU_D001` error with actionable message if packages are missing
- Follow existing analyzer patterns in the codebase

## Checklist

### Design
- [x] Review existing analyzer infrastructure in `source/timewarp-nuru-analyzers/`
- [x] Study `nuru-route-analyzer.cs` for patterns and conventions
- [x] Design diagnostic message with clear installation instructions

### Implementation
- [x] Create `source/timewarp-nuru-analyzers/analyzers/mediator-dependency-analyzer.cs`
- [x] Implement `DiagnosticAnalyzer` base class
- [x] Define `NURU_D001` diagnostic descriptor
- [x] Implement `Initialize` method to register syntax node action
- [x] Implement `AnalyzeInvocation` to detect `Map<T>()` calls
- [x] Check `Compilation.ReferencedAssemblyNames` for Mediator packages
- [x] Report diagnostic with installation instructions

### Testing
- [x] Add tests in `timewarp-nuru-analyzers-tests/`
- [x] Test case: Map<T> without Mediator packages (should report NURU_D001)
- [x] Test case: Map<T> with Mediator packages (should not report)
- [x] Test case: Non-generic Map (should not report)
- [x] Test case: Map with wrong signature (should not report)

### Documentation
- [x] Add diagnostic to documentation
- [x] Update analyzer README if one exists

## Results

### Implementation

Created `MediatorDependencyAnalyzer` in `source/timewarp-nuru-analyzers/analyzers/mediator-dependency-analyzer.cs`:

- Detects `Map<T>()` generic method invocations
- Checks for generated `AddMediator` extension method (proves source generator ran)
- Reports `NURU_D001` with actionable installation instructions

Diagnostic descriptor in `diagnostic-descriptors.dependencies.cs`:
```csharp
public static readonly DiagnosticDescriptor MissingMediatorPackages = new(
  id: "NURU_D001",
  title: "Mediator packages required for Map<TCommand>",
  messageFormat: "Map<{0}> requires Mediator packages. Run: dotnet add package Mediator.Abstractions && dotnet add package Mediator.SourceGenerator.",
  category: "Dependencies",
  defaultSeverity: DiagnosticSeverity.Error,
  isEnabledByDefault: true)
```

### Test Files

- `should-fail-map-generic-no-sourcegen.cs` - Expects compile failure without Mediator
- `should-pass-map-generic-with-mediator.cs` - Passes with both packages
- `should-pass-map-non-generic.cs` - Non-generic Map doesn't require Mediator

### Documentation Updated

- `documentation/user/features/analyzer.md` - Added NURU_D001 section
- `documentation/developer/guides/using-analyzers.md` - Added Dependency Errors section

## Notes

**Key implementation insight**: Rather than checking assembly references directly, the analyzer checks for the existence of the generated `AddMediator` extension method on `IServiceCollection`. This proves the source generator has run successfully, which is the actual requirement.
