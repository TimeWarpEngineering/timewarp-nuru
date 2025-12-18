namespace TimeWarp.Nuru;

/// <summary>
/// Analyzer that validates [NuruRoute] patterns on attributed route classes.
/// Reports NURU_A001 if a pattern contains multiple literals (spaces).
/// Reports NURU_A002 if multiple parameters exist without explicit Order.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NuruAttributedRoutePatternAnalyzer : DiagnosticAnalyzer
{
  private const string NuruRouteAttributeName = "TimeWarp.Nuru.NuruRouteAttribute";
  private const string ParameterAttributeName = "TimeWarp.Nuru.ParameterAttribute";

  public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
  [
    DiagnosticDescriptors.MultiWordPatternRequiresGroup,
    DiagnosticDescriptors.MultipleParametersRequireOrder
  ];

  public override void Initialize(AnalysisContext context)
  {
    ArgumentNullException.ThrowIfNull(context);

    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    context.EnableConcurrentExecution();

    context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
  }

  private static void AnalyzeNamedType(SymbolAnalysisContext context)
  {
    if (context.Symbol is not INamedTypeSymbol namedType)
      return;

    // Find [NuruRoute] attribute
    AttributeData? routeAttribute = namedType.GetAttributes()
      .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == NuruRouteAttributeName);

    if (routeAttribute is null)
      return;

    // Check for multi-word pattern (NURU_A001)
    CheckMultiWordPattern(context, routeAttribute);

    // Check for multiple parameters without Order (NURU_A002)
    CheckParameterOrdering(context, namedType);
  }

  private static void CheckMultiWordPattern(SymbolAnalysisContext context, AttributeData routeAttribute)
  {
    // Extract pattern from constructor argument
    if (routeAttribute.ConstructorArguments.Length == 0)
      return;

    string? pattern = routeAttribute.ConstructorArguments[0].Value?.ToString();

    if (string.IsNullOrEmpty(pattern))
      return;

    // Check if pattern contains spaces (multiple literals)
    if (pattern.Contains(' ', StringComparison.Ordinal))
    {
      // Get the location of the attribute for the diagnostic
      Location? location = routeAttribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();

      if (location is not null)
      {
        Diagnostic diagnostic = Diagnostic.Create(
          DiagnosticDescriptors.MultiWordPatternRequiresGroup,
          location,
          pattern);

        context.ReportDiagnostic(diagnostic);
      }
    }
  }

  private static void CheckParameterOrdering(SymbolAnalysisContext context, INamedTypeSymbol namedType)
  {
    // Collect all properties with [Parameter] attribute
    List<(IPropertySymbol Property, AttributeData Attribute)> parameters = [];

    foreach (ISymbol member in namedType.GetMembers())
    {
      if (member is not IPropertySymbol property)
        continue;

      AttributeData? paramAttr = property.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == ParameterAttributeName);

      if (paramAttr is not null)
      {
        parameters.Add((property, paramAttr));
      }
    }

    // If there are 2 or more parameters, all must have explicit Order
    if (parameters.Count < 2)
      return;

    foreach ((IPropertySymbol property, AttributeData attr) in parameters)
    {
      // Check if Order is set (look for named argument)
      bool hasOrder = attr.NamedArguments.Any(arg =>
        arg.Key == "Order" && arg.Value.Value is int orderValue && orderValue >= 0);

      if (!hasOrder)
      {
        // Get the location of the attribute for the diagnostic
        Location? location = attr.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation();

        if (location is not null)
        {
          Diagnostic diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.MultipleParametersRequireOrder,
            location,
            property.Name);

          context.ReportDiagnostic(diagnostic);
        }
      }
    }
  }
}
