// Semantic DSL interpreter that walks fluent builder chains.
//
// Instead of pattern-matching syntax, this interpreter "executes" the DSL
// by dispatching method calls to corresponding IR builder methods.
//
// Phase 1: Supports pure fluent chains only.
// Phase 2: Adds group support via IIrGroupBuilder.
// Future phases will add variable tracking for fragmented styles.
//
// Key design:
// - Uses SemanticModel for accurate type resolution
// - Unrolls fluent chains from nested syntax to execution order
// - Dispatches to IR builders based on method name
// - Uses marker interfaces (IIrRouteSource, IIrAppBuilder, IIrGroupBuilder, IIrRouteBuilder)
//   for polymorphic dispatch without explicit type enumeration
// - Fails fast on unrecognized DSL methods

namespace TimeWarp.Nuru.Generators;

using RoslynSyntaxNode = Microsoft.CodeAnalysis.SyntaxNode;

/// <summary>
/// Interprets DSL code semantically by walking statements and dispatching to IR builders.
/// </summary>
/// <remarks>
/// CA1859 is suppressed because this class uses polymorphic dispatch where methods
/// intentionally return object? to support different builder types in a fluent chain.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance", Justification = "Polymorphic dispatch pattern requires object? return types")]
public sealed class DslInterpreter
{
  private readonly SemanticModel SemanticModel;
  private readonly CancellationToken CancellationToken;

  /// <summary>
  /// Creates a new DSL interpreter.
  /// </summary>
  /// <param name="semanticModel">Semantic model for type resolution.</param>
  /// <param name="cancellationToken">Cancellation token.</param>
  public DslInterpreter(SemanticModel semanticModel, CancellationToken cancellationToken)
  {
    SemanticModel = semanticModel;
    CancellationToken = cancellationToken;
  }

  /// <summary>
  /// Interprets from a CreateBuilder() call to produce an AppModel.
  /// </summary>
  /// <param name="createBuilderCall">The NuruApp.CreateBuilder() invocation.</param>
  /// <returns>The interpreted AppModel.</returns>
  public AppModel Interpret(InvocationExpressionSyntax createBuilderCall)
  {
    // Create root IR builder
    IrAppBuilder irAppBuilder = new();

    // For Phase 1, we handle pure fluent chains only.
    // Find the outermost expression that contains this CreateBuilder call.
    // Walk up to find the full chain including Build() and potentially RunAsync().
    ExpressionSyntax? chainRoot = FindFluentChainRoot(createBuilderCall);

    if (chainRoot is InvocationExpressionSyntax rootInvocation)
    {
      // Unroll and execute the fluent chain
      EvaluateFluentChain(rootInvocation, irAppBuilder);
    }

    // Look for RunAsync() call on the result
    FindAndProcessRunAsyncCalls(createBuilderCall, irAppBuilder);

    return irAppBuilder.FinalizeModel();
  }

  /// <summary>
  /// Finds the root of the fluent chain containing the given expression.
  /// Walks up through member access and invocation expressions.
  /// </summary>
  private static ExpressionSyntax? FindFluentChainRoot(ExpressionSyntax expression)
  {
    RoslynSyntaxNode? current = expression;
    ExpressionSyntax? root = expression;

    while (current?.Parent is not null)
    {
      switch (current.Parent)
      {
        case MemberAccessExpressionSyntax:
          current = current.Parent;
          continue;

        case InvocationExpressionSyntax invocation:
          root = invocation;
          current = current.Parent;
          continue;

        case ArgumentSyntax:
        case ArgumentListSyntax:
          current = current.Parent;
          continue;

        default:
          return root;
      }
    }

    return root;
  }

  /// <summary>
  /// Evaluates a fluent chain by unrolling it and dispatching each call.
  /// </summary>
  private void EvaluateFluentChain(InvocationExpressionSyntax chainRoot, IrAppBuilder irAppBuilder)
  {
    // Unroll the chain to get calls in execution order
    List<InvocationExpressionSyntax> calls = UnrollFluentChain(chainRoot);

    // Current receiver state - starts as null (static call), then tracks builder type
    object? currentReceiver = null;

    foreach (InvocationExpressionSyntax call in calls)
    {
      currentReceiver = DispatchMethodCall(call, currentReceiver, irAppBuilder);
    }
  }

