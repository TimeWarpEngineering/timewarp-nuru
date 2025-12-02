namespace TimeWarp.Nuru;

/// <summary>
/// Analyzes Map&lt;TCommand&gt; usage and reports an error if Mediator packages are not referenced.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class MediatorDependencyAnalyzer : DiagnosticAnalyzer
{
  // The Mediator.Abstractions NuGet package produces an assembly named "Mediator"
  private const string MediatorAssemblyName = "Mediator";

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

    // Check if Mediator is referenced (from Mediator.Abstractions package)
    if (HasMediatorReference(context.Compilation))
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

  private static bool HasMediatorReference(Compilation compilation)
  {
    foreach (AssemblyIdentity assembly in compilation.ReferencedAssemblyNames)
    {
      if (string.Equals(assembly.Name, MediatorAssemblyName, StringComparison.OrdinalIgnoreCase))
      {
        return true;
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
