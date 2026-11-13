#region Purpose
// Route modifier, Done, Build, and RunAsync dispatch handlers for the DSL interpreter.
#endregion

namespace TimeWarp.Nuru.Generators;

public sealed partial class DslInterpreter
{
  /// <summary>
  /// Dispatches WithAlias() call to IIrRouteBuilder.
  /// </summary>
  private object? DispatchWithAlias(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (!IsDslBuilderMethod(invocation)) return null;

    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"WithAlias() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? alias = ExtractStringArgument(invocation);
    if (alias is null)
    {
      throw new InvalidOperationException(
        $"WithAlias() requires an alias string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.WithAlias(alias);
  }

  /// <summary>
  /// Dispatches WithExample() call to IIrRouteBuilder.
  /// </summary>
  private object? DispatchWithExample(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (!IsDslBuilderMethod(invocation)) return null;

    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"WithExample() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    // Resolve the command ARGUMENT first (not just its string value) so "missing" (throw),
    // "present but not a string literal" (throw, consistent with WithAlias below), and
    // "literal but empty" (silent skip, mirroring the attribute extraction path's
    // skip-on-empty in EndpointExtractor.ExtractNuruRouteExampleAttributes) are distinguished
    // rather than all collapsing to null.
    ArgumentSyntax? commandArgument = ResolveArgumentAt(invocation, 0, "command");
    if (commandArgument is null)
    {
      throw new InvalidOperationException(
        $"WithExample() requires a command string. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    string? command = ExtractLiteralStringValue(commandArgument.Expression);
    if (command is null)
    {
      throw new InvalidOperationException(
        $"WithExample() requires a command string literal. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    if (command.Length == 0)
    {
      return routeBuilder;
    }

    // Description keeps the existing lenient behavior (non-literal/absent -> null),
    // consistent with WithDescription's precedent.
    string? description = ExtractStringArgumentAt(invocation, 1, "description");

    // Normalize "" to null so both emitters (help / capabilities) see the same absent-vs-present
    // signal - capabilities' `is not null` guard would otherwise emit an empty "description": "".
    if (string.IsNullOrEmpty(description))
      description = null;

    return routeBuilder.WithExample(command, description);
  }

  /// <summary>
  /// Dispatches Implements&lt;T&gt;() call to IIrRouteBuilder.
  /// Extracts the interface type and property assignments from the expression.
  /// </summary>
  private object? DispatchImplements(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException(
        $"Implements<T>() must be called on a route builder. Location: {invocation.GetLocation().GetLineSpan()}");
    }

    InterfaceImplementationDefinition? implementation = ImplementsExtractor.Extract(invocation, SemanticModel);
    if (implementation is null)
    {
      throw new InvalidOperationException(
        $"Could not extract interface implementation from Implements<T>(). Location: {invocation.GetLocation().GetLineSpan()}");
    }

    return routeBuilder.AddImplementation(implementation);
  }

  /// <summary>
  /// Dispatches AsQuery() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsQuery(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsQuery() must be called on a route builder.");
    }

    return routeBuilder.AsQuery();
  }

  /// <summary>
  /// Dispatches AsCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsCommand(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsCommand() must be called on a route builder.");
    }

    return routeBuilder.AsCommand();
  }

  /// <summary>
  /// Dispatches AsIdempotentCommand() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchAsIdempotentCommand(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("AsIdempotentCommand() must be called on a route builder.");
    }

