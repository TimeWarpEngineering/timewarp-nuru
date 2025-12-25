// Fluent builder for assembling RouteDefinition from pieces.
//
// The builder collects data from different analysis phases:
// - Pattern parsing -> segments, specificity
// - Fluent chain analysis -> description, aliases
// - Delegate analysis -> handler definition
// - Middleware analysis -> pipeline definition
// - Route registration context -> group prefix, order

namespace TimeWarp.Nuru.Generators;

using System.Collections.Immutable;

/// <summary>
/// Fluent builder for assembling RouteDefinition from pieces.
/// Each piece may come from a different analysis phase.
/// </summary>
internal sealed class RouteDefinitionBuilder
{
  private string? _pattern;
  private ImmutableArray<SegmentDefinition> _segments = [];
  private string _messageType = "Unspecified";
  private string? _description;
  private HandlerDefinition? _handler;
  private PipelineDefinition? _pipeline;
  private ImmutableArray<string> _aliases = [];
  private string? _groupPrefix;
  private int _specificity;
  private int _order;

  /// <summary>
  /// Sets the original pattern string.
  /// </summary>
  public RouteDefinitionBuilder WithPattern(string pattern)
  {
    _pattern = pattern;
    return this;
  }

  /// <summary>
  /// Sets the segments converted from Syntax or CompiledRoute.
  /// Use <see cref="SegmentDefinitionConverter"/> to convert segments.
  /// </summary>
  public RouteDefinitionBuilder WithSegments(ImmutableArray<SegmentDefinition> segments)
  {
    _segments = segments;
    return this;
  }

  /// <summary>
  /// Sets the message type (Query, Command, IdempotentCommand, Unspecified).
  /// </summary>
  public RouteDefinitionBuilder WithMessageType(string messageType)
  {
    _messageType = messageType;
    return this;
  }

  /// <summary>
  /// Sets the route description (from .WithDescription() fluent call).
  /// </summary>
  public RouteDefinitionBuilder WithDescription(string? description)
  {
    _description = description;
    return this;
  }

  /// <summary>
  /// Sets the handler definition (from delegate/method analysis).
  /// </summary>
  public RouteDefinitionBuilder WithHandler(HandlerDefinition handler)
  {
    _handler = handler;
    return this;
  }

  /// <summary>
  /// Sets the pipeline definition (from middleware analysis).
  /// </summary>
  public RouteDefinitionBuilder WithPipeline(PipelineDefinition? pipeline)
  {
    _pipeline = pipeline;
    return this;
  }

  /// <summary>
  /// Sets the route aliases (from .WithAlias() fluent calls).
  /// </summary>
  public RouteDefinitionBuilder WithAliases(ImmutableArray<string> aliases)
  {
    _aliases = aliases;
    return this;
  }

  /// <summary>
  /// Adds a single alias.
  /// </summary>
  public RouteDefinitionBuilder WithAlias(string alias)
  {
    _aliases = _aliases.Add(alias);
    return this;
  }

  /// <summary>
  /// Sets the group prefix (from route grouping context).
  /// </summary>
  public RouteDefinitionBuilder WithGroupPrefix(string? prefix)
  {
    _groupPrefix = prefix;
    return this;
  }

  /// <summary>
  /// Sets the computed specificity score.
  /// </summary>
  public RouteDefinitionBuilder WithSpecificity(int specificity)
  {
    _specificity = specificity;
    return this;
  }

  /// <summary>
  /// Sets the registration order.
  /// </summary>
  public RouteDefinitionBuilder WithOrder(int order)
  {
    _order = order;
    return this;
  }

  /// <summary>
  /// Builds the immutable RouteDefinition.
  /// </summary>
  /// <exception cref="InvalidOperationException">Thrown when required fields are missing.</exception>
  public RouteDefinition Build()
  {
    if (_pattern is null)
    {
      throw new InvalidOperationException("Pattern is required. Call WithPattern() before Build().");
    }

    if (_handler is null)
    {
      throw new InvalidOperationException("Handler is required. Call WithHandler() before Build().");
    }

    return new RouteDefinition
    (
      OriginalPattern: _pattern,
      Segments: _segments,
      MessageType: _messageType,
      Description: _description,
      Handler: _handler,
      Pipeline: _pipeline,
      Aliases: _aliases,
      GroupPrefix: _groupPrefix,
      ComputedSpecificity: _specificity,
      Order: _order
    );
  }
}
