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
- [ ] Review existing analyzer infrastructure in `source/timewarp-nuru-analyzers/`
- [ ] Study `nuru-route-analyzer.cs` for patterns and conventions
- [ ] Design diagnostic message with clear installation instructions

### Implementation
- [ ] Create `source/timewarp-nuru-analyzers/analyzers/mediator-dependency-analyzer.cs`
- [ ] Implement `DiagnosticAnalyzer` base class
- [ ] Define `NURU_D001` diagnostic descriptor
- [ ] Implement `Initialize` method to register syntax node action
- [ ] Implement `AnalyzeInvocation` to detect `Map<T>()` calls
- [ ] Check `Compilation.ReferencedAssemblyNames` for Mediator packages
- [ ] Report diagnostic with installation instructions

### Testing
- [ ] Add tests in `timewarp-nuru-analyzers-tests/`
- [ ] Test case: Map<T> without Mediator packages (should report NURU_D001)
- [ ] Test case: Map<T> with Mediator packages (should not report)
- [ ] Test case: Non-generic Map (should not report)
- [ ] Test case: Map with wrong signature (should not report)

### Documentation
- [ ] Add diagnostic to documentation
- [ ] Update analyzer README if one exists

## Notes

**Diagnostic definition:**

```csharp
public static readonly DiagnosticDescriptor MissingMediatorPackages = new(
  id: "NURU_D001",
  title: "Mediator packages required for Map<TCommand>",
  messageFormat: "Map<{0}> requires Mediator packages. Install: dotnet add package Mediator.Abstractions && dotnet add package Mediator.SourceGenerator, then call services.AddMediator(options => {{ options.PipelineBehaviors = [...]; }})",
  category: "Dependencies",
  defaultSeverity: DiagnosticSeverity.Error,
  isEnabledByDefault: true,
  description: "The Map<TCommand> pattern requires Mediator.Abstractions and Mediator.SourceGenerator packages. The source generator creates AddMediator() in your assembly.");
```

**Key implementation details:**

1. Register `SyntaxNodeAction` for `InvocationExpression`
2. Check if invocation is `builder.Map<T>()` pattern (generic method with one type argument)
3. Check `context.Compilation.ReferencedAssemblyNames` for `Mediator.Abstractions`
4. Report diagnostic at the invocation location if missing

File to create:
- `source/timewarp-nuru-analyzers/analyzers/mediator-dependency-analyzer.cs`

Reference files:
- `source/timewarp-nuru-analyzers/analyzers/nuru-route-analyzer.cs` (existing analyzer)
- `source/timewarp-nuru-analyzers/diagnostics/` (diagnostic infrastructure)

Reference analysis: `.agent/workspace/2025-12-01T21-15-00_mcp-builder-pattern-guidance-analysis.md`