    return routeBuilder.AsIdempotentCommand();
  }

  /// <summary>
  /// Dispatches LocalOnly() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchLocalOnly(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("LocalOnly() must be called on a route builder.");
    }

    return routeBuilder.LocalOnly();
  }

  /// <summary>
  /// Dispatches Exclusive() call to IIrRouteBuilder.
  /// </summary>
  private static object? DispatchExclusive(object? receiver)
  {
    if (receiver is not IIrRouteBuilder routeBuilder)
    {
      throw new InvalidOperationException("Exclusive() must be called on a route builder.");
    }

    return routeBuilder.Exclusive();
  }

  /// <summary>
  /// Dispatches Done() call to IIrRouteBuilder or IIrGroupBuilder.
  /// </summary>
  private object? DispatchDone(InvocationExpressionSyntax invocation, object? receiver)
  {
    return receiver switch
    {
      IIrRouteBuilder routeBuilder => TryDoneRoute(invocation, routeBuilder),
      IIrGroupBuilder groupBuilder => groupBuilder.Done(),
      _ => throw new InvalidOperationException("Done() must be called on a route or group builder.")
    };
  }

  /// <summary>
  /// Tries Done() on a route builder, catching parameter mismatch and emitting diagnostic.
  /// </summary>
  private object? TryDoneRoute(InvocationExpressionSyntax invocation, IIrRouteBuilder routeBuilder)
  {
    try
    {
      return routeBuilder.Done();
    }
    catch (HandlerParameterMismatchException ex)
    {
      CollectedDiagnostics.Add(Diagnostic.Create(
        DiagnosticDescriptors.ParameterNameMismatch,
        invocation.GetLocation(),
        ex.ParameterName, ex.AvailableSegments));
      return routeBuilder.DoneParent;
    }
  }

  /// <summary>
  /// Dispatches Build() call to IIrAppBuilder.
  /// </summary>
  /// <remarks>
  /// <c>EndpointBuilder.Build()</c> lets a chain end on an open route
  /// (<c>.Map("ping").WithHandler(...).Build()</c>). That route must be completed exactly as
  /// <c>Done()</c> would, then the app built; otherwise the route is silently dropped.
  /// </remarks>
  private object? DispatchBuild(InvocationExpressionSyntax invocation, object? receiver)
  {
    if (receiver is IIrRouteBuilder routeBuilder)
    {
      receiver = TryDoneRoute(invocation, routeBuilder);
    }

    // Ignore Build() calls on non-Nuru types (e.g., DotNet.Build().Build())
    if (receiver is not IrAppBuilder appBuilder)
    {
      return null;
    }

    // Mark as built, recording the Build() call so the emitter can attach the capabilities catalog
    appBuilder.Build(ExtractBuildInterceptSite(invocation));

    // Track this built app for finalization, and which Build() call built it
    BuiltApps.Add(appBuilder);
    BuildCalls[invocation] = appBuilder;

    // Return a marker for RunAsync detection
    return new BuiltAppMarker(appBuilder);
  }

  /// <summary>
  /// Extracts the intercept site of a Build() call and the type that declares that Build().
  /// Returns null when the call cannot be intercepted.
  /// </summary>
  private BuildInterceptSite? ExtractBuildInterceptSite(InvocationExpressionSyntax invocation)
  {
    if (SemanticModel.GetSymbolInfo(invocation, CancellationToken).Symbol is not IMethodSymbol { ContainingType: INamedTypeSymbol containingType })
      return null;

    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    return site is null
      ? null
      : new BuildInterceptSite(site, containingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
  }

  /// <summary>
  /// Dispatches RunAsync() call to a built app.
  /// </summary>
  private object? DispatchRunAsyncCall(InvocationExpressionSyntax invocation, object? receiver)
  {
    // Receiver could be:
    // 1. BuiltAppMarker - from chained ".Build().RunAsync()"
    // 2. IrAppBuilder - from variable reference "app.RunAsync()"
    IrAppBuilder? appBuilder = receiver switch
    {
      BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
      IrAppBuilder builder => builder,
      _ => null
    };

    // Ignore RunAsync() calls on non-Nuru types (e.g., CommandResult.RunAsync())
    if (appBuilder is null)
    {
      return null;
    }

    // Extract and add the intercept site
    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    if (site is not null)
    {
      appBuilder.AddInterceptSite("RunAsync", site);
      BoundEntryPointCalls.Add(invocation);
    }

    return null; // RunAsync returns void/Task
  }

  /// <summary>
  /// Dispatches RunReplAsync() call to a built app.
  /// </summary>
  private object? DispatchRunReplAsyncCall(InvocationExpressionSyntax invocation, object? receiver)
  {
    // Receiver could be:
    // 1. BuiltAppMarker - from chained ".Build().RunReplAsync()"
    // 2. IrAppBuilder - from variable reference "app.RunReplAsync()"
    IrAppBuilder? appBuilder = receiver switch
    {
      BuiltAppMarker marker => (IrAppBuilder)marker.Builder,
      IrAppBuilder builder => builder,
      _ => null
    };

    // Ignore RunReplAsync() calls on non-Nuru types
    if (appBuilder is null)
    {
      return null;
    }

    // Extract and add the intercept site
    InterceptSiteModel? site = InterceptSiteExtractor.Extract(SemanticModel, invocation);
    if (site is not null)
    {
      appBuilder.AddInterceptSite("RunReplAsync", site);
      BoundEntryPointCalls.Add(invocation);
    }

    return null; // RunReplAsync returns void/Task
  }

  // HELPER METHODS
  // ═══════════════════════════════════════════════════════════════════════════════
}
