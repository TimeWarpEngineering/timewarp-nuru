namespace TimeWarp.Nuru.Generators;

/// <summary>
/// Design-time representation of a complete route definition.
/// This type exists only during compilation for use by the source generator.
/// </summary>
/// <param name="OriginalPattern">The original route pattern string as written in code</param>
/// <param name="Segments">The parsed segments of the route</param>
/// <param name="MessageType">The inferred message type (Query, Command, IdempotentCommand, Unspecified)</param>
/// <param name="Description">Optional description for help text</param>
/// <param name="Handler">Information about the handler method</param>
/// <param name="Pipeline">Optional middleware pipeline configuration</param>
/// <param name="Aliases">Alternative patterns that map to the same handler</param>
/// <param name="GroupPrefix">Prefix inherited from route group, if any</param>
/// <param name="ComputedSpecificity">Calculated specificity for route matching priority</param>
/// <param name="Order">Explicit order override, if specified</param>
public sealed record RouteDefinition(
  string OriginalPattern,
  ImmutableArray<SegmentDefinition> Segments,
  string MessageType,
  string? Description,
  HandlerDefinition Handler,
  PipelineDefinition? Pipeline,
  ImmutableArray<string> Aliases,
  string? GroupPrefix,
  int ComputedSpecificity,
  int Order)
{
  /// <summary>
  /// Creates a RouteDefinition with default values for optional parameters.
  /// </summary>
  public static RouteDefinition Create(
    string originalPattern,
    ImmutableArray<SegmentDefinition> segments,
    HandlerDefinition handler,
    string messageType = "Unspecified",
    string? description = null,
    PipelineDefinition? pipeline = null,
    ImmutableArray<string>? aliases = null,
    string? groupPrefix = null,
    int computedSpecificity = 0,
    int order = 0)
  {
    return new RouteDefinition(
      OriginalPattern: originalPattern,
      Segments: segments,
      MessageType: messageType,
      Description: description,
      Handler: handler,
      Pipeline: pipeline,
      Aliases: aliases ?? [],
      GroupPrefix: groupPrefix,
      ComputedSpecificity: computedSpecificity,
      Order: order);
  }

  /// <summary>
  /// Gets the full pattern including group prefix.
  /// </summary>
  public string FullPattern => string.IsNullOrEmpty(GroupPrefix)
    ? OriginalPattern
    : $"{GroupPrefix} {OriginalPattern}";

  /// <summary>
  /// Gets all literal segments in order.
  /// </summary>
  public IEnumerable<LiteralDefinition> Literals =>
    Segments.OfType<LiteralDefinition>();

  /// <summary>
  /// Gets all parameter segments in order.
  /// </summary>
  public IEnumerable<ParameterDefinition> Parameters =>
    Segments.OfType<ParameterDefinition>();

  /// <summary>
  /// Gets all option segments in order.
  /// </summary>
  public IEnumerable<OptionDefinition> Options =>
    Segments.OfType<OptionDefinition>();

  /// <summary>
  /// Gets whether this route has any required parameters.
  /// </summary>
  public bool HasRequiredParameters =>
    Parameters.Any(p => !p.IsOptional);

  /// <summary>
  /// Gets whether this route has any options.
  /// </summary>
  public bool HasOptions => Options.Any();

  /// <summary>
  /// Gets whether this route has a catch-all parameter.
  /// </summary>
  public bool HasCatchAll =>
    Parameters.Any(p => p.IsCatchAll);

  /// <summary>
  /// Gets whether this route has any optional positional parameters.
  /// </summary>
  public bool HasOptionalPositionalParams =>
    Parameters.Any(p => p.IsOptional && !p.IsCatchAll);
}
