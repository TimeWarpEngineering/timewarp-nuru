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

    diagnostics.Add
    (
      Diagnostic.Create
      (
        DiagnosticDescriptors.DebugRouteFound,
        routeInfo.Location,
        routeInfo.Pattern
      )
    );

    // Use the actual parser to validate the route pattern
    bool parseSuccess =
      RoutePatternParser.TryParse
      (
        routeInfo.Pattern,
        out _,
        out IReadOnlyList<ParseError>? parseErrors,
        out IReadOnlyList<SemanticError>? semanticErrors
      );

    if (!parseSuccess)
    {
      // Map parser errors to our diagnostics
      if (parseErrors is not null)
      {
        foreach (ParseError error in parseErrors)
        {
          Diagnostic? diagnostic = MapParseErrorToDiagnostic(error, routeInfo);
          if (diagnostic is not null)
          {
            diagnostics.Add(diagnostic);
          }
        }
      }

      // Map semantic errors to our diagnostics
      if (semanticErrors is not null)
      {
        foreach (SemanticError error in semanticErrors)
        {
          Diagnostic? diagnostic = MapSemanticErrorToDiagnostic(error, routeInfo);
          if (diagnostic is not null)
          {
            diagnostics.Add(diagnostic);
          }
        }
      }
    }

    return new DiagnosticResult(diagnostics.ToArray());
  }

  private static Diagnostic? MapParseErrorToDiagnostic(ParseError error, RouteInfo routeInfo)
  {
    // Map parser error types directly to diagnostic codes using pattern matching
    return error switch
    {
      InvalidParameterSyntaxError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.InvalidParameterSyntax,
          routeInfo.Location,
          e.InvalidSyntax,
          e.Suggestion
        ),

      UnbalancedBracesError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.UnbalancedBraces,
          routeInfo.Location,
          e.Pattern
        ),

      InvalidOptionFormatError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.InvalidOptionFormat,
          routeInfo.Location,
          e.InvalidOption
        ),

      InvalidTypeConstraintError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.InvalidTypeConstraint,
          routeInfo.Location,
          e.InvalidType
        ),

      InvalidCharacterError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.InvalidCharacter,
          routeInfo.Location,
          e.Character
        ),

      UnexpectedTokenError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.UnexpectedToken,
          routeInfo.Location,
          e.Expected,
          e.Found
        ),

      NullPatternError =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.NullPattern,
          routeInfo.Location
        ),

      // Default case for any new error types
      _ => null
    };
  }

  private static Diagnostic? MapSemanticErrorToDiagnostic(SemanticError error, RouteInfo routeInfo)
  {
    // Map semantic error types to diagnostic codes using pattern matching
    return error switch
    {
      DuplicateParameterNamesError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.DuplicateParameterNames,
          routeInfo.Location,
          e.ParameterName
        ),

      ConflictingOptionalParametersError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.ConflictingOptionalParameters,
          routeInfo.Location,
          string.Join(", ", e.ConflictingParameters)
        ),

      CatchAllNotAtEndError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.CatchAllNotAtEnd,
          routeInfo.Location,
          e.CatchAllParameter,
          e.FollowingSegment
        ),

      MixedCatchAllWithOptionalError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.MixedCatchAllWithOptional,
          routeInfo.Location,
          string.Join(", ", e.OptionalParams),
          e.CatchAllParam
        ),

      DuplicateOptionAliasError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.DuplicateOptionAlias,
          routeInfo.Location,
          e.Alias,
          string.Join(", ", e.ConflictingOptions)
        ),

      OptionalBeforeRequiredError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.OptionalBeforeRequired,
          routeInfo.Location,
          e.OptionalParam,
          e.RequiredParam
        ),

      InvalidEndOfOptionsSeparatorError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.InvalidEndOfOptionsSeparator,
          routeInfo.Location,
          e.Reason
        ),

      OptionsAfterEndOfOptionsSeparatorError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.OptionsAfterEndOfOptionsSeparator,
          routeInfo.Location,
          e.Option
        ),

      ParameterAfterCatchAllError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.ParameterAfterCatchAll,
          routeInfo.Location,
          e.Parameter,
          e.CatchAll
        ),

      ParameterAfterRepeatedError e =>
        Diagnostic.Create
        (
          DiagnosticDescriptors.ParameterAfterRepeated,
          routeInfo.Location,
          e.Parameter,
          e.RepeatedParam
        ),

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
