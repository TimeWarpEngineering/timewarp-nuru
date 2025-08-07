namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
internal static class RouteBasedCommandResolver
{
  public static ResolverResult Resolve(string[] args, EndpointCollection endpoints, ITypeConverterRegistry typeConverterRegistry, ILogger? logger = null)
  {
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);

    logger ??= NullLogger.Instance;

    // TODO: Replace with LoggerMessages.ResolvingCommand(logger, string.Join(" ", args), null);
    NuruLogger.Matcher.Info($"Resolving command: '{string.Join(" ", args)}'");
    // TODO: Replace with LoggerMessages.CheckingAvailableRoutes(logger, endpoints.Count, null);
    NuruLogger.Matcher.Debug($"Checking {endpoints.Count} available routes");

    // Try to match against route endpoints
    (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? matchResult = MatchRoute(args, endpoints, logger);

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

  private static (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? MatchRoute(string[] args, EndpointCollection endpoints, ILogger logger)
  {
    var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    int endpointIndex = 0;
    foreach (RouteEndpoint endpoint in endpoints)
    {
      endpointIndex++;
      // TODO: Replace with LoggerMessages.CheckingRoute(logger, endpointIndex, endpoints.Count, endpoint.RoutePattern, null);
      NuruLogger.Matcher.Trace($"[{endpointIndex}/{endpoints.Count}] Checking route: '{endpoint.RoutePattern}'");
      extractedValues.Clear(); // Clear for each attempt

      // Check positional segments
      if (MatchPositionalSegments(endpoint, args, extractedValues, logger, out int consumedArgs))
      {
        // Check if remaining args match required options
        var remainingArgs = new ArraySegment<string>(args, consumedArgs, args.Length - consumedArgs);
        if (CheckRequiredOptions(endpoint, remainingArgs, extractedValues, logger, out int optionsConsumed))
        {
          // For catch-all routes, we don't need to check if all args were consumed
          if (endpoint.CompiledRoute.HasCatchAll)
          {
            // TODO: Replace with LoggerMessages.MatchedCatchAllRoute(logger, endpoint.RoutePattern, null);
            NuruLogger.Matcher.Debug($"✓ Matched catch-all route: '{endpoint.RoutePattern}'");
            LogExtractedValues(extractedValues, logger);
            return (endpoint, extractedValues);
          }

          // For non-catch-all routes, ensure all arguments were consumed
          int totalConsumed = consumedArgs + optionsConsumed;
          if (totalConsumed == args.Length)
          {
            // TODO: Replace with LoggerMessages.MatchedRoute(logger, endpoint.RoutePattern, null);
            NuruLogger.Matcher.Debug($"✓ Matched route: '{endpoint.RoutePattern}'");
            LogExtractedValues(extractedValues, logger);
            return (endpoint, extractedValues);
          }
          else
          {
            // TODO: Replace with LoggerMessages.RouteConsumedPartialArgs(logger, endpoint.RoutePattern, totalConsumed, args.Length, null);
            NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' consumed only {totalConsumed}/{args.Length} args");
          }
        }
        else
        {
          // TODO: Replace with LoggerMessages.RouteFailedAtOptionMatching(logger, endpoint.RoutePattern, null);
          NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' failed at option matching");
        }
      }
      else
      {
        // TODO: Replace with LoggerMessages.RouteFailedAtPositionalMatching(logger, endpoint.RoutePattern, null);
        NuruLogger.Matcher.Trace($"Route '{endpoint.RoutePattern}' failed at positional matching");
      }
    }

    // TODO: Replace with LoggerMessages.NoMatchingRouteFound(logger, string.Join(" ", args), null);
    NuruLogger.Matcher.Info($"No matching route found for: '{string.Join(" ", args)}'");
    return null;
  }

  private static void LogExtractedValues(Dictionary<string, string> extractedValues, ILogger logger)
  {
    _ = logger; // TODO: Remove when logger is actually used
    if (extractedValues.Count > 0)
    {
      // TODO: Replace with LoggerMessages.ExtractedValues(logger, null);
      NuruLogger.Matcher.Debug("Extracted values:");
      foreach (KeyValuePair<string, string> kvp in extractedValues)
      {
        // TODO: Replace with LoggerMessages.ExtractedValue(logger, kvp.Key, kvp.Value, null);
        NuruLogger.Matcher.Debug($"  {kvp.Key} = '{kvp.Value}'");
      }
    }
  }

  private static bool MatchPositionalSegments(RouteEndpoint endpoint, string[] args,
      Dictionary<string, string> extractedValues, ILogger logger, out int consumedArgs)
  {
    _ = logger; // TODO: Remove when logger is actually used
    consumedArgs = 0;
    IReadOnlyList<RouteMatcher> template = endpoint.CompiledRoute.PositionalMatchers;

    // TODO: Replace with LoggerMessages.MatchingPositionalSegments(logger, template.Count, args.Length, null);
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
          // TODO: Replace with LoggerMessages.CatchAllParameterCaptured(logger, param.Name, catchAllValue, null);
          NuruLogger.Matcher.Trace($"Catch-all parameter '{param.Name}' captured: '{catchAllValue}'");
        }
        else
        {
          // TODO: Replace with LoggerMessages.CatchAllParameterNoArgs(logger, param.Name, null);
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
          // TODO: Replace with LoggerMessages.OptionalParameterNoValue(logger, optionalParam.Name, null);
          NuruLogger.Matcher.Trace($"Optional parameter '{optionalParam.Name}' - no value provided");
          continue;
        }

        // TODO: Replace with LoggerMessages.NotEnoughArgumentsForSegment(logger, segment.ToDisplayString(), null);
        NuruLogger.Matcher.Trace($"Not enough arguments for segment '{segment.ToDisplayString()}'");
        return false; // Not enough args
      }

      if (args[i].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // Hit an option - check if current segment is optional
        if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter followed by an option - skip it
          // TODO: Replace with LoggerMessages.OptionalParameterSkippedHitOption(logger, optionalParam.Name, args[i], null);
          NuruLogger.Matcher.Trace($"Optional parameter '{optionalParam.Name}' skipped - hit option '{args[i]}'");
          continue;
        }

        // TODO: Replace with LoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[i], null);
        NuruLogger.Matcher.Trace($"Required segment '{segment.ToDisplayString()}' expected but found option '{args[i]}'");
        return false; // Required parameter but hit an option
      }

      string argToMatch = args[i];
      // TODO: Replace with LoggerMessages.AttemptingToMatch(logger, argToMatch, segment.ToDisplayString(), null);
      NuruLogger.Matcher.Trace($"Attempting to match '{argToMatch}' against {segment.ToDisplayString()}");

      if (!segment.TryMatch(argToMatch, out string? value))
      {
        // TODO: Replace with LoggerMessages.FailedToMatch(logger, argToMatch, segment.ToDisplayString(), null);
        NuruLogger.Matcher.Trace($"  Failed to match '{argToMatch}' against {segment.ToDisplayString()}");
        return false;
      }

      if (value is not null && segment is ParameterMatcher ps)
      {
        extractedValues[ps.Name] = value;
        // TODO: Replace with LoggerMessages.ExtractedParameter(logger, ps.Name, value, null);
        NuruLogger.Matcher.Trace($"  Extracted parameter '{ps.Name}' = '{value}'");
      }
      else if (segment is LiteralMatcher)
      {
        // TODO: Replace with LoggerMessages.LiteralMatched(logger, segment.ToDisplayString(), null);
        NuruLogger.Matcher.Trace($"  Literal '{segment.ToDisplayString()}' matched");
      }

      consumedArgs++;
    }

    // TODO: Replace with LoggerMessages.PositionalMatchingComplete(logger, consumedArgs, null);
    NuruLogger.Matcher.Trace($"Positional matching complete. Consumed {consumedArgs} arguments.");
    return true;
  }

  private static bool CheckRequiredOptions(RouteEndpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues, ILogger logger, out int optionsConsumed)
  {
    _ = logger; // TODO: Remove when logger is actually used
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
              // TODO: Replace with LoggerMessages.BooleanOptionSet(logger, optionSegment.ParameterName, null);
              NuruLogger.Matcher.Trace($"Boolean option '{optionSegment.ParameterName}' = true");
            }

            optionsConsumed++; // Just the option
          }

          break;
        }
      }

      if (!found)
      {
        // TODO: Replace with LoggerMessages.RequiredOptionNotFound(logger, optionSegment.ToDisplayString(), null);
        NuruLogger.Matcher.Trace($"Required option not found: {optionSegment.ToDisplayString()}");
        return false;
      }
    }

    // TODO: Replace with LoggerMessages.OptionsMatchingComplete(logger, optionsConsumed, null);
    NuruLogger.Matcher.Trace($"Options matching complete. Consumed {optionsConsumed} args.");
    return true;
  }
}
