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
        _ = cancellationToken; // TODO: Forward to parser when available

        List<Diagnostic> diagnostics = [];

        // Quick checks for common syntax errors
        if (routeInfo.Pattern.Contains('<', StringComparison.Ordinal) && routeInfo.Pattern.Contains('>', StringComparison.Ordinal))
        {
            // NURU001: Invalid parameter syntax
            // Find the parameter part for the message
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

        // Check for unbalanced braces
        int openBraces = routeInfo.Pattern.Count(c => c == '{');
        int closeBraces = routeInfo.Pattern.Count(c => c == '}');
        if (openBraces != closeBraces)
        {
            // NURU002: Unbalanced braces
            diagnostics.Add(Diagnostic.Create(
                DiagnosticDescriptors.UnbalancedBraces,
                routeInfo.Location,
                routeInfo.Pattern));
        }

        // Check for invalid option format
        if (routeInfo.Pattern.Contains(" -", StringComparison.Ordinal) && !routeInfo.Pattern.Contains(" --", StringComparison.Ordinal))
        {
            int index = routeInfo.Pattern.IndexOf(" -", StringComparison.Ordinal);
            if (index + 2 < routeInfo.Pattern.Length && routeInfo.Pattern[index + 2] != '-')
            {
                // NURU003: Invalid option format
                diagnostics.Add(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidOptionFormat,
                    routeInfo.Location,
                    routeInfo.Pattern.Substring(index + 1)));
            }
        }

        return new DiagnosticResult(diagnostics.ToArray());
    }
}

// Route information with equatable semantics for caching
internal sealed record RouteInfo(string Pattern, Location Location);

// Result of analyzing a route pattern
internal sealed record DiagnosticResult(ImmutableArray<Diagnostic> Diagnostics)
{
    public DiagnosticResult(Diagnostic[] diagnostics) : this(ImmutableArray.Create(diagnostics)) { }
}