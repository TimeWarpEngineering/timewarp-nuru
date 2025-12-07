namespace TimeWarp.Nuru;

/// <summary>
/// Generates automatic help routes for registered endpoints.
/// </summary>
internal static class HelpRouteGenerator
{
  /// <summary>
  /// Generates help routes for all registered endpoints.
  /// </summary>
  /// <param name="builder">The NuruAppBuilder to add help routes to.</param>
  /// <param name="endpointCollection">The collection of registered endpoints.</param>
  /// <param name="appMetadata">Optional application metadata for help display.</param>
  /// <param name="helpOptions">Optional help options for filtering.</param>
  internal static void GenerateHelpRoutes(
    NuruCoreAppBuilder builder,
    EndpointCollection endpointCollection,
    ApplicationMetadata? appMetadata,
    HelpOptions? helpOptions = null)
  {
    // Get a snapshot of existing endpoints (before we add help routes)
    List<Endpoint> existingEndpoints = [.. endpointCollection.Endpoints];

    // Group endpoints by their command prefix
    Dictionary<string, List<Endpoint>> commandGroups = [];

    foreach (Endpoint endpoint in existingEndpoints)
    {
      string commandPrefix = GetCommandPrefix(endpoint);

      if (!commandGroups.TryGetValue(commandPrefix, out List<Endpoint>? group))
      {
        group = [];
        commandGroups[commandPrefix] = group;
      }

      group.Add(endpoint);
    }

    // Add help routes for each command group
    foreach ((string prefix, List<Endpoint> endpoints) in commandGroups)
    {
      if (string.IsNullOrEmpty(prefix))
      {
        // Skip empty prefix - will be handled by base --help
        continue;
      }

      // Use --help? (optional) so help routes don't outrank user routes with optional flags
      // When both have same specificity, user routes win due to insertion order
      string helpRoute = $"{prefix} --help?";
      string description = $"Show help for {prefix} command";

      // Only add if not already present (check both --help and --help? patterns)
      if (!existingEndpoints.Any(e => e.RoutePattern == helpRoute || e.RoutePattern == $"{prefix} --help"))
      {
        // Capture endpoints by value to avoid issues with collection modification
        List<Endpoint> capturedEndpoints = [.. endpoints];
        builder.Map(helpRoute, () => GetCommandGroupHelpText(prefix, capturedEndpoints), description);
      }
    }

    // Add base --help route if not already present
    // Use --help? (optional) so help routes don't outrank user routes with optional flags
    // CLI context - may filter REPL commands based on options
    if (!existingEndpoints.Any(e => e.RoutePattern == "--help" || e.RoutePattern == "--help?"))
    {
      builder.Map("--help?", () => HelpProvider.GetHelpText(endpointCollection, appMetadata?.Name, appMetadata?.Description, helpOptions, HelpContext.Cli),
      description: "Show available commands");
    }

    // Add base help route if not already present (REPL-friendly)
    // REPL context - always shows REPL commands
    if (!existingEndpoints.Any(e => e.RoutePattern == "help"))
    {
      builder.Map("help", () => HelpProvider.GetHelpText(endpointCollection, appMetadata?.Name, appMetadata?.Description, helpOptions, HelpContext.Repl),
      description: "Show available commands");
    }
  }

  private static string GetCommandGroupHelpText(string commandPrefix, List<Endpoint> endpoints)
  {
    StringBuilder sb = new();

    sb.AppendLine(CultureInfo.InvariantCulture, $"Usage patterns for '{commandPrefix}':");
    sb.AppendLine();

    foreach (Endpoint endpoint in endpoints)
    {
      sb.AppendLine(CultureInfo.InvariantCulture, $"  {endpoint.RoutePattern}");
      if (!string.IsNullOrEmpty(endpoint.Description))
      {
        sb.AppendLine(CultureInfo.InvariantCulture, $"    {endpoint.Description}");
      }
    }

    // Show consolidated argument and option information
    HashSet<string> shownParams = [];

    sb.AppendLine("\nArguments:");
    foreach (Endpoint endpoint in endpoints)
    {
      foreach (RouteMatcher segment in endpoint.CompiledRoute.PositionalMatchers)
      {
        if (segment is ParameterMatcher param && shownParams.Add(param.Name))
        {
          bool isOptional = endpoint.RoutePattern.Contains($"{{{param.Name}?", StringComparison.Ordinal) ||
                           (endpoint.RoutePattern.Contains($"{{{param.Name}:", StringComparison.Ordinal) &&
                            endpoint.RoutePattern.Contains("?}", StringComparison.Ordinal));
          string status = isOptional ? "(Optional)" : "(Required)";
          string typeInfo = $"Type: {param.Constraint ?? "string"}";
          if (param.Description is not null)
          {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {param.Name,-20} {status,-12} {typeInfo,-15} {param.Description}");
          }
          else
          {
            sb.AppendLine(CultureInfo.InvariantCulture, $"  {param.Name,-20} {status,-12} {typeInfo}");
          }
        }
      }
    }

    HashSet<string> shownOptions = [];

    if (endpoints.Any(e => e.CompiledRoute.OptionMatchers.Count > 0))
    {
      sb.AppendLine("\nOptions:");
      foreach (Endpoint endpoint in endpoints)
      {
        foreach (OptionMatcher option in endpoint.CompiledRoute.OptionMatchers)
        {
          if (shownOptions.Add(option.MatchPattern))
          {
            string optionName = option.MatchPattern.StartsWith("--", StringComparison.Ordinal) ? option.MatchPattern : $"--{option.MatchPattern}";
            if (option.AlternateForm is not null)
            {
              optionName = $"{optionName},{option.AlternateForm}";
            }

            string paramInfo = option.ExpectsValue && option.ParameterName is not null ? $" <{option.ParameterName}>" : "";

            if (option.Description is not null)
            {
              sb.AppendLine(CultureInfo.InvariantCulture, $"  {optionName + paramInfo,-30} {option.Description}");
            }
            else
            {
              sb.AppendLine(CultureInfo.InvariantCulture, $"  {optionName}{paramInfo}");
            }
          }
        }
      }
    }

    return sb.ToString().TrimEnd();
  }

  private static string GetCommandPrefix(Endpoint endpoint)
  {
    List<string> parts = [];

    foreach (RouteMatcher segment in endpoint.CompiledRoute.PositionalMatchers)
    {
      if (segment is LiteralMatcher literal)
      {
        parts.Add(literal.Value);
      }
      else
      {
        // Stop at first parameter
        break;
      }
    }

    return string.Join(" ", parts);
  }
}
