namespace TimeWarp.Nuru;

/// <summary>
/// Analyzer that validates handler expressions in .WithHandler() calls.
/// Reports errors for unsupported handler types:
/// - NURU_H001: Instance methods
/// - NURU_H002: Lambdas with closures
/// - NURU_H003: Unsupported expression types
/// - NURU_H004: Private methods not accessible
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NuruHandlerAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
  [
    DiagnosticDescriptors.InstanceMethodNotSupported,
    DiagnosticDescriptors.ClosureNotAllowed,
    DiagnosticDescriptors.UnsupportedHandlerExpression,
    DiagnosticDescriptors.PrivateMethodNotAccessible
  ];

  public override void Initialize(AnalysisContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();
    context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
  }

  private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
  {
    if (context.Node is not InvocationExpressionSyntax invocation)
      return;

    // Check if this is a .WithHandler(...) call
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return;

    if (memberAccess.Name.Identifier.Text != "WithHandler")
      return;

    // Get the handler argument
    if (invocation.ArgumentList.Arguments.Count == 0)
      return;

    ExpressionSyntax handlerExpression = invocation.ArgumentList.Arguments[0].Expression;

    // Validate the handler expression
    ValidateHandlerExpression(context, handlerExpression);
  }

  private static void ValidateHandlerExpression(
    SyntaxNodeAnalysisContext context,
    ExpressionSyntax handlerExpression)
  {
    switch (handlerExpression)
    {
      case ParenthesizedLambdaExpressionSyntax lambda:
        ValidateLambdaHandler(context, lambda);
        break;

      case SimpleLambdaExpressionSyntax lambda:
        ValidateLambdaHandler(context, lambda);
        break;

      case IdentifierNameSyntax identifier:
        ValidateMethodGroupHandler(context, identifier);
        break;

      case MemberAccessExpressionSyntax memberAccess:
        ValidateMemberAccessHandler(context, memberAccess);
        break;

      default:
        // Unsupported expression type
        context.ReportDiagnostic(
          Diagnostic.Create(
            DiagnosticDescriptors.UnsupportedHandlerExpression,
            handlerExpression.GetLocation(),
            handlerExpression.Kind().ToString()));
        break;
    }
  }

  private static void ValidateLambdaHandler(
    SyntaxNodeAnalysisContext context,
    LambdaExpressionSyntax lambda)
  {
    // Check for closures (captured external variables)
    List<string> capturedVariables = DetectClosures(lambda, context.SemanticModel);

    if (capturedVariables.Count > 0)
    {
      context.ReportDiagnostic(
        Diagnostic.Create(
          DiagnosticDescriptors.ClosureNotAllowed,
          lambda.GetLocation(),
          string.Join(", ", capturedVariables)));
    }
  }

  private static void ValidateMethodGroupHandler(
    SyntaxNodeAnalysisContext context,
    IdentifierNameSyntax identifier)
  {
    // Get the method symbol
    SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken);

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
      context.ReportDiagnostic(
        Diagnostic.Create(
          DiagnosticDescriptors.PrivateMethodNotAccessible,
          identifier.GetLocation(),
          methodSymbol.Name));
    }
  }

  private static void ValidateMemberAccessHandler(
    SyntaxNodeAnalysisContext context,
    MemberAccessExpressionSyntax memberAccess)
  {
    // Get the method symbol to determine if it's static or instance
    SymbolInfo symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken);

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
      context.ReportDiagnostic(
        Diagnostic.Create(
          DiagnosticDescriptors.InstanceMethodNotSupported,
          memberAccess.GetLocation(),
          methodName));
    }
    else if (methodSymbol.DeclaredAccessibility is Accessibility.Private or Accessibility.Protected)
    {
      // Private static methods can't be called from generated code
      string methodName = $"{memberAccess.Expression}.{memberAccess.Name}";
      context.ReportDiagnostic(
        Diagnostic.Create(
          DiagnosticDescriptors.PrivateMethodNotAccessible,
          memberAccess.GetLocation(),
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
      if (identifier.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Name == identifier)
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
        foreach (Microsoft.CodeAnalysis.CSharp.Syntax.ParameterSyntax param in parenthesized.ParameterList.Parameters)
        {
          names.Add(param.Identifier.Text);
        }

        break;
    }

    return names;
  }
}
