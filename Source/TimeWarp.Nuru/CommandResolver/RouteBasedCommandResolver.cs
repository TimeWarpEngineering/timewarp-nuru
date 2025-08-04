namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
internal static class RouteBasedCommandResolver
{
  public static ResolverResult Resolve(string[] args, EndpointCollection endpoints, ITypeConverterRegistry typeConverterRegistry)
  {
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);

    // Try to match against route endpoints
    (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? matchResult = MatchRoute(args, endpoints);

    if (matchResult is not null)
    {
      (RouteEndpoint endpoint, Dictionary<string, string> extractedValues) = matchResult.Value;

      return new ResolverResult(
        success: true,
        matchedEndpoint: endpoint,
        extractedValues: extractedValues
      );
    }

    return new ResolverResult(
      success: false,
      errorMessage: "No matching command found"
    );
  }

  private static (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? MatchRoute(string[] args, EndpointCollection endpoints)
  {
    var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (RouteEndpoint endpoint in endpoints)
    {
      // Check positional segments
      if (MatchPositionalSegments(endpoint, args, extractedValues, out int consumedArgs))
      {
        // Check if remaining args match required options
        var remainingArgs = new ArraySegment<string>(args, consumedArgs, args.Length - consumedArgs);
        if (CheckRequiredOptions(endpoint, remainingArgs, extractedValues, out int optionsConsumed))
        {
          // For catch-all routes, we don't need to check if all args were consumed
          if (endpoint.ParsedRoute.HasCatchAll)
          {
            return (endpoint, extractedValues);
          }

          // For non-catch-all routes, ensure all arguments were consumed
          int totalConsumed = consumedArgs + optionsConsumed;
          if (totalConsumed == args.Length)
          {
            return (endpoint, extractedValues);
          }
        }
      }
    }

    return null;
  }

  private static bool MatchPositionalSegments(RouteEndpoint endpoint, string[] args,
      Dictionary<string, string> extractedValues, out int consumedArgs)
  {
    consumedArgs = 0;
    IReadOnlyList<RouteSegment> template = endpoint.ParsedRoute.PositionalTemplate;

    // Match each segment in the template
    for (int i = 0; i < template.Count; i++)
    {
      RouteSegment segment = template[i];

      // For catch-all segment (must be last), consume all remaining
      if (segment is ParameterSegment param && param.IsCatchAll)
      {
        consumedArgs = args.Length;
        if (i < args.Length)
        {
          extractedValues[param.Name] = string.Join(CommonStrings.Space, args.Skip(i));
        }

        return true;
      }

      // Regular segment matching
      if (i >= args.Length)
      {
        // Check if this is an optional parameter
        if (segment is ParameterSegment optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter with no value - skip it
          continue;
        }

        return false; // Not enough args
      }

      if (args[i].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // Hit an option - check if current segment is optional
        if (segment is ParameterSegment optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter followed by an option - skip it
          continue;
        }

        return false; // Required parameter but hit an option
      }

      if (!segment.TryMatch(args[i], out string? value))
        return false;

      if (value is not null && segment is ParameterSegment ps)
        extractedValues[ps.Name] = value;

      consumedArgs++;
    }

    return true;
  }

  private static bool CheckRequiredOptions(RouteEndpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues, out int optionsConsumed)
  {
    optionsConsumed = 0;
    IReadOnlyList<OptionSegment> optionSegments = endpoint.ParsedRoute.OptionSegments;

    // If no required options, we're good
    if (optionSegments.Count == 0)
      return true;

    // Check each required option segment
    foreach (OptionSegment optionSegment in optionSegments)
    {
      bool found = false;

      // Look through remaining args for this option
      for (int i = 0; i < remainingArgs.Count; i++)
      {
        string arg = remainingArgs[i];

        if (optionSegment.TryMatch(arg, out _))
        {
          found = true;

          // If this option expects a value, verify one exists and extract it
          if (optionSegment.ExpectsValue)
          {
            if (i + 1 >= remainingArgs.Count || remainingArgs[i + 1].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
            {
              return false;
            }

            // Extract the option value
            if (optionSegment.ValueParameterName is not null)
            {
              extractedValues[optionSegment.ValueParameterName] = remainingArgs[i + 1];
            }

            optionsConsumed += 2; // Option + value
          }
          else
          {
            optionsConsumed++; // Just the option
          }

          break;
        }
      }

      if (!found)
      {
        return false;
      }
    }

    return true;
  }
}
