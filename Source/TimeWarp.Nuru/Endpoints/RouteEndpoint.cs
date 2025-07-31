namespace TimeWarp.Nuru.Endpoints;

/// <summary>
/// Represents an endpoint that can be matched by a route pattern.
/// Similar to ASP.NET Core's RouteEndpoint but adapted for CLI scenarios.
/// </summary>
public class RouteEndpoint
{
  /// <summary>
  /// Gets or sets the route pattern string (e.g., "git commit --amend").
  /// </summary>
  public required string RoutePattern { get; set; }
  /// <summary>
  /// Gets or sets the parsed representation of the route pattern for efficient matching.
  /// </summary>
  public required ParsedRoute ParsedRoute { get; set; }
  /// <summary>
  /// Gets or sets the handler delegate that will be invoked when this endpoint is matched.
  /// For Mediator commands, this will be null and CommandType will be used instead.
  /// </summary>
  public Delegate? Handler { get; set; }
  /// <summary>
  /// Gets or sets the method info for the handler (used for parameter binding).
  /// For Mediator commands, this will be null.
  /// </summary>
  public MethodInfo? Method { get; set; }
  /// <summary>
  /// Gets or sets the order/specificity of this route (higher values match first).
  /// </summary>
  public int Order { get; set; }
  /// <summary>
  /// Gets or sets a user-friendly description of this endpoint.
  /// </summary>
  public string? Description { get; set; }
  /// <summary>
  /// Gets or sets the command type if this is a command-based route (using Mediator pattern).
  /// If null, this is a delegate-based route.
  /// </summary>
  public Type? CommandType { get; set; }

  /// <summary>
  /// Gets the help route pattern for this endpoint (e.g., "hello --help").
  /// </summary>
  public string GetHelpRoute()
  {
    // Extract positional literals from the parsed route
    List<string> positionalParts = [];

    foreach (RouteSegment segment in ParsedRoute.PositionalTemplate)
    {
      if (segment is LiteralSegment literal)
      {
        positionalParts.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter - we only want the command prefix
        break;
      }
    }

    // If no positional parts, just return --help
    if (positionalParts.Count == 0)
    {
      return "--help";
    }

    // Otherwise, append --help to the command prefix
    return $"{string.Join(" ", positionalParts)} --help";
  }

  /// <summary>
  /// Displays help information for this endpoint.
  /// </summary>
  public void ShowHelp()
  {
    Console.WriteLine($"Usage: {RoutePattern}");

    if (!string.IsNullOrEmpty(Description))
    {
      Console.WriteLine($"\n{Description}");
    }

    // Show positional arguments
    List<ParameterSegment> positionalParams = [];
    foreach (RouteSegment segment in ParsedRoute.PositionalTemplate)
    {
      if (segment is ParameterSegment param)
      {
        positionalParams.Add(param);
      }
    }

    if (positionalParams.Count > 0)
    {
      Console.WriteLine("\nArguments:");
      foreach (ParameterSegment param in positionalParams)
      {
        // Check if parameter is optional by looking for '?' in pattern
        bool isOptional = RoutePattern.Contains($"{{{param.Name}?", StringComparison.Ordinal) ||
                         (RoutePattern.Contains($"{{{param.Name}:", StringComparison.Ordinal) && RoutePattern.Contains("?}", StringComparison.Ordinal));
        string status = isOptional ? "(Optional)" : "(Required)";
        string type = param.Constraint ?? "string";
        string description = param.Description ?? "";
        Console.WriteLine($"  {param.Name,-20} {status,-12} Type: {type,-10} {description}");
      }
    }

    // Show options
    if (ParsedRoute.OptionSegments.Count > 0)
    {
      Console.WriteLine("\nOptions:");
      foreach (OptionSegment option in ParsedRoute.OptionSegments)
      {
        string optionName = option.Name.StartsWith("--", StringComparison.Ordinal) ? option.Name : $"--{option.Name}";
        if (option.ShortAlias is not null)
        {
          optionName = $"{optionName},{option.ShortAlias}";
        }

        string paramInfo = option.ExpectsValue && option.ValueParameterName is not null ? $" <{option.ValueParameterName}>" : "";
        string description = option.Description ?? "";
        Console.WriteLine($"  {optionName + paramInfo,-30} {description}");
      }
    }
  }
}
