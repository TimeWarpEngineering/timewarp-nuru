// Handler validator that detects unsupported handler patterns.
//
// This validator checks for:
// - NURU_H001: Instance methods (not supported for code generation)
// - NURU_H002: Lambdas with closures (captured variables)
// - NURU_H003: Unsupported expression types
// - NURU_H004: Private methods not accessible from generated code
// - NURU_H006: Discard parameters in lambdas
//
// This replaces the syntax-level NuruHandlerAnalyzer with model-level validation
// that integrates with the unified NuruAnalyzer.

namespace TimeWarp.Nuru.Validation;

using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

/// <summary>
/// Validates handler expressions for unsupported patterns.
/// </summary>
internal static class HandlerValidator
{
  /// <summary>
  /// Validates a handler expression and returns any diagnostics.
  /// </summary>
  /// <param name="handlerExpression">The handler expression from .WithHandler() call.</param>
  /// <param name="semanticModel">Semantic model for symbol resolution.</param>
  /// <param name="location">Location for error reporting.</param>
  /// <returns>Diagnostics for any validation errors found.</returns>
  public static ImmutableArray<Diagnostic> Validate(
    ExpressionSyntax handlerExpression,
    SemanticModel semanticModel,
    Location location)
  {
    List<Diagnostic> diagnostics = [];

    switch (handlerExpression)
    {
      case ParenthesizedLambdaExpressionSyntax lambda:
        ValidateLambdaHandler(lambda, semanticModel, location, diagnostics);
        break;

      case SimpleLambdaExpressionSyntax lambda:
        ValidateLambdaHandler(lambda, semanticModel, location, diagnostics);
        break;

      case IdentifierNameSyntax identifier:
        ValidateMethodGroupHandler(identifier, semanticModel, location, diagnostics);
        break;

      case MemberAccessExpressionSyntax memberAccess:
        ValidateMemberAccessHandler(memberAccess, semanticModel, location, diagnostics);
        break;

      default:
        // Unsupported expression type
        diagnostics.Add(Diagnostic.Create(
          DiagnosticDescriptors.UnsupportedHandlerExpression,
          location,
          handlerExpression.Kind().ToString()));
        break;
    }

    return [.. diagnostics];
  }

