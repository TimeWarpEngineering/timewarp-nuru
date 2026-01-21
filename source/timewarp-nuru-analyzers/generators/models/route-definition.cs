namespace TimeWarp.Nuru.Generators;

using System.Text;

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
/// <param name="Implements">
/// Interface implementations declared via <c>.Implements&lt;T&gt;()</c>.
/// Used for delegate routes to declare filter interfaces for behaviors.
/// </param>
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
  int Order,
  ImmutableArray<InterfaceImplementationDefinition> Implements = default)
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
    int order = 0,
    ImmutableArray<InterfaceImplementationDefinition>? implements = null)
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
      Order: order,
      Implements: implements ?? []);
  }

  /// <summary>
  /// Gets the full pattern including group prefix.
  /// </summary>
  public string FullPattern => string.IsNullOrEmpty(GroupPrefix)
    ? OriginalPattern
    : $"{GroupPrefix} {OriginalPattern}";

  /// <summary>
  /// Gets the effective pattern reconstructed from segments.
  /// This includes all parameters and options extracted from endpoints.
  /// For fluent routes, this matches FullPattern. For endpoints,
  /// this shows the actual route structure with properties.
  /// </summary>
  public string EffectivePattern
  {
    get
    {
      StringBuilder sb = new();

      if (!string.IsNullOrEmpty(GroupPrefix))
      {
        sb.Append(GroupPrefix);
      }

      foreach (SegmentDefinition segment in Segments)
      {
        if (sb.Length > 0)
          sb.Append(' ');

        switch (segment)
        {
          case LiteralDefinition literal:
            sb.Append(literal.Value);
            break;
          case ParameterDefinition param:
            sb.Append(param.PatternSyntax);
            break;
          case OptionDefinition option:
            sb.Append(option.PatternSyntax);
            break;
          case EndOfOptionsSeparatorDefinition:
            sb.Append("--");
            break;
        }
      }

      return sb.ToString();
    }
  }

  /// <summary>
  /// Gets all literal segments in order, including group prefix literals.
  /// </summary>
  public IEnumerable<LiteralDefinition> Literals
  {
    get
    {
      int position = 0;

      // Prepend group prefix as literal(s) if present
      if (!string.IsNullOrEmpty(GroupPrefix))
      {
        foreach (string word in GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
          yield return new LiteralDefinition(position++, word);
        }
      }

      // Then yield original literals from segments (with adjusted positions)
      foreach (LiteralDefinition literal in Segments.OfType<LiteralDefinition>())
      {
        yield return new LiteralDefinition(position++, literal.Value);
      }
    }
  }

  /// <summary>
  /// Gets all segments that require positional matching, in order.
  /// This includes group prefix literals, pattern literals, and end-of-options separators.
  /// Used by the route matcher emitter to generate positional matching code.
  /// </summary>
  public IEnumerable<SegmentDefinition> PositionalMatchSegments
  {
    get
    {
      int position = 0;

      // Prepend group prefix as literal(s) if present
      if (!string.IsNullOrEmpty(GroupPrefix))
      {
        foreach (string word in GroupPrefix.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
          yield return new LiteralDefinition(position++, word);
        }
      }

      // Then yield literals and end-of-options from segments (with adjusted positions)
      foreach (SegmentDefinition segment in Segments)
      {
        switch (segment)
        {
          case LiteralDefinition literal:
            yield return new LiteralDefinition(position++, literal.Value);
            break;
          case EndOfOptionsSeparatorDefinition:
            yield return new EndOfOptionsSeparatorDefinition(position++);
            break;
        }
      }
    }
  }

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

  /// <summary>
  /// Gets whether this route explicitly includes the end-of-options separator (--).
  /// When true, the -- must be present in the input and matched as part of the route.
  /// </summary>
  public bool HasEndOfOptions =>
    Segments.OfType<EndOfOptionsSeparatorDefinition>().Any();

  /// <summary>
  /// Gets the interface type names this route's command implements.
  /// Combines interfaces from <c>.Implements&lt;T&gt;()</c> calls (delegate routes)
  /// and from the command class itself (endpoints).
  /// </summary>
  public IEnumerable<string> ImplementedInterfaces =>
    Implements.IsDefaultOrEmpty
      ? []
      : Implements.Select(i => i.FullInterfaceTypeName);

  /// <summary>
  /// Gets whether this route has any interface implementations.
  /// </summary>
  public bool HasImplements => !Implements.IsDefaultOrEmpty && Implements.Length > 0;
}
