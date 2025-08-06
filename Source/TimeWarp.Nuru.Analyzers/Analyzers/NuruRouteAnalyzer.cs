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

        // Step 2: Report diagnostics (placeholder for now)
        context.RegisterSourceOutput(routeInvocations, static (ctx, routeInfo) =>
        {
            // TODO: Analyze route pattern and report diagnostics

            // For now, just report that we found the route (debug)
            if (routeInfo is not null)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.RouteFound,
                    routeInfo.Location,
                    routeInfo.Pattern);

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
}

// Placeholder for route information
internal sealed record RouteInfo(string Pattern, Location Location);