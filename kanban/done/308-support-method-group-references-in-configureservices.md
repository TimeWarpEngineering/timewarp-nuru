# Support method group references in ConfigureServices

## Summary

The `ServiceExtractor` does not analyze method group references passed to `ConfigureServices()`. When a user writes:

```csharp
.ConfigureServices(ConfigureServices)
...
static void ConfigureServices(IServiceCollection services)
{
  services.AddSingleton<IScientificCalculator, ScientificCalculator>();
}
```

The generator fails to discover `IScientificCalculator` as a registered service, causing attributed route handlers that depend on it to get `default!` instead of proper service resolution.

## Root Cause

In `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs` lines 42-46:

```csharp
// Handle method group references
if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
{
  // Can't easily analyze method group without more complex flow analysis
  return [];
}
```

The extractor explicitly gives up on method groups and returns an empty array.

## Solution

When the `configureExpression` is an `IdentifierNameSyntax` or `MemberAccessExpressionSyntax`:

1. Use `SemanticModel.GetSymbolInfo()` to resolve it to an `IMethodSymbol`
2. Get the method's declaring syntax via `IMethodSymbol.DeclaringSyntaxReferences`
3. Extract the method body (either `BlockSyntax` or `ExpressionSyntax`)
4. Reuse the existing `ExtractFromLambda` logic (or factor it out to `ExtractFromBody`)

## Implementation Details

**File:** `source/timewarp-nuru-analyzers/generators/extractors/service-extractor.cs`

**Changes:**

1. Replace the early return at lines 42-46 with method resolution:

```csharp
// Handle method group references
if (configureExpression is IdentifierNameSyntax or MemberAccessExpressionSyntax)
{
  return ExtractFromMethodGroup(configureExpression, semanticModel, cancellationToken);
}
```

2. Add new method `ExtractFromMethodGroup`:

```csharp
/// <summary>
/// Extracts services from a method group reference (e.g., ConfigureServices(MyMethod)).
/// </summary>
private static ImmutableArray<ServiceDefinition> ExtractFromMethodGroup
(
  ExpressionSyntax methodGroupExpression,
  SemanticModel semanticModel,
  CancellationToken cancellationToken
)
{
  // Resolve the method symbol
  SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(methodGroupExpression, cancellationToken);
  
  if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
  {
    // Try candidate symbols (overload resolution may not be complete)
    methodSymbol = symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
  }
  
  if (methodSymbol is null)
    return [];
  
  // Get the method's syntax declaration
  SyntaxReference? syntaxRef = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
  if (syntaxRef is null)
    return [];
  
  SyntaxNode? methodSyntax = syntaxRef.GetSyntax(cancellationToken);
  
  // Extract the body based on method syntax type
  CSharpSyntaxNode? body = methodSyntax switch
  {
    MethodDeclarationSyntax method => (CSharpSyntaxNode?)method.Body ?? method.ExpressionBody?.Expression,
    LocalFunctionStatementSyntax localFunc => (CSharpSyntaxNode?)localFunc.Body ?? localFunc.ExpressionBody?.Expression,
    _ => null
  };
  
  if (body is null)
    return [];
  
  return ExtractFromBody(body, semanticModel, cancellationToken);
}
```

3. Refactor `ExtractFromLambda` to extract a shared `ExtractFromBody` method:

```csharp
/// <summary>
/// Extracts services from a method body (block or expression).
/// </summary>
private static ImmutableArray<ServiceDefinition> ExtractFromBody
(
  CSharpSyntaxNode body,
  SemanticModel semanticModel,
  CancellationToken cancellationToken
)
{
  ImmutableArray<ServiceDefinition>.Builder services = ImmutableArray.CreateBuilder<ServiceDefinition>();

  // Handle expression body
  if (body is ExpressionSyntax expression)
  {
    ServiceDefinition? service = ExtractFromExpression(expression, semanticModel, cancellationToken);
    if (service is not null)
      services.Add(service);

    return services.ToImmutable();
  }

  // Handle block body
  if (body is BlockSyntax block)
  {
    foreach (StatementSyntax statement in block.Statements)
    {
      if (statement is ExpressionStatementSyntax expressionStatement)
      {
        ServiceDefinition? service = ExtractFromExpression(expressionStatement.Expression, semanticModel, cancellationToken);
        if (service is not null)
          services.Add(service);
      }
    }
  }

  return services.ToImmutable();
}

/// <summary>
/// Extracts services from a lambda body.
/// </summary>
private static ImmutableArray<ServiceDefinition> ExtractFromLambda
(
  LambdaExpressionSyntax lambda,
  SemanticModel semanticModel,
  CancellationToken cancellationToken
)
{
  return ExtractFromBody(lambda.Body, semanticModel, cancellationToken);
}
```

## Semantic Model Consideration

The method body may be in the same compilation unit, so the existing `SemanticModel` should work. However, if the method is in a different file, we may need to get the semantic model for that syntax tree:

```csharp
// Get semantic model for the method's syntax tree (may be different from current)
SemanticModel? methodSemanticModel = semanticModel.Compilation.GetSemanticModel(methodSyntax.SyntaxTree);
```

## Checklist

- [ ] Add `ExtractFromMethodGroup` method to `ServiceExtractor`
- [ ] Refactor to share `ExtractFromBody` between lambda and method group extraction
- [ ] Handle both `MethodDeclarationSyntax` and `LocalFunctionStatementSyntax`
- [ ] Handle cross-file semantic model lookup if needed
- [ ] Add test: `ConfigureServices` with inline lambda (existing behavior)
- [ ] Add test: `ConfigureServices` with static method reference
- [ ] Add test: `ConfigureServices` with local function reference
- [ ] Verify `03-calc-mixed.cs` works without workarounds

## Notes

- Discovered during Task #304 Phase 10 testing
- The same pattern may apply to other callbacks that accept method groups
- This is a common C# pattern that users expect to work
- Blocks Task #304 Phase 10 completion for `03-calc-mixed.cs`