  /// <summary>
  /// Validates a lambda handler expression.
  /// </summary>
  private static void ValidateLambdaHandler(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel,
    Location location,
    List<Diagnostic> diagnostics)
  {
    // Check for discard parameters ('_') which can't be referenced in generated code
    if (HasDiscardParameters(lambda))
    {
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.DiscardParameterNotSupported,
        location));
      return; // Don't report other errors if discards are present
    }

    // Check for closures (captured external variables)
    List<string> capturedVariables = DetectClosures(lambda, semanticModel);

    if (capturedVariables.Count > 0)
    {
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.ClosureNotAllowed,
        location,
        string.Join(", ", capturedVariables)));
    }
  }

  /// <summary>
  /// Checks if a lambda has any discard parameters ('_').
  /// </summary>
  private static bool HasDiscardParameters(LambdaExpressionSyntax lambda)
  {
    switch (lambda)
    {
      case SimpleLambdaExpressionSyntax simple:
        return simple.Parameter.Identifier.Text == "_";

      case ParenthesizedLambdaExpressionSyntax parenthesized:
        foreach (ParameterSyntax param in parenthesized.ParameterList.Parameters)
        {
          if (param.Identifier.Text == "_")
            return true;
        }

        return false;

      default:
        return false;
    }
  }

  /// <summary>
  /// Validates a method group handler (identifier reference).
  /// </summary>
  private static void ValidateMethodGroupHandler(
    IdentifierNameSyntax identifier,
    SemanticModel semanticModel,
    Location location,
    List<Diagnostic> diagnostics)
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(identifier);

    IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

    if (methodSymbol is null)
    {
      // Can't resolve - let the source generator handle it
      return;
    }

    // Check accessibility - private methods can't be called from generated code
    if (methodSymbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
    {
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.PrivateMethodNotAccessible,
        location,
        methodSymbol.Name));
    }
  }

  /// <summary>
  /// Validates a member access handler (e.g., obj.Method or Type.StaticMethod).
  /// </summary>
  private static void ValidateMemberAccessHandler(
    MemberAccessExpressionSyntax memberAccess,
    SemanticModel semanticModel,
    Location location,
    List<Diagnostic> diagnostics)
  {
    SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(memberAccess);

    IMethodSymbol? methodSymbol = symbolInfo.Symbol as IMethodSymbol
      ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();

    if (methodSymbol is null)
    {
      // Can't resolve - let the source generator handle it
      return;
    }

    if (!methodSymbol.IsStatic)
    {
      // Instance method - not supported
      string methodName = $"{memberAccess.Expression}.{memberAccess.Name}";
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.InstanceMethodNotSupported,
        location,
        methodName));
    }
    else if (methodSymbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
    {
      // Private static methods can't be called from generated code
      string methodName = $"{memberAccess.Expression}.{memberAccess.Name}";
      diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.PrivateMethodNotAccessible,
        location,
        methodName));
    }
  }

  /// <summary>
  /// Detects if a lambda captures variables from enclosing scope (closures).
  /// </summary>
  private static List<string> DetectClosures(
    LambdaExpressionSyntax lambda,
    SemanticModel semanticModel)
  {
    List<string> capturedVariables = [];

    // Get lambda parameters
    HashSet<string> lambdaParameters = GetLambdaParameterNames(lambda);

    // Track local variables declared inside the lambda
    HashSet<string> localVariables = [];
    if (lambda.Body is not null)
    {
      foreach (VariableDeclaratorSyntax declarator in lambda.Body.DescendantNodes()
        .OfType<VariableDeclaratorSyntax>())
      {
        localVariables.Add(declarator.Identifier.Text);
      }
    }

    // Walk lambda body looking for identifiers
    if (lambda.Body is null)
      return capturedVariables;

    foreach (IdentifierNameSyntax identifier in lambda.Body.DescendantNodes()
      .OfType<IdentifierNameSyntax>())
    {
      string name = identifier.Identifier.Text;

      // Skip if it's a parameter
      if (lambdaParameters.Contains(name))
        continue;

      // Skip if it's a local variable declared inside lambda
      if (localVariables.Contains(name))
        continue;

      // Skip if it's on the right side of a member access (obj.name - we care about 'obj', not 'name')
      if (identifier.Parent is MemberAccessExpressionSyntax ma && ma.Name == identifier)
        continue;

      // Skip if it's the name part of a conditional member access (obj?.name)
      if (identifier.Parent is MemberBindingExpressionSyntax mb && mb.Name == identifier)
        continue;

      // Skip if it's the target of a property assignment in an object initializer
      // e.g., new Foo { X = value } - X is not a closure, it's setting a property on the new object
      if (IsObjectInitializerTarget(identifier))
        continue;

      // Get symbol info
      SymbolInfo symbolInfo = semanticModel.GetSymbolInfo(identifier);
      ISymbol? symbol = symbolInfo.Symbol;

      if (symbol is null)
      {
        // Symbol resolution failed - be conservative and treat as closure
        capturedVariables.Add(name);
        continue;
      }

      // Check what kind of symbol it is
      switch (symbol)
      {
        case ILocalSymbol:
          // Local variable from outer scope - closure!
          capturedVariables.Add(name);
          break;

        case IParameterSymbol param:
          // If not our lambda's parameter, it's from outer method - closure!
          if (!lambdaParameters.Contains(param.Name))
          {
            capturedVariables.Add(name);
          }

          break;

        case IFieldSymbol field when !field.IsStatic:
          // Instance field access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        case IPropertySymbol prop when !prop.IsStatic:
          // Instance property access (via implicit 'this') - closure!
          capturedVariables.Add($"this.{name}");
          break;

        // Static members, types, namespaces, methods are OK
        case IMethodSymbol:
        case IFieldSymbol { IsStatic: true }:
        case IPropertySymbol { IsStatic: true }:
        case INamedTypeSymbol:
        case INamespaceSymbol:
          break;
      }
    }

    return capturedVariables;
  }

  /// <summary>
  /// Checks if an identifier is the target (left side) of a property assignment
  /// inside an object initializer. These are NOT closures - they're setting properties
  /// on the newly created object, not accessing properties from the enclosing scope.
  /// </summary>
  /// <example>
  /// new ComparisonResult { X = x, Y = y } - X and Y are object initializer targets
  /// </example>
  private static bool IsObjectInitializerTarget(IdentifierNameSyntax identifier)
  {
    // Check if parent is an assignment expression
    if (identifier.Parent is not AssignmentExpressionSyntax assignment)
      return false;

    // Check if the identifier is on the left side of the assignment
    if (assignment.Left != identifier)
      return false;

    // Check if the assignment is inside an object initializer
    // Walk up the parent chain looking for InitializerExpressionSyntax
    Microsoft.CodeAnalysis.SyntaxNode? current = assignment.Parent;
    while (current is not null)
    {
      if (current is InitializerExpressionSyntax initializer &&
          initializer.Kind() == SyntaxKind.ObjectInitializerExpression)
      {
        return true;
      }

      // Stop if we hit a statement or member declaration - we've left the expression
      if (current is StatementSyntax or MemberDeclarationSyntax)
        break;

      current = current.Parent;
    }

    return false;
  }

  /// <summary>
  /// Gets the parameter names from a lambda expression.
  /// </summary>
  private static HashSet<string> GetLambdaParameterNames(LambdaExpressionSyntax lambda)
  {
    HashSet<string> names = [];

    switch (lambda)
    {
      case SimpleLambdaExpressionSyntax simple:
        names.Add(simple.Parameter.Identifier.Text);
        break;

      case ParenthesizedLambdaExpressionSyntax parenthesized:
        foreach (ParameterSyntax param in parenthesized.ParameterList.Parameters)
        {
          names.Add(param.Identifier.Text);
        }

        break;
    }

    return names;
  }
}
