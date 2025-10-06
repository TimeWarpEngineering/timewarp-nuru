namespace TimeWarp.Nuru;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
internal static class EndpointResolver
{
  public static EndpointResolutionResult Resolve
  (
    string[] args,
    EndpointCollection endpoints,
    ITypeConverterRegistry typeConverterRegistry,
    ILogger? logger = null
  )
  {
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);

    logger ??= NullLogger.Instance;

    LoggerMessages.ResolvingCommand(logger, string.Join(" ", args), null);
    LoggerMessages.CheckingAvailableRoutes(logger, endpoints.Count, null);

    // Try to match against route endpoints
    (Endpoint endpoint, Dictionary<string, string> extractedValues)? matchResult =
      MatchRoute(args, endpoints, logger);

    if (matchResult is not null)
    {
      (Endpoint endpoint, Dictionary<string, string> extractedValues) = matchResult.Value;

      return new EndpointResolutionResult
      (
        success: true,
        matchedEndpoint: endpoint,
        extractedValues: extractedValues
      );
    }

    return new EndpointResolutionResult
    (
      success: false,
      errorMessage: "No matching command found"
    );
  }

  private static (Endpoint endpoint, Dictionary<string, string> extractedValues)?
    MatchRoute(string[] args, EndpointCollection endpoints, ILogger logger)
  {
    var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    int endpointIndex = 0;
    foreach (Endpoint endpoint in endpoints)
    {
      endpointIndex++;
      LoggerMessages.CheckingRoute(logger, endpointIndex, endpoints.Count, endpoint.RoutePattern, null);
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
            LoggerMessages.MatchedCatchAllRoute(logger, endpoint.RoutePattern, null);
            LogExtractedValues(extractedValues, logger);
            return (endpoint, extractedValues);
          }

          // For non-catch-all routes, ensure all arguments were consumed
          int totalConsumed = consumedArgs + optionsConsumed;
          if (totalConsumed == args.Length)
          {
            LoggerMessages.MatchedRoute(logger, endpoint.RoutePattern, null);
            LogExtractedValues(extractedValues, logger);
            return (endpoint, extractedValues);
          }
          else
          {
            LoggerMessages.RouteConsumedPartialArgs(logger, endpoint.RoutePattern, totalConsumed, args.Length, null);
          }
        }
        else
        {
          LoggerMessages.RouteFailedAtOptionMatching(logger, endpoint.RoutePattern, null);
        }
      }
      else
      {
        LoggerMessages.RouteFailedAtPositionalMatching(logger, endpoint.RoutePattern, null);
      }
    }

    LoggerMessages.NoMatchingRouteFound(logger, string.Join(" ", args), null);
    return null;
  }

  private static void LogExtractedValues(Dictionary<string, string> extractedValues, ILogger logger)
  {
    if (extractedValues.Count > 0)
    {
      LoggerMessages.ExtractedValues(logger, null);
      foreach (KeyValuePair<string, string> kvp in extractedValues)
      {
        LoggerMessages.ExtractedValue(logger, kvp.Key, kvp.Value, null);
      }
    }
  }

  private static bool MatchPositionalSegments
  (
    Endpoint endpoint,
    string[] args,
    Dictionary<string, string> extractedValues,
    ILogger logger,
    out int consumedArgs
  )
  {
    consumedArgs = 0;
    IReadOnlyList<RouteMatcher> template = endpoint.CompiledRoute.PositionalMatchers;

    LoggerMessages.MatchingPositionalSegments(logger, template.Count, args.Length, null);

    // Track if we've seen the end-of-options separator
    bool seenEndOfOptions = false;

    // Match each segment in the template
    for (int i = 0; i < template.Count; i++)
    {
      RouteMatcher segment = template[i];

      // Check if current segment is the end-of-options separator "--"
      if (segment is LiteralMatcher literal && literal.Value == "--")
      {
        seenEndOfOptions = true;
      }

      // For catch-all segment (must be last), consume positional args until we hit a defined option
      if (segment is ParameterMatcher param && param.IsCatchAll)
      {
        consumedArgs = HandleCatchAllSegment(
          param,
          args,
          consumedArgs,
          endpoint.CompiledRoute.OptionMatchers,
          extractedValues,
          logger
        );
        return true;
      }

      // Validate argument availability and check for optional skipping
      SegmentValidationResult validationResult = ValidateSegmentAvailability(
        segment,
        args,
        i,
        seenEndOfOptions,
        logger
      );

      if (validationResult == SegmentValidationResult.Skip)
      {
        continue;
      }

      if (validationResult == SegmentValidationResult.Fail)
      {
        return false;
      }

      // Match the segment against the argument
      if (!MatchRegularSegment(segment, args[i], extractedValues, logger))
      {
        return false;
      }

      consumedArgs++;
    }

    LoggerMessages.PositionalMatchingComplete(logger, consumedArgs, null);
    return true;
  }

  private enum SegmentValidationResult
  {
    Proceed,  // Argument is available and should be matched
    Skip,     // Skip this segment (optional parameter)
    Fail      // Validation failed, stop matching
  }

  private static SegmentValidationResult ValidateSegmentAvailability
  (
    RouteMatcher segment,
    string[] args,
    int argIndex,
    bool seenEndOfOptions,
    ILogger logger
  )
  {
    // Check if we have enough arguments
    if (argIndex >= args.Length)
    {
      // Check if this is an optional parameter
      if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
      {
        // Optional parameter with no value - skip it
        LoggerMessages.OptionalParameterNoValue(logger, optionalParam.Name, null);
        return SegmentValidationResult.Skip;
      }

      LoggerMessages.NotEnoughArgumentsForSegment(logger, segment.ToDisplayString(), null);
      return SegmentValidationResult.Fail;
    }

    // After --, don't check for options - everything is treated as positional arguments
    if (!seenEndOfOptions && args[argIndex].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
    {
      // Hit an option - check if current segment is optional
      if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
      {
        // Optional parameter followed by an option - skip it
        LoggerMessages.OptionalParameterSkippedHitOption(logger, optionalParam.Name, args[argIndex], null);
        return SegmentValidationResult.Skip;
      }

      LoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[argIndex], null);
      return SegmentValidationResult.Fail;
    }

    return SegmentValidationResult.Proceed;
  }

  private static bool MatchRegularSegment
  (
    RouteMatcher segment,
    string arg,
    Dictionary<string, string> extractedValues,
    ILogger logger
  )
  {
    LoggerMessages.AttemptingToMatch(logger, arg, segment.ToDisplayString(), null);

    if (!segment.TryMatch(arg, out string? value))
    {
      LoggerMessages.FailedToMatch(logger, arg, segment.ToDisplayString(), null);
      return false;
    }

    if (value is not null && segment is ParameterMatcher ps)
    {
      extractedValues[ps.Name] = value;
      LoggerMessages.ExtractedParameter(logger, ps.Name, value, null);
    }
    else if (segment is LiteralMatcher)
    {
      LoggerMessages.LiteralMatched(logger, segment.ToDisplayString(), null);
    }

    return true;
  }

  private static int HandleCatchAllSegment
  (
    ParameterMatcher param,
    string[] args,
    int startPosition,
    IReadOnlyList<OptionMatcher> optionMatchers,
    Dictionary<string, string> extractedValues,
    ILogger logger
  )
  {
    // Collect positional arguments until we encounter a defined option from the route
    var catchAllArgs = new List<string>();
    int j = startPosition;
    while (j < args.Length)
    {
      string arg = args[j];
      // Stop if we encounter a defined option in the route pattern
      if (arg.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) &&
          IsDefinedOption(arg, optionMatchers))
      {
        break;
      }

      catchAllArgs.Add(arg);
      j++;
    }

    // Always store catch-all value (even if empty) so parameter binding succeeds
    string catchAllValue = catchAllArgs.Count > 0
      ? string.Join(CommonStrings.Space, catchAllArgs)
      : string.Empty;
    extractedValues[param.Name] = catchAllValue;

    if (catchAllArgs.Count > 0)
    {
      LoggerMessages.CatchAllParameterCaptured(logger, param.Name, catchAllValue, null);
    }
    else
    {
      LoggerMessages.CatchAllParameterNoArgs(logger, param.Name, null);
    }

    return j; // Return position after all catch-all args
  }

  private static bool IsDefinedOption(string arg, IReadOnlyList<OptionMatcher> optionMatchers)
  {
    foreach (OptionMatcher option in optionMatchers)
    {
      if (option.TryMatch(arg, out _))
      {
        return true;
      }
    }

    return false;
  }

  private static bool CheckRequiredOptions(Endpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues, ILogger logger, out int optionsConsumed)
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
      var collectedValues = new List<string>(); // For repeated options

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
            bool valueIsAvailable = i + 1 < remainingArgs.Count &&
                                    !remainingArgs[i + 1].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal);

            if (!valueIsAvailable)
            {
              // No value available - check if value is optional
              if (!optionSegment.ParameterIsOptional)
              {
                // Value is required but not provided
                return false;
              }
              // Value is optional and not provided - that's OK, parameter will be null
              optionsConsumed++; // Just the option flag
            }
            else
            {
              // Value is available - extract it
              if (optionSegment.ParameterName is not null)
              {
                if (optionSegment.IsRepeated)
                {
                  // For repeated options, collect values
                  collectedValues.Add(remainingArgs[i + 1]);
                }
                else
                {
                  // Single value option
                  extractedValues[optionSegment.ParameterName] = remainingArgs[i + 1];
                }
              }

              optionsConsumed += 2; // Option + value
            }
          }
          else
          {
            // Boolean option - no value needed
            if (optionSegment.ParameterName is not null)
            {
              extractedValues[optionSegment.ParameterName] = "true";
              LoggerMessages.BooleanOptionSet(logger, optionSegment.ParameterName, null);
            }

            optionsConsumed++; // Just the option
          }

          // For repeated options, continue looking for more occurrences
          if (!optionSegment.IsRepeated)
          {
            break;
          }
        }
      }

      // For repeated options, store the collected values
      if (optionSegment.IsRepeated && collectedValues.Count > 0 && optionSegment.ParameterName is not null)
      {
        // Store as space-separated for now (will be split by parameter binder)
        extractedValues[optionSegment.ParameterName] = string.Join(" ", collectedValues);
      }

      if (!found)
      {
        // Check if this option is optional
        if (optionSegment.IsOptional)
        {
          // Optional option not provided - that's OK, just set it to false/null
          if (optionSegment.ParameterName is not null)
          {
            // For boolean options, set to "false" when not provided
            if (!optionSegment.ExpectsValue)
            {
              extractedValues[optionSegment.ParameterName] = "false";
              LoggerMessages.OptionalBooleanOptionNotProvided(logger, optionSegment.ParameterName, null);
            }
            // For value options, the parameter will be null (handled by binding)
            else
            {
              LoggerMessages.OptionalValueOptionNotProvided(logger, optionSegment.ParameterName, null);
            }
          }

          continue; // Skip to next option
        }

        LoggerMessages.RequiredOptionNotFound(logger, optionSegment.ToDisplayString(), null);
        return false;
      }
    }

    LoggerMessages.OptionsMatchingComplete(logger, optionsConsumed, null);
    return true;
  }
}
