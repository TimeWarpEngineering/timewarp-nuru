// Extracts route definitions and app metadata from fluent builder chains.
//
// Walks the builder chain from Build() upward to CreateBuilder():
// - For each .Map() call: extract pattern, handler, description, options, etc.
// - For app-level calls: extract name, description, aiPrompt, help, repl, etc.
// - Track .WithGroupPrefix() scope for route prefixes

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Extracts route definitions and app metadata from fluent builder chains.
/// </summary>
internal static class FluentChainExtractor
{
  /// <summary>
  /// Extracts all information from a Build() call chain into an AppModelBuilder.
  /// </summary>
  /// <param name="buildCall">The .Build() invocation expression.</param>
  /// <param name="builder">The builder to populate.</param>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  public static void ExtractToBuilder
  (
    InvocationExpressionSyntax buildCall,
    AppModelBuilder builder,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Walk the chain from Build() upward
    List<InvocationExpressionSyntax> chainCalls = CollectChainCalls(buildCall);

    // Track current group prefix for nested routes
    string? currentGroupPrefix = null;
    int routeOrder = 0;

    // Process each call in the chain (in reverse order, from CreateBuilder to Build)
    for (int i = chainCalls.Count - 1; i >= 0; i--)
    {
      InvocationExpressionSyntax invocation = chainCalls[i];
      string? methodName = GetMethodName(invocation);

      if (methodName is null)
        continue;

      switch (methodName)
      {
        case "Map":
          RouteDefinition? route = ExtractRoute(invocation, currentGroupPrefix, routeOrder++, semanticModel, cancellationToken);
          if (route is not null)
            builder.AddRoute(route);
          break;

        case "WithName":
          string? name = ExtractStringArgument(invocation);
          if (name is not null)
            builder.WithName(name);
          break;

        case "WithDescription":
          // App-level description (not route description)
          // Only apply if we haven't started processing routes yet
          if (routeOrder == 0)
          {
            string? description = ExtractStringArgument(invocation);
            if (description is not null)
              builder.WithDescription(description);
          }

          break;

        case "WithAiPrompt":
          string? aiPrompt = ExtractStringArgument(invocation);
          if (aiPrompt is not null)
            builder.WithAiPrompt(aiPrompt);
          break;

        case "AddHelp":
          builder.WithHelp();
          break;

        case "AddRepl":
          builder.WithRepl();
          break;

        case "AddConfiguration":
          builder.WithConfiguration();
          break;

        case "WithGroupPrefix":
          currentGroupPrefix = ExtractStringArgument(invocation);
          break;

        case "ConfigureServices":
          // Service extraction handled in Phase 6
          break;

        case "AddBehavior":
          // Behavior extraction handled in Phase 8
          break;
      }
    }
  }

  /// <summary>
  /// Extracts a single route definition from a Map() call.
  /// </summary>
  private static RouteDefinition? ExtractRoute
  (
    InvocationExpressionSyntax mapInvocation,
    string? groupPrefix,
    int order,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Extract the pattern string
    string? pattern = ExtractPatternFromMap(mapInvocation);
    if (pattern is null)
      return null;

    // Create a builder for this route
    RouteDefinitionBuilder routeBuilder = new();
    routeBuilder.WithPattern(pattern);
    routeBuilder.WithGroupPrefix(groupPrefix);
    routeBuilder.WithOrder(order);

    // Parse the pattern to get segments (handles both simple and mini-language patterns)
    ImmutableArray<SegmentDefinition> segments = PatternStringExtractor.ExtractSegments(pattern);
    routeBuilder.WithSegments(segments);

    // Calculate specificity from segments
    int specificity = segments.Sum(s => s.SpecificityContribution);
    routeBuilder.WithSpecificity(specificity);

    // Walk the fluent chain after Map() to find WithHandler, WithDescription, etc.
    ExtractRouteChain(mapInvocation, routeBuilder, semanticModel, cancellationToken);

    return routeBuilder.Build();
  }

