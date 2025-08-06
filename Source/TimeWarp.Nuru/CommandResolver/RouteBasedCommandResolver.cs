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

    NuruLogger.Matcher.Info($"Resolving command: '{string.Join(" ", args)}'");
    NuruLogger.Matcher.Debug($"Checking {endpoints.Count} available routes");

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

    int endpointIndex = 0;
    foreach (RouteEndpoint endpoint in endpoints)
    {
      endpointIndex++;
      NuruLogger.Matcher.Trace($"[{endpointIndex}/{endpoints.Count}] Checking route: '{endpoint.RoutePattern}'");
      extractedValues.Clear(); // Clear for each attempt

      // Check positional segments
      if (MatchPositionalSegments(endpoint, args, extractedValues, out int consumedArgs))
      {
        // Check if remaining args match required options
        var remainingArgs = new ArraySegment<string>(args, consumedArgs, args.Length - consumedArgs);
        if (CheckRequiredOptions(endpoint, remainingArgs, extractedValues, out int optionsConsumed))
        {
          // For catch-all routes, we don't need to check if all args were consumed
          if (endpoint.CompiledRoute.HasCatchAll)
          {
            NuruLogger.Matcher.Debug($"✓ Matched catch-all route: '{endpoint.RoutePattern}'");
            LogExtractedValues(extractedValues);
            return (endpoint, extractedValues);
          }

          // For non-catch-all routes, ensure all arguments were consumed
          int totalConsumed = consumedArgs + optionsConsumed;
          if (totalConsumed == args.Length)
          {
            NuruLogger.Matcher.Debug($"✓ Matched route: '{endpoint.RoutePattern}'");
            LogExtractedValues(extractedValues);
            return (endpoint, extractedValues);
          }
          else
          {
            NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' consumed only {totalConsumed}/{args.Length} args");
          }
        }
        else
        {
          NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' failed at option matching");
        }
      }
      else
      {
        NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' failed at positional matching");
      }
    }

    NuruLogger.Matcher.Info($"No matching route found for: '{string.Join(" ", args)}'");
    return null;
  }

  private static void LogExtractedValues(Dictionary<string, string> extractedValues)
  {
    if (extractedValues.Count > 0)
    {
      NuruLogger.Matcher.Debug("Extracted values:");
      foreach (KeyValuePair<string, string> kvp in extractedValues)
      {
        NuruLogger.Matcher.Debug($"  {kvp.Key} = '{kvp.Value}'");
      }
    }
  }

  private static bool MatchPositionalSegments(RouteEndpoint endpoint, string[] args,
      Dictionary<string, string> extractedValues, out int consumedArgs)
  {
    consumedArgs = 0;
    IReadOnlyList<RouteMatcher> template = endpoint.CompiledRoute.PositionalMatchers;

    NuruLogger.Matcher.Trace($"Matching {template.Count} positional segments against {args.Length} arguments");

    // Match each segment in the template
    for (int i = 0; i < template.Count; i++)
    {
      RouteMatcher segment = template[i];

      // For catch-all segment (must be last), consume all remaining
      if (segment is ParameterMatcher param && param.IsCatchAll)
      {
        consumedArgs = args.Length;
        if (i < args.Length)
        {
          string catchAllValue = string.Join(CommonStrings.Space, args.Skip(i));
          extractedValues[param.Name] = catchAllValue;
          NuruLogger.Matcher.Trace($"Catch-all parameter '{param.Name}' captured: '{catchAllValue}'");
        }
        else
        {
          NuruLogger.Matcher.Trace($"Catch-all parameter '{param.Name}' has no args to consume");
        }

        return true;
      }

      // Regular segment matching
      if (i >= args.Length)
      {
        // Check if this is an optional parameter
        if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter with no value - skip it
          NuruLogger.Matcher.Trace($"Optional parameter '{optionalParam.Name}' - no value provided");
          continue;
        }

        NuruLogger.Matcher.Trace($"Not enough arguments for segment '{segment.ToDisplayString()}'");
        return false; // Not enough args
      }

      if (args[i].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // Hit an option - check if current segment is optional
        if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter followed by an option - skip it
          NuruLogger.Matcher.Trace($"Optional parameter '{optionalParam.Name}' skipped - hit option '{args[i]}'");
          continue;
        }

        NuruLogger.Matcher.Trace($"Required segment '{segment.ToDisplayString()}' expected but found option '{args[i]}'");
        return false; // Required parameter but hit an option
      }

      string argToMatch = args[i];
      NuruLogger.Matcher.Trace($"Attempting to match '{argToMatch}' against {segment.ToDisplayString()}");

      if (!segment.TryMatch(argToMatch, out string? value))
      {
        NuruLogger.Matcher.Trace($"  Failed to match '{argToMatch}' against {segment.ToDisplayString()}");
        return false;
      }

      if (value is not null && segment is ParameterMatcher ps)
      {
        extractedValues[ps.Name] = value;
        NuruLogger.Matcher.Trace($"  Extracted parameter '{ps.Name}' = '{value}'");
      }
      else if (segment is LiteralMatcher)
      {
        NuruLogger.Matcher.Trace($"  Literal '{segment.ToDisplayString()}' matched");
      }

      consumedArgs++;
    }

    NuruLogger.Matcher.Trace($"Positional matching complete. Consumed {consumedArgs} arguments.");
    return true;
  }

  private static bool CheckRequiredOptions(RouteEndpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues, out int optionsConsumed)
  {
    optionsConsumed = 0;
    IReadOnlyList<OptionMatcher> optionSegments = endpoint.CompiledRoute.OptionMatchers;

    // If no required options, we're good
    if (optionSegments.Count == 0)
      return true;

    // Check each required option segment
    foreach (OptionMatcher optionSegment in optionSegments)
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
            if (optionSegment.ParameterName is not null)
            {
              extractedValues[optionSegment.ParameterName] = remainingArgs[i + 1];
            }

            optionsConsumed += 2; // Option + value
          }
          else
          {
            // Boolean option - no value needed
            if (optionSegment.ParameterName is not null)
            {
              extractedValues[optionSegment.ParameterName] = "true";
              NuruLogger.Matcher.Trace($"Boolean option '{optionSegment.ParameterName}' = true");
            }

            optionsConsumed++; // Just the option
          }

          break;
        }
      }

      if (!found)
      {
        NuruLogger.Matcher.Trace($"Required option not found: {optionSegment.ToDisplayString()}");
        return false;
      }
    }

    NuruLogger.Matcher.Trace($"Options matching complete. Consumed {optionsConsumed} args.");
    return true;
  }
}
