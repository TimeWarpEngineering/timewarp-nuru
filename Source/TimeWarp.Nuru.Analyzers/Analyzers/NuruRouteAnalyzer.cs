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
        context.RegisterSourceOutput(routeInvocations, static (_, _) =>
        {
            // TODO: Analyze route pattern and report diagnostics
        });
    }

    private static bool IsAddRouteInvocation(SyntaxNode node)
    {
        // TODO: Implement predicate to identify AddRoute method calls
        _ = node;
        return false;
    }

    private static RouteInfo? GetRouteInfo(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        // TODO: Extract route pattern from the syntax node
        _ = context;
        _ = cancellationToken;
        return null;
    }
}

// Placeholder for route information
internal sealed record RouteInfo(string Pattern, Location Location);