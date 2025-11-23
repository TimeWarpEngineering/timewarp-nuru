namespace TimeWarp.Nuru;

/// <summary>
/// Represents an endpoint that can be matched by a route pattern.
/// Similar to ASP.NET Core's RouteEndpoint but adapted for CLI scenarios.
/// </summary>
public class Endpoint
{
  /// <summary>
  /// Gets or sets the route pattern string (e.g., "git commit --amend").
  /// </summary>
  public required string RoutePattern { get; set; }
  /// <summary>
  /// Gets or sets the compiled representation of the route pattern for efficient matching.
  /// </summary>
  public required CompiledRoute CompiledRoute { get; set; }
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
  [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
  public Type? CommandType { get; set; }

  /// <summary>
  /// Gets the execution strategy for this endpoint based on its configuration.
  /// </summary>
  public ExecutionStrategy Strategy =>
    CommandType is not null ? ExecutionStrategy.Mediator :
    Handler is not null ? ExecutionStrategy.Delegate :
    ExecutionStrategy.Invalid;

  /// <summary>
  /// Gets the help route pattern for this endpoint (e.g., "hello --help").
  /// </summary>
  public string GetHelpRoute()
  {
    // Extract positional literals from the parsed route
    List<string> positionalParts = [];

    foreach (RouteMatcher segment in CompiledRoute.PositionalMatchers)
    {
      if (segment is LiteralMatcher literal)
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
    NuruConsole.Default.WriteLine($"Usage: {RoutePattern}");

    if (!string.IsNullOrEmpty(Description))
    {
      NuruConsole.Default.WriteLine($"\n{Description}");
    }

    // Show positional arguments
    List<ParameterMatcher> positionalParams = [];
    foreach (RouteMatcher segment in CompiledRoute.PositionalMatchers)
    {
      if (segment is ParameterMatcher param)
      {
        positionalParams.Add(param);
      }
    }

    if (positionalParams.Count > 0)
    {
      NuruConsole.Default.WriteLine("\nArguments:");
      foreach (ParameterMatcher param in positionalParams)
      {
        // Check if parameter is optional by looking for '?' in pattern
        bool isOptional = RoutePattern.Contains($"{{{param.Name}?", StringComparison.Ordinal) ||
                         (RoutePattern.Contains($"{{{param.Name}:", StringComparison.Ordinal) && RoutePattern.Contains("?}", StringComparison.Ordinal));
        string status = isOptional ? "(Optional)" : "(Required)";
        string type = param.Constraint ?? "string";
        string description = param.Description ?? "";
        NuruConsole.Default.WriteLine($"  {param.Name,-20} {status,-12} Type: {type,-10} {description}");
      }
    }

    // Show options
    if (CompiledRoute.OptionMatchers.Count > 0)
    {
      NuruConsole.Default.WriteLine("\nOptions:");
      foreach (OptionMatcher option in CompiledRoute.OptionMatchers)
      {
        string optionName = option.MatchPattern.StartsWith("--", StringComparison.Ordinal) ? option.MatchPattern : $"--{option.MatchPattern}";
        if (option.AlternateForm is not null)
        {
          optionName = $"{optionName},{option.AlternateForm}";
        }

        string paramInfo = option.ExpectsValue && option.ParameterName is not null ? $" <{option.ParameterName}>" : "";
        string description = option.Description ?? "";
        NuruConsole.Default.WriteLine($"  {optionName + paramInfo,-30} {description}");
      }
    }
  }
}