  /// <summary>
  /// Unrolls a fluent chain from nested syntax to execution order.
  /// For a.B().C().D(), returns [a.B(), a.B().C(), a.B().C().D()] but we process
  /// by walking down and collecting in reverse, then reversing.
  /// </summary>
  private static List<InvocationExpressionSyntax> UnrollFluentChain(InvocationExpressionSyntax outermost)
  {
    List<InvocationExpressionSyntax> calls = [];
    InvocationExpressionSyntax? current = outermost;

    // Walk down the chain collecting invocations
    while (current is not null)
    {
      calls.Add(current);

      // Get the expression this method was called on
      if (current.Expression is MemberAccessExpressionSyntax memberAccess)
      {
        if (memberAccess.Expression is InvocationExpressionSyntax nextInvocation)
        {
          current = nextInvocation;
        }
        else
        {
          // Reached the start (e.g., NuruApp.CreateBuilder)
          break;
        }
      }
      else
      {
        break;
      }
    }

    // Reverse to get execution order
    calls.Reverse();
    return calls;
  }

  /// <summary>
  /// Dispatches a method call to the appropriate IR builder method.
  /// </summary>
  /// <param name="invocation">The method invocation.</param>
  /// <param name="currentReceiver">The current receiver state (IR builder or marker).</param>
  /// <param name="irAppBuilder">The root app builder.</param>
  /// <returns>The new receiver state after this call.</returns>
  private object? DispatchMethodCall(
    InvocationExpressionSyntax invocation,
    object? currentReceiver,
    IrAppBuilder irAppBuilder)
  {
    string? methodName = GetMethodName(invocation);
    if (methodName is null)
      return currentReceiver;

    return methodName switch
    {
      "CreateBuilder" => irAppBuilder,

      "Map" => DispatchMap(invocation, currentReceiver),

      "WithGroupPrefix" => DispatchWithGroupPrefix(invocation, currentReceiver),

      "WithHandler" => DispatchWithHandler(invocation, currentReceiver),

      "WithDescription" => DispatchWithDescription(invocation, currentReceiver),

      "AsQuery" => DispatchAsQuery(currentReceiver),

      "AsCommand" => DispatchAsCommand(currentReceiver),

      "AsIdempotentCommand" => DispatchAsIdempotentCommand(currentReceiver),

      "Done" => DispatchDone(currentReceiver),

      "Build" => DispatchBuild(currentReceiver),

      "WithName" => DispatchWithName(invocation, currentReceiver),

      _ => throw new InvalidOperationException(
        $"Unrecognized DSL method: {methodName}. " +
        $"Location: {invocation.GetLocation().GetLineSpan()}")
    };
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // DISPATCH METHODS - Using marker interfaces for polymorphic dispatch
  // ═══════════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Dispatches Map() call to any IIrRouteSource (app or group builder).
  /// </summary>
  private static object? DispatchMap(InvocationExpressionSyntax invocation, object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteSource source)
    {
      throw new InvalidOperationException(
        $"Map() must be called on an app or group builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? pattern = ExtractStringArgument(invocation);
    if (pattern is null)
    {
      throw new InvalidOperationException(
        $"Map() requires a pattern string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return source.Map(pattern);
  }

  /// <summary>
  /// Dispatches WithGroupPrefix() call to any IIrRouteSource (app or group builder).
  /// </summary>
  private static object? DispatchWithGroupPrefix(InvocationExpressionSyntax invocation, object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteSource source)
    {
      throw new InvalidOperationException(
        $"WithGroupPrefix() must be called on an app or group builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? prefix = ExtractStringArgument(invocation);
    if (prefix is null)
    {
      throw new InvalidOperationException(
        $"WithGroupPrefix() requires a prefix string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return source.WithGroupPrefix(prefix);
  }

  /// <summary>
  /// Dispatches WithHandler() call to IIrRouteBuilder.
  /// </summary>
  private object? DispatchWithHandler(InvocationExpressionSyntax invocation, object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"WithHandler() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    HandlerDefinition? handler = HandlerExtractor.Extract(invocation, SemanticModel, CancellationToken);
    if (handler is null)
    {
      throw new InvalidOperationException(
        $"Could not extract handler from WithHandler(). Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.WithHandler(handler);
  }

  /// <summary>
  /// Dispatches WithDescription() call to IIrRouteBuilder or IIrAppBuilder.
  /// </summary>
  private static object? DispatchWithDescription(InvocationExpressionSyntax invocation, object? currentReceiver)
  {
    string? description = ExtractStringArgument(invocation);

    return currentReceiver switch
    {
      IIrRouteBuilder routeBuilder => routeBuilder.WithDescription(description ?? ""),
      IIrAppBuilder appBuilder => appBuilder.WithDescription(description ?? ""),
      _ => throw new InvalidOperationException(
        $"WithDescription() must be called on an app or route builder. Location: {invocation.GetLocation().GetLineSpan()}")
    };
  }

  /// <summary>
  /// Dispatches WithName() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchWithName(InvocationExpressionSyntax invocation, object? currentReceiver)
  {
    if (currentReceiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException(
        $"WithName() must be called on an app builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? name = ExtractStringArgument(invocation);
    return appBuilder.WithName(name ?? "");
  }

  /// <summary>
  /// Dispatches AsQuery() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsQuery(object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsQuery() must be called on a route builder.");
    }

    return routeBuilder.AsQuery();
  }

  /// <summary>
  /// Dispatches AsCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsCommand(object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsCommand() must be called on a route builder.");
    }

    return routeBuilder.AsCommand();
  }

  /// <summary>
  /// Dispatches AsIdempotentCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsIdempotentCommand(object? currentReceiver)
  {
    if (currentReceiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsIdempotentCommand() must be called on a route builder.");
    }

    return routeBuilder.AsIdempotentCommand();
  }

  /// <summary>
  /// Dispatches Done() call to IIrRouteBuilder or IIrGroupBuilder.
  /// </summary>
  private static object? DispatchDone(object? currentReceiver)
  {
    return currentReceiver switch
    {
      IIrRouteBuilder routeBuilder => routeBuilder.Done(),
      IIrGroupBuilder groupBuilder => groupBuilder.Done(),
      _ => throw new InvalidOperationException("Done() must be called on a route or group builder.")
    };
  }

  /// <summary>
  /// Dispatches Build() call to IIrAppBuilder.
  /// </summary>
  private static object? DispatchBuild(object? currentReceiver)
  {
    if (currentReceiver is not IIrAppBuilder appBuilder)
    {
      throw new InvalidOperationException("Build() must be called on an app builder.");
    }

    // Build() marks the app as built
    appBuilder.Build();

    // Return a marker indicating the app is built (for RunAsync detection)
    return new BuiltAppMarker(appBuilder);
  }

  /// <summary>
  /// Finds and processes RunAsync() calls following the Build().
  /// </summary>
  private void FindAndProcessRunAsyncCalls(InvocationExpressionSyntax createBuilderCall, IrAppBuilder irAppBuilder)
  {
    // Find the statement containing CreateBuilder
    RoslynSyntaxNode? statementNode = createBuilderCall.Parent;
    while (statementNode is not null && statementNode is not StatementSyntax)
    {
      statementNode = statementNode.Parent;
    }

    if (statementNode is not StatementSyntax statement)
      return;

    // Get the containing block
    if (statement.Parent is not BlockSyntax block)
      return;

    // Find statements after our statement
    bool foundOurStatement = false;
    foreach (StatementSyntax stmt in block.Statements)
    {
      if (stmt == statement)
      {
        foundOurStatement = true;
        // Also check if RunAsync is in this same statement (chained)
        ProcessRunAsyncInStatement(stmt, irAppBuilder);
        continue;
      }

      if (foundOurStatement)
      {
        ProcessRunAsyncInStatement(stmt, irAppBuilder);
      }
    }
  }

  /// <summary>
  /// Processes any RunAsync() calls in a statement.
  /// </summary>
  private void ProcessRunAsyncInStatement(StatementSyntax statement, IrAppBuilder irAppBuilder)
  {
    // Find all RunAsync invocations in this statement
    IEnumerable<InvocationExpressionSyntax> runAsyncCalls = statement
      .DescendantNodes()
      .OfType<InvocationExpressionSyntax>()
      .Where(inv => GetMethodName(inv) == "RunAsync");

    foreach (InvocationExpressionSyntax runAsyncCall in runAsyncCalls)
    {
      InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, runAsyncCall);
      if (site is not null)
      {
        irAppBuilder.AddInterceptSite(site);
      }
    }
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
  /// Extracts the first string argument from a method invocation.
  /// </summary>
  private static string? ExtractStringArgument(InvocationExpressionSyntax invocation)
  {
    ArgumentListSyntax? args = invocation.ArgumentList;
    if (args is null || args.Arguments.Count == 0)
      return null;

    ExpressionSyntax argExpression = args.Arguments[0].Expression;

    return argExpression switch
    {
      LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression)
        => literal.Token.ValueText,
      _ => null
    };
  }

  /// <summary>
  /// Marker type indicating a built app (for RunAsync detection).
  /// </summary>
  private sealed class BuiltAppMarker(IIrAppBuilder builder)
  {
    public IIrAppBuilder Builder { get; } = builder;
  }
}
