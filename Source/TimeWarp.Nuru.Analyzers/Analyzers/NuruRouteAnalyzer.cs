namespace TimeWarp.Nuru.Analyzers;

[Generator]
public class NuruRouteAnalyzer : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Find all AddRoute invocations
    IncrementalValuesProvider<RouteInfo?> routeInvocations = context.SyntaxProvider
        .CreateSyntaxProvider(
            predicate: static (node, _) => IsAddRouteInvocation(node),
            transform: static (ctx, ct) => GetRouteInfo(ctx, ct))
        .Where(static info => info is not null);

    // Step 2: Analyze routes and report diagnostics
    IncrementalValuesProvider<DiagnosticResult> diagnostics = routeInvocations
        .Select(static (info, ct) => AnalyzeRoutePattern(info!, ct))
        .Where(static result => result.Diagnostics.Length > 0);

    // Step 3: Report diagnostics
    context.RegisterSourceOutput(diagnostics, static (ctx, result) =>
    {
      foreach (Diagnostic diagnostic in result.Diagnostics)
      {
        ctx.ReportDiagnostic(diagnostic);
      }
    });
  }

  private static bool IsAddRouteInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    // Check if this is an invocation expression
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    // Check if it's a member access expression (e.g., builder.AddRoute)
    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    // Check if the method name is "AddRoute"
    return memberAccess.Name.Identifier.Text == "AddRoute";
  }

  private static RouteInfo? GetRouteInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    _ = cancellationToken; // Will use later for parsing

    // The node should be an InvocationExpressionSyntax (validated by predicate)
    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    // Get the argument list
    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count == 0)
      return null;

    // The first argument should be the route pattern string
    ArgumentSyntax firstArgument = argumentList.Arguments[0];
    ExpressionSyntax? expression = firstArgument.Expression;

    // We only handle string literals for now
    if (expression is not LiteralExpressionSyntax literal ||
        !literal.IsKind(SyntaxKind.StringLiteralExpression))
      return null;

    // Extract the route pattern string value
    string? pattern = literal.Token.ValueText;
    if (string.IsNullOrEmpty(pattern))
      return null;

    // Get the location for error reporting
    Location location = literal.GetLocation();

    return new RouteInfo(pattern, location);
  }
  private static DiagnosticResult AnalyzeRoutePattern(RouteInfo routeInfo, CancellationToken cancellationToken)
  {
    _ = cancellationToken; // Parser is synchronous

    List<Diagnostic> diagnostics = [];

    // Temporary debug: Report every route we see
    diagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.DebugRouteFound,
        routeInfo.Location,
        routeInfo.Pattern));

    // Use the actual parser to validate the route pattern
    bool parseSuccess = RoutePatternParser.TryParse(
        routeInfo.Pattern,
        out _,
        out IReadOnlyList<ParseError> parseErrors);

    if (!parseSuccess)
    {
      // Map parser errors to our diagnostics
      foreach (ParseError error in parseErrors)
      {
        Diagnostic? diagnostic = MapParseErrorToDiagnostic(error, routeInfo);
        if (diagnostic is not null)
        {
          diagnostics.Add(diagnostic);
        }
      }
    }

    // The parser now handles all syntax errors with typed error codes
    // No need for redundant checks here

    return new DiagnosticResult(diagnostics.ToArray());
  }

  private static Diagnostic? MapParseErrorToDiagnostic(ParseError error, RouteInfo routeInfo)
  {
    // Map parser error types directly to diagnostic codes
    return error.ErrorType switch
    {
      ParseErrorType.InvalidParameterSyntax => Diagnostic.Create(
          DiagnosticDescriptors.InvalidParameterSyntax,
          routeInfo.Location,
          error.Message,
          error.Suggestion ?? ""),

      ParseErrorType.UnbalancedBraces => Diagnostic.Create(
          DiagnosticDescriptors.UnbalancedBraces,
          routeInfo.Location,
          routeInfo.Pattern),

      ParseErrorType.InvalidOptionFormat => Diagnostic.Create(
          DiagnosticDescriptors.InvalidOptionFormat,
          routeInfo.Location,
          error.Message),

      ParseErrorType.InvalidTypeConstraint => Diagnostic.Create(
          DiagnosticDescriptors.InvalidTypeConstraint,
          routeInfo.Location,
          error.Message),

      ParseErrorType.CatchAllNotAtEnd => Diagnostic.Create(
          DiagnosticDescriptors.CatchAllNotAtEnd,
          routeInfo.Location,
          error.Message),

      ParseErrorType.DuplicateParameterNames => Diagnostic.Create(
          DiagnosticDescriptors.DuplicateParameterNames,
          routeInfo.Location,
          error.Message),

      ParseErrorType.ConflictingOptionalParameters => Diagnostic.Create(
          DiagnosticDescriptors.ConflictingOptionalParameters,
          routeInfo.Location,
          error.Message),

      ParseErrorType.MixedCatchAllWithOptional => Diagnostic.Create(
          DiagnosticDescriptors.MixedCatchAllWithOptional,
          routeInfo.Location,
          error.Message),

      ParseErrorType.DuplicateOptionAlias => Diagnostic.Create(
          DiagnosticDescriptors.DuplicateOptionAlias,
          routeInfo.Location,
          error.Message),

      // Generic errors we don't map to specific diagnostics
      ParseErrorType.Generic => null,

      // Default case for any new error types
      _ => null
    };
  }
}

// Route information with equatable semantics for caching
internal sealed record RouteInfo(string Pattern, Location Location);

// Result of analyzing a route pattern
internal sealed record DiagnosticResult(ImmutableArray<Diagnostic> Diagnostics)
{
  public DiagnosticResult(Diagnostic[] diagnostics) : this(ImmutableArray.Create(diagnostics)) { }
}
