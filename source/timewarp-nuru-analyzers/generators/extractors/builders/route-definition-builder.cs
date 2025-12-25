// Fluent builder for assembling RouteDefinition from pieces.
//
// The builder collects data from different analysis phases:
// - Pattern parsing -> segments, specificity
// - Fluent chain analysis -> description, aliases
// - Delegate analysis -> handler definition
// - Middleware analysis -> pipeline definition
// - Route registration context -> group prefix, order

namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Fluent builder for assembling RouteDefinition from pieces.
/// Each piece may come from a different analysis phase.
/// </summary>
internal sealed class RouteDefinitionBuilder
{
  private string? Pattern;
  private ImmutableArray<SegmentDefinition> Segments = [];
  private string MessageType = "Unspecified";
  private string? Description;
  private HandlerDefinition? Handler;
  private PipelineDefinition? Pipeline;
  private ImmutableArray<string> Aliases = [];
  private string? GroupPrefix;
  private int Specificity;
  private int Order;

  /// <summary>
  /// Sets the original pattern string.
  /// </summary>
  public RouteDefinitionBuilder WithPattern(string pattern)
  {
    Pattern = pattern;
    return this;
  }

  /// <summary>
  /// Sets the segments converted from Syntax or CompiledRoute.
  /// Use <see cref="SegmentDefinitionConverter"/> to convert segments.
  /// </summary>
  public RouteDefinitionBuilder WithSegments(ImmutableArray<SegmentDefinition> segments)
  {
    Segments = segments;
    return this;
  }

  /// <summary>
  /// Sets the message type (Query, Command, IdempotentCommand, Unspecified).
  /// </summary>
  public RouteDefinitionBuilder WithMessageType(string messageType)
  {
    MessageType = messageType;
    return this;
  }

  /// <summary>
  /// Sets the route description (from .WithDescription() fluent call).
  /// </summary>
  public RouteDefinitionBuilder WithDescription(string? description)
  {
    Description = description;
    return this;
  }

  /// <summary>
  /// Sets the handler definition (from delegate/method analysis).
  /// </summary>
  public RouteDefinitionBuilder WithHandler(HandlerDefinition handler)
  {
    Handler = handler;
    return this;
  }

  /// <summary>
  /// Sets the pipeline definition (from middleware analysis).
  /// </summary>
  public RouteDefinitionBuilder WithPipeline(PipelineDefinition? pipeline)
  {
    Pipeline = pipeline;
    return this;
  }

  /// <summary>
  /// Sets the route aliases (from .WithAlias() fluent calls).
  /// </summary>
  public RouteDefinitionBuilder WithAliases(ImmutableArray<string> aliases)
  {
    Aliases = aliases;
    return this;
  }

  /// <summary>
  /// Adds a single alias.
  /// </summary>
  public RouteDefinitionBuilder WithAlias(string alias)
  {
    Aliases = Aliases.Add(alias);
    return this;
  }

  /// <summary>
  /// Sets the group prefix (from route grouping context).
  /// </summary>
  public RouteDefinitionBuilder WithGroupPrefix(string? prefix)
  {
    GroupPrefix = prefix;
    return this;
  }

  /// <summary>
  /// Sets the computed specificity score.
  /// </summary>
  public RouteDefinitionBuilder WithSpecificity(int specificity)
  {
    Specificity = specificity;
    return this;
  }

  /// <summary>
  /// Sets the registration order.
  /// </summary>
  public RouteDefinitionBuilder WithOrder(int order)
  {
    Order = order;
    return this;
  }

  /// <summary>
  /// Builds the immutable RouteDefinition.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
  public RouteDefinition Build()
  {
    if (Pattern is null)
    {
      throw new InvalidOperationException("Pattern is required. Call WithPattern() before Build().");
    }

    if (Handler is null)
    {
      throw new InvalidOperationException("Handler is required. Call WithHandler() before Build().");
    }

    return new RouteDefinition
    (
      OriginalPattern: Pattern,
      Segments: Segments,
      MessageType: MessageType,
      Description: Description,
      Handler: Handler,
      Pipeline: Pipeline,
      Aliases: Aliases,
      GroupPrefix: GroupPrefix,
      ComputedSpecificity: Specificity,
      Order: Order
    );
  }
}
