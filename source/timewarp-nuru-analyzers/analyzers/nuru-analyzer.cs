// Unified Nuru analyzer that builds the full IR model and validates it.
//
// This analyzer:
// 1. Uses AppExtractor to build the full AppModel from source code
// 2. Runs all validators on the model (overlap detection, etc.)
// 3. Reports all diagnostics (extraction errors + validation errors)
//
// This replaces the pattern-level validation in NuruRouteAnalyzer with
// model-level validation that can detect cross-route issues like overlapping patterns.

namespace TimeWarp.Nuru;

using TimeWarp.Nuru.Generators;
using TimeWarp.Nuru.Validation;

/// <summary>
/// Unified analyzer that validates Nuru route patterns using the full IR model.
/// Detects extraction errors and cross-route validation issues like overlapping patterns.
/// </summary>
[Generator]
public sealed class NuruAnalyzer : IIncrementalGenerator
{
  public void Initialize(IncrementalGeneratorInitializationContext context)
  {
    // Step 1: Find all Map invocations and collect route info with locations
    IncrementalValuesProvider<RouteWithLocation?> routeInvocations = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsMapInvocation(node),
        transform: static (ctx, ct) => GetRouteWithLocation(ctx, ct))
      .Where(static info => info is not null);

    // Step 2: Find RunAsync invocations (same as the generator)
    IncrementalValuesProvider<GeneratorSyntaxContext> runAsyncCalls = context.SyntaxProvider
      .CreateSyntaxProvider(
        predicate: static (node, _) => IsRunAsyncInvocation(node),
        transform: static (ctx, _) => ctx)
      .Where(static ctx => ctx.Node is not null);

    // Step 3: Collect all route locations into a dictionary
    IncrementalValueProvider<ImmutableDictionary<string, Location>> routeLocations = routeInvocations
      .Where(static r => r is not null)
      .Collect()
      .Select(static (routes, _) =>
      {
        ImmutableDictionary<string, Location>.Builder builder =
          ImmutableDictionary.CreateBuilder<string, Location>();

        foreach (RouteWithLocation? route in routes)
        {
          if (route is not null && !builder.ContainsKey(route.Pattern))
          {
            builder[route.Pattern] = route.Location;
          }
        }

        return builder.ToImmutable();
      });

    // Step 4: Combine RunAsync calls with route locations
    IncrementalValuesProvider<(GeneratorSyntaxContext Context, ImmutableDictionary<string, Location> RouteLocations)> combined =
      runAsyncCalls.Combine(routeLocations);

    // Step 5: Extract model and validate
    IncrementalValuesProvider<ImmutableArray<Diagnostic>> diagnostics = combined
      .Select(static (pair, ct) => ExtractAndValidate(pair.Context, pair.RouteLocations, ct))
      .Where(static d => d.Length > 0);

    // Step 6: Report all diagnostics
    context.RegisterSourceOutput(diagnostics, static (ctx, diags) =>
    {
      foreach (Diagnostic diagnostic in diags)
      {
        ctx.ReportDiagnostic(diagnostic);
      }
    });
  }

  /// <summary>
  /// Checks if a syntax node is a Map() invocation.
  /// </summary>
  private static bool IsMapInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text == "Map";
  }

  /// <summary>
  /// Checks if a syntax node is a RunAsync() invocation.
  /// </summary>
  private static bool IsRunAsyncInvocation(Microsoft.CodeAnalysis.SyntaxNode node)
  {
    if (node is not InvocationExpressionSyntax invocation)
      return false;

    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
      return false;

    return memberAccess.Name.Identifier.Text == "RunAsync";
  }

  /// <summary>
  /// Extracts route pattern and location from a Map() call.
  /// </summary>
  private static RouteWithLocation? GetRouteWithLocation(GeneratorSyntaxContext context, CancellationToken cancellationToken)
  {
    _ = cancellationToken;

    if (context.Node is not InvocationExpressionSyntax invocation)
      return null;

    ArgumentListSyntax? argumentList = invocation.ArgumentList;
    if (argumentList is null || argumentList.Arguments.Count == 0)
      return null;

    ArgumentSyntax firstArgument = argumentList.Arguments[0];
    ExpressionSyntax? expression = firstArgument.Expression;

    if (expression is not LiteralExpressionSyntax literal ||
        !literal.IsKind(SyntaxKind.StringLiteralExpression))
      return null;

    string? pattern = literal.Token.ValueText;
    if (string.IsNullOrEmpty(pattern))
      return null;

    Location location = literal.GetLocation();

    return new RouteWithLocation(pattern, location);
  }

  /// <summary>
  /// Extracts the model and runs all validators.
  /// </summary>
  private static ImmutableArray<Diagnostic> ExtractAndValidate(
    GeneratorSyntaxContext context,
    ImmutableDictionary<string, Location> routeLocations,
    CancellationToken cancellationToken)
  {
    // Extract model with diagnostics
    ExtractionResult result = AppExtractor.ExtractWithDiagnostics(context, cancellationToken);

    List<Diagnostic> allDiagnostics = [.. result.Diagnostics];

    // If we have a model, run validators
    if (result.Model is not null)
    {
      ImmutableArray<Diagnostic> validationDiagnostics = ModelValidator.Validate(
        result.Model,
        routeLocations);

      allDiagnostics.AddRange(validationDiagnostics);
    }

    return [.. allDiagnostics];
  }

  /// <summary>
  /// Route pattern with its source location for error reporting.
  /// </summary>
  private sealed record RouteWithLocation(string Pattern, Location Location);
}
