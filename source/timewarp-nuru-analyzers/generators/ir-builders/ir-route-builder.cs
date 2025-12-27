// IR builder that mirrors the route builder DSL for semantic interpretation.
//
// This builder is used by the DslInterpreter to configure a single route,
// accumulating state via the internal RouteDefinitionBuilder.
//
// Key design:
// - Method names mirror DSL methods exactly
// - Uses existing RouteDefinitionBuilder for state accumulation
// - Done() registers the route with parent and returns parent for chaining
// - Implements IIrRouteBuilder for polymorphic dispatch in interpreter

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// IR builder that mirrors the route builder DSL for semantic interpretation.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to via Done().</typeparam>
public sealed class IrRouteBuilder<TParent> : IIrRouteBuilder
{
  private readonly TParent Parent;
  private readonly Action<RouteDefinition> RegisterRoute;
  private readonly RouteDefinitionBuilder Builder = new();

  /// <summary>
  /// Creates a new route builder.
  /// </summary>
  /// <param name="parent">The parent builder to return to via Done().</param>
  /// <param name="pattern">The route pattern string.</param>
  /// <param name="segments">The parsed segments from PatternStringExtractor.</param>
  /// <param name="registerRoute">Callback to register the completed route with parent.</param>
  internal IrRouteBuilder(
    TParent parent,
    string pattern,
    ImmutableArray<SegmentDefinition> segments,
    Action<RouteDefinition> registerRoute)
  {
    Parent = parent;
    RegisterRoute = registerRoute;

    Builder.WithPattern(pattern);
    Builder.WithSegments(segments);

    // Calculate specificity from segments
    int specificity = segments.Sum(s => s.SpecificityContribution);
    Builder.WithSpecificity(specificity);
  }

  /// <summary>
  /// Sets the handler for this route.
  /// Mirrors: RouteBuilder.WithHandler()
  /// </summary>
  public IrRouteBuilder<TParent> WithHandler(HandlerDefinition handler)
  {
    Builder.WithHandler(handler);
    return this;
  }

  /// <summary>
  /// Sets the description for this route.
  /// Mirrors: RouteBuilder.WithDescription()
  /// </summary>
  public IrRouteBuilder<TParent> WithDescription(string description)
  {
    Builder.WithDescription(description);
    return this;
  }

  /// <summary>
  /// Marks this route as a Query.
  /// Mirrors: RouteBuilder.AsQuery()
  /// </summary>
  public IrRouteBuilder<TParent> AsQuery()
  {
    Builder.WithMessageType("Query");
    return this;
  }

  /// <summary>
  /// Marks this route as a Command.
  /// Mirrors: RouteBuilder.AsCommand()
  /// </summary>
  public IrRouteBuilder<TParent> AsCommand()
  {
    Builder.WithMessageType("Command");
    return this;
  }

  /// <summary>
  /// Marks this route as an IdempotentCommand.
  /// Mirrors: RouteBuilder.AsIdempotentCommand()
  /// </summary>
  public IrRouteBuilder<TParent> AsIdempotentCommand()
  {
    Builder.WithMessageType("IdempotentCommand");
    return this;
  }

  /// <summary>
  /// Completes the route and returns to the parent builder.
  /// Mirrors: RouteBuilder.Done()
  /// </summary>
  public TParent Done()
  {
    RouteDefinition route = Builder.Build();
    RegisterRoute(route);
    return Parent;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // EXPLICIT INTERFACE IMPLEMENTATIONS
  // ═══════════════════════════════════════════════════════════════════════════════
  // These return interface types for polymorphic dispatch in DslInterpreter.
  // The public methods above return concrete types for direct usage.

  IIrRouteBuilder IIrRouteBuilder.WithHandler(HandlerDefinition handler) => WithHandler(handler);
  IIrRouteBuilder IIrRouteBuilder.WithDescription(string description) => WithDescription(description);
  IIrRouteBuilder IIrRouteBuilder.AsQuery() => AsQuery();
  IIrRouteBuilder IIrRouteBuilder.AsCommand() => AsCommand();
  IIrRouteBuilder IIrRouteBuilder.AsIdempotentCommand() => AsIdempotentCommand();
  object IIrRouteBuilder.Done() => Done()!;
}
