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

    private static bool IsAddRouteInvocation(SyntaxNode node)
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

        // Quick checks for syntax errors that might not be caught by parser
        // or that we want to report with specific diagnostics

        // NURU001: Check for angle bracket parameters
        if (routeInfo.Pattern.Contains('<', StringComparison.Ordinal) && routeInfo.Pattern.Contains('>', StringComparison.Ordinal))
        {
            int start = routeInfo.Pattern.IndexOf('<', StringComparison.Ordinal);
            int end = routeInfo.Pattern.IndexOf('>', StringComparison.Ordinal);
            string paramPart = routeInfo.Pattern.Substring(start, end - start + 1);
            string suggestion = paramPart.Replace('<', '{').Replace('>', '}');

            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.InvalidParameterSyntax,
                routeInfo.Location,
                paramPart,
                suggestion));
        }

        // NURU003: Check for invalid option format (single dash with word)
        if (routeInfo.Pattern.Contains(" -", StringComparison.Ordinal))
        {
            int index = routeInfo.Pattern.IndexOf(" -", StringComparison.Ordinal);
            if (index + 2 < routeInfo.Pattern.Length &&
                routeInfo.Pattern[index + 2] != '-' &&
                char.IsLetter(routeInfo.Pattern[index + 2]) &&
                index + 3 < routeInfo.Pattern.Length &&
                char.IsLetter(routeInfo.Pattern[index + 3]))
            {
                // Find the end of the option
                int endIndex = index + 2;
                while (endIndex < routeInfo.Pattern.Length && !char.IsWhiteSpace(routeInfo.Pattern[endIndex]))
                {
                    endIndex++;
                }

                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidOptionFormat,
                    routeInfo.Location,
                    routeInfo.Pattern.Substring(index + 1, endIndex - index - 1)));
            }
        }

        return new DiagnosticResult(diagnostics.ToArray());
    }

    private static Diagnostic? MapParseErrorToDiagnostic(ParseError error, RouteInfo routeInfo)
    {
        // Map parser error messages to our diagnostic codes
        if (error.Message.Contains("closing brace without matching opening", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("unbalanced", StringComparison.OrdinalIgnoreCase))
        {
            return Diagnostic.Create(
                DiagnosticDescriptors.UnbalancedBraces,
                routeInfo.Location,
                routeInfo.Pattern);
        }

        // For now, we'll report other parser errors as compiler errors
        // This ensures users see all validation issues
        return null;
    }
}

// Route information with equatable semantics for caching
internal sealed record RouteInfo(string Pattern, Location Location);

// Result of analyzing a route pattern
internal sealed record DiagnosticResult(ImmutableArray<Diagnostic> Diagnostics)
{
    public DiagnosticResult(Diagnostic[] diagnostics) : this(ImmutableArray.Create(diagnostics)) { }
}