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
  private ImmutableArray<InterfaceImplementationDefinition> Implements = [];

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
  /// Adds an interface implementation (from .Implements&lt;T&gt;() fluent call).
  /// </summary>
  public RouteDefinitionBuilder WithImplements(InterfaceImplementationDefinition implementation)
  {
    Implements = Implements.Add(implementation);
    return this;
  }

  /// <summary>
  /// Sets all interface implementations.
  /// </summary>
  public RouteDefinitionBuilder WithImplements(ImmutableArray<InterfaceImplementationDefinition> implementations)
  {
    Implements = implementations;
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

    // Rebind handler parameters to route segments
    // The handler extractor creates all params as BindingSource.Parameter,
    // but they need to be rebound based on the actual route segments (flags, options, catch-all, etc.)
    HandlerDefinition reboundHandler = RebindHandlerParameters(Handler, Segments);

    return new RouteDefinition
    (
      OriginalPattern: Pattern,
      Segments: Segments,
      MessageType: MessageType,
      Description: Description,
      Handler: reboundHandler,
      Pipeline: Pipeline,
      Aliases: Aliases,
      GroupPrefix: GroupPrefix,
      ComputedSpecificity: Specificity,
      Order: Order,
      Implements: Implements
    );
  }

  /// <summary>
  /// Rebinds handler parameters to route segments.
  /// This converts generic Parameter bindings to specific Flag, Option, or CatchAll bindings.
  /// </summary>
  private static HandlerDefinition RebindHandlerParameters(HandlerDefinition handler, ImmutableArray<SegmentDefinition> segments)
  {
    // Build the handler parameter info for rebinding
    ImmutableArray<(string Name, string TypeName, bool IsOptional, bool IsEnumType)> handlerParams =
    [
      .. handler.Parameters
        .Where(p => p.Source is BindingSource.Parameter or BindingSource.CatchAll or BindingSource.Option or BindingSource.Flag)
        .Select(p => (p.ParameterName, p.ParameterTypeName, p.IsOptional, p.IsEnumType))
    ];

    // Rebind using the route segments
    ImmutableArray<ParameterBinding> reboundParams = PatternStringExtractor.BuildBindings(segments, handlerParams);

    // Merge rebound params with service/cancellation token params (which don't need rebinding)
    List<ParameterBinding> finalParams = [];
    int reboundIndex = 0;

    foreach (ParameterBinding original in handler.Parameters)
    {
      if (original.Source is BindingSource.Service or BindingSource.CancellationToken)
      {
        // Keep service/cancellation token bindings as-is
        finalParams.Add(original);
      }
      else if (reboundIndex < reboundParams.Length)
      {
        // Use the rebound parameter
        finalParams.Add(reboundParams[reboundIndex]);
        reboundIndex++;
      }
      else
      {
        string segmentNames = string.Join(", ", segments.Select(s => s switch
        {
          OptionDefinition o => $"--{o.LongForm ?? o.ShortForm}",
          ParameterDefinition p => $"{{{p.Name}}}",
          LiteralDefinition l => l.Value,
          _ => s.ToString() ?? "?"
        }));
        throw new InvalidOperationException(
          $"Handler parameter '{original.ParameterName}' ({original.ParameterTypeName}) " +
          $"does not match any segment in route [{segmentNames}]. " +
          $"Rebound {reboundParams.Length} of {handlerParams.Length} route parameters.");
      }
    }

    return handler with { Parameters = [.. finalParams] };
  }
}