  /// <summary>
  /// Walks the fluent chain after a Map() call to extract route-specific settings.
  /// </summary>
  private static void ExtractRouteChain
  (
    InvocationExpressionSyntax mapInvocation,
    RouteDefinitionBuilder builder,
    SemanticModel semanticModel,
    CancellationToken cancellationToken
  )
  {
    // Find calls that follow the Map() in the fluent chain
    // The chain is structured as: Map().WithHandler().WithDescription().AsQuery().Done()
    // Each call is the parent's expression's invocation

    RoslynSyntaxNode? current = mapInvocation.Parent;

    while (current is not null)
    {
      if (current is InvocationExpressionSyntax invocation)
      {
        string? methodName = GetMethodName(invocation);

        switch (methodName)
        {
          case "WithHandler":
            HandlerDefinition? handler = HandlerExtractor.Extract(invocation, semanticModel, cancellationToken);
            if (handler is not null)
              builder.WithHandler(handler);
            break;

          case "WithDescription":
            string? description = ExtractStringArgument(invocation);
            if (description is not null)
              builder.WithDescription(description);
            break;

          case "WithAlias":
            string? alias = ExtractStringArgument(invocation);
            if (alias is not null)
              builder.WithAlias(alias);
            break;

          case "WithOption":
            // Options can also be specified via WithOption() method
            // For now, options are primarily parsed from the pattern string
            break;

          case "AsQuery":
            builder.WithMessageType("Query");
            break;

          case "AsCommand":
            builder.WithMessageType("Command");
            break;

          case "AsIdempotentCommand":
            builder.WithMessageType("IdempotentCommand");
            break;

          case "Done":
            // End of route chain
            return;

          case "Map":
          case "Build":
            // We've reached the next route or end of chain
            return;
        }
      }

      current = current.Parent;
    }
  }

  /// <summary>
  /// Collects all invocation expressions in the builder chain from Build() to CreateBuilder().
  /// </summary>
  private static List<InvocationExpressionSyntax> CollectChainCalls(InvocationExpressionSyntax buildCall)
  {
    List<InvocationExpressionSyntax> calls = [];
    InvocationExpressionSyntax? current = buildCall;

    while (current is not null)
    {
      calls.Add(current);

      // Get the expression that this method was called on
      if (current.Expression is MemberAccessExpressionSyntax memberAccess)
      {
        // The expression could be another invocation (chained call)
        if (memberAccess.Expression is InvocationExpressionSyntax nextInvocation)
        {
          current = nextInvocation;
        }
        else
        {
          // End of chain (reached CreateBuilder or a variable)
          break;
        }
      }
      else
      {
        break;
      }
    }

    return calls;
  }

  /// <summary>
  /// Gets the method name from an invocation expression.
  /// </summary>
  private static string? GetMethodName(InvocationExpressionSyntax invocation)
  {
    return invocation.Expression switch
    {
      MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.Text,
      IdentifierNameSyntax identifier => identifier.Identifier.Text,
      _ => null
    };
  }

  /// <summary>
  /// Extracts the pattern string from Map("pattern") or Map("pattern", handler).
  /// </summary>
  private static string? ExtractPatternFromMap(InvocationExpressionSyntax mapInvocation)
  {
    ArgumentListSyntax? args = mapInvocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    // Pattern is always the first argument
    ArgumentSyntax firstArg = args.Arguments[0];
    return ExtractStringLiteral(firstArg.Expression);
  }

  /// <summary>
  /// Extracts the first string argument from a method invocation.
  /// </summary>
  private static string? ExtractStringArgument(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    return ExtractStringLiteral(args.Arguments[0].Expression);
  }

  /// <summary>
  /// Extracts the string value from a literal expression.
  /// </summary>
  private static string? ExtractStringLiteral(ExpressionSyntax expression)
  {
    return expression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }
}
