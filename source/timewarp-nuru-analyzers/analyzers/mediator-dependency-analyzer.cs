namespace TimeWarp.Nuru;

/// <summary>
/// Analyzes Map&lt;TCommand&gt; usage and reports an error if Mediator.SourceGenerator is not referenced.
/// The source generator must be DIRECTLY referenced (not transitive) to generate AddMediator().
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MediatorDependencyAnalyzer : DiagnosticAnalyzer
{
  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
    [DiagnosticDescriptors.MissingMediatorPackages];

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

    // Check if this is a generic method invocation (e.g., builder.Map<TCommand>())
    if (!IsGenericMapInvocation(invocation))
      return;

    // Check if Mediator.SourceGenerator has run (generates AddMediator extension method)
    if (HasMediatorGeneratedSource(context.Compilation))
      return;

    // Get the type argument name for the diagnostic message
    string typeArgumentName = GetTypeArgumentName(invocation);

    // Report the diagnostic
    Diagnostic diagnostic = Diagnostic.Create(
      DiagnosticDescriptors.MissingMediatorPackages,
      invocation.GetLocation(),
      typeArgumentName);

    context.ReportDiagnostic(diagnostic);
  }

  private static bool IsGenericMapInvocation(InvocationExpressionSyntax invocation)
  {
    // Check for member access pattern: something.Map<T>()
    if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
    {
      // Check if the method name is "Map" with generic type arguments
      if (memberAccess.Name is GenericNameSyntax genericName &&
          genericName.Identifier.Text == "Map" &&
          genericName.TypeArgumentList.Arguments.Count == 1)
      {
        return true;
      }
    }

    // Check for simple invocation pattern: Map<T>() (less common but possible)
    if (invocation.Expression is GenericNameSyntax simpleGenericName &&
        simpleGenericName.Identifier.Text == "Map" &&
        simpleGenericName.TypeArgumentList.Arguments.Count == 1)
    {
      return true;
    }

    return false;
  }

  /// <summary>
  /// Checks if Mediator.SourceGenerator has produced the AddMediator extension method.
  /// This method is generated when the source generator runs successfully.
  /// </summary>
  private static bool HasMediatorGeneratedSource(Compilation compilation)
  {
    // Mediator.SourceGenerator generates an AddMediator extension method on IServiceCollection.
    // If this method exists, the source generator has run successfully.
    IEnumerable<ISymbol> symbols = compilation.GetSymbolsWithName("AddMediator", SymbolFilter.Member);

    foreach (ISymbol symbol in symbols)
    {
      // Check if it's a method (the extension method we're looking for)
      if (symbol is IMethodSymbol method)
      {
        // Verify it's an extension method on IServiceCollection
        if (method.IsExtensionMethod &&
            method.Parameters.Length > 0 &&
            method.Parameters[0].Type.Name == "IServiceCollection")
        {
          return true;
        }
      }
    }

    return false;
  }

  private static string GetTypeArgumentName(InvocationExpressionSyntax invocation)
  {
    GenericNameSyntax? genericName = invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name as GenericNameSyntax,
      GenericNameSyntax simple => simple,
      _ => null
    };

    if (genericName?.TypeArgumentList.Arguments.Count > 0)
    {
      return genericName.TypeArgumentList.Arguments[0].ToString();
    }

    return "TCommand";
  }
}
