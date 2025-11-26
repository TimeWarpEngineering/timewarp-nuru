namespace TimeWarp.Nuru;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
internal static class EndpointResolver
{
  /// <summary>
  /// Represents a route match with quality metrics for ranking.
  /// </summary>
  private record RouteMatch(
    Endpoint Endpoint,
    Dictionary<string, string> ExtractedValues,
    int DefaultsUsed  // Count of optional parameters that used default values
  );
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
    var matches = new List<RouteMatch>();
    var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    int endpointIndex = 0;
    foreach (Endpoint endpoint in endpoints)
    {
      endpointIndex++;
      LoggerMessages.CheckingRoute(logger, endpointIndex, endpoints.Count, endpoint.RoutePattern, null);
      extractedValues.Clear(); // Clear for each attempt

      // Match all segments sequentially (literals, parameters, and options)
      if (MatchSegments(endpoint, args, extractedValues, logger, out int consumedArgs, out int defaultsUsed, out int totalConsumed))
      {
        // For catch-all routes, we don't need to check if all args were consumed
        if (endpoint.CompiledRoute.HasCatchAll)
        {
          LoggerMessages.MatchedCatchAllRoute(logger, endpoint.RoutePattern, null);
          LogExtractedValues(extractedValues, logger);
          // Store this match and continue checking other routes
          matches.Add(new RouteMatch(endpoint, new Dictionary<string, string>(extractedValues, StringComparer.OrdinalIgnoreCase), defaultsUsed));
          continue;
        }

        // For non-catch-all routes, ensure all arguments were consumed
        if (totalConsumed == args.Length)
        {
          LoggerMessages.MatchedRoute(logger, endpoint.RoutePattern, null);
          LogExtractedValues(extractedValues, logger);
          // Store this match and continue checking other routes
          matches.Add(new RouteMatch(endpoint, new Dictionary<string, string>(extractedValues, StringComparer.OrdinalIgnoreCase), defaultsUsed));
        }
        else
        {
          LoggerMessages.RouteConsumedPartialArgs(logger, endpoint.RoutePattern, totalConsumed, args.Length, null);
        }
      }
      else
      {
        LoggerMessages.RouteFailedAtPositionalMatching(logger, endpoint.RoutePattern, null);
      }
    }

    // If no matches found, return null
    if (matches.Count == 0)
    {
      LoggerMessages.NoMatchingRouteFound(logger, string.Join(" ", args), null);
      return null;
    }

    // Select the best match:
    // 1. Prefer exact matches (0 defaults) over partial matches
    // 2. Among partial matches, prefer fewer defaults
    // 3. Among matches with same defaults, prefer higher specificity (already sorted by endpoints collection)
    RouteMatch bestMatch = matches
      .OrderBy(m => m.DefaultsUsed)  // Fewer defaults is better
      .ThenByDescending(m => m.Endpoint.CompiledRoute.Specificity)  // Higher specificity is better
      .First();

    return (bestMatch.Endpoint, bestMatch.ExtractedValues);
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

  private static bool MatchSegments
  (
    Endpoint endpoint,
    string[] args,
    Dictionary<string, string> extractedValues,
    ILogger logger,
    out int consumedArgs,
    out int defaultsUsed,
    out int totalConsumed
  )
  {
    consumedArgs = 0;
    defaultsUsed = 0;
    IReadOnlyList<RouteMatcher> template = endpoint.CompiledRoute.Segments;

    LoggerMessages.MatchingPositionalSegments(logger, template.Count, args.Length, null);

    // Pre-pass: Handle repeated options first and mark consumed indices
    var consumedIndices = new HashSet<int>();
    var repeatedOptions = template.OfType<OptionMatcher>().Where(o => o.IsRepeated).ToList();

    foreach (OptionMatcher repeatedOption in repeatedOptions)
    {
      if (!MatchRepeatedOptionWithIndices(repeatedOption, args, consumedIndices, extractedValues, logger, ref defaultsUsed))
      {
        totalConsumed = consumedIndices.Count;
        return false;
      }
    }

    // Track if we've seen the end-of-options separator
    bool seenEndOfOptions = false;

    // Match each segment in the template
    for (int i = 0; i < template.Count; i++)
    {
      RouteMatcher segment = template[i];

      // Skip repeated options - already handled in pre-pass
      if (segment is OptionMatcher { IsRepeated: true })
      {
        continue;
      }

      // Check if current segment is the end-of-options separator "--"
      if (segment is LiteralMatcher literal && literal.Value == "--")
      {
        seenEndOfOptions = true;
      }

      // Handle non-repeated option segments
      if (segment is OptionMatcher option)
      {
        if (!MatchOptionSegment(option, args, extractedValues, seenEndOfOptions, endpoint.CompiledRoute.OptionMatchers, logger, ref defaultsUsed, consumedIndices))
        {
          totalConsumed = consumedIndices.Count;
          return false;
        }

        continue; // Option handled, move to next segment
      }

      // For catch-all segment (must be last), consume remaining positional args
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
        // Mark all consumed args
        for (int idx = 0; idx < consumedArgs; idx++)
        {
          consumedIndices.Add(idx);
        }

        totalConsumed = consumedIndices.Count;
        return true;
      }

      // Skip args that were consumed by repeated options
      while (consumedArgs < args.Length && consumedIndices.Contains(consumedArgs))
      {
        consumedArgs++;
      }

      // Validate argument availability for positional segments
      SegmentValidationResult validationResult = ValidateSegmentAvailability(
        segment,
        args,
        consumedArgs,
        seenEndOfOptions,
        endpoint.CompiledRoute.OptionMatchers,
        logger
      );

      if (validationResult == SegmentValidationResult.Skip)
      {
        continue;
      }

      if (validationResult == SegmentValidationResult.Fail)
      {
        totalConsumed = consumedIndices.Count;
        return false;
      }

      // Match the positional segment against the argument
      if (!MatchRegularSegment(segment, args[consumedArgs], extractedValues, logger))
      {
        totalConsumed = consumedIndices.Count;
        return false;
      }

      consumedIndices.Add(consumedArgs); // Track this index as consumed
      consumedArgs++;
    }

    totalConsumed = consumedIndices.Count;
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
    IReadOnlyList<OptionMatcher> optionMatchers,
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
      // Check if this argument matches a DEFINED option in the route pattern
      if (IsDefinedOption(args[argIndex], optionMatchers))
      {
        // It's a defined option - check if current segment is optional
        if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter followed by an option - skip it
          LoggerMessages.OptionalParameterSkippedHitOption(logger, optionalParam.Name, args[argIndex], null);
          return SegmentValidationResult.Skip;
        }

        LoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[argIndex], null);
        return SegmentValidationResult.Fail;
      }

      // Not a defined option - treat as positional value
      // This allows: negative numbers (-3), literals (-sometext), etc.
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

  private static bool MatchOptionSegment
  (
    OptionMatcher option,
    string[] args,
    Dictionary<string, string> extractedValues,
    bool seenEndOfOptions,
    IReadOnlyList<OptionMatcher> optionMatchers,
    ILogger logger,
    ref int defaultsUsed,
    HashSet<int> consumedIndices
  )
  {
    // After --, don't match options - everything is positional
    if (seenEndOfOptions)
    {
      // Option appears in pattern after --, but we're in positional mode
      // This is a pattern error, but treat as optional option not found
      if (option.IsOptional)
      {
        defaultsUsed++;
        SetDefaultOptionValue(option, extractedValues, logger);
        return true;
      }

      LoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
      return false;
    }

    // Search through ALL unconsumed arguments for this option (position-independent)
    // This allows options to appear in any order AFTER positional arguments
    int foundIndex = -1;
    for (int i = 0; i < args.Length; i++)
    {
      // Skip already consumed indices
      if (consumedIndices.Contains(i))
        continue;

      if (option.TryMatch(args[i], out _))
      {
        foundIndex = i;
        break;
      }
    }

    if (foundIndex >= 0)
    {
      // Option found at foundIndex
      consumedIndices.Add(foundIndex); // Mark option flag as consumed

      if (option.ExpectsValue)
      {
        // Option expects a value - look at the next arg after the option
        int valueIndex = foundIndex + 1;

        // Check if next arg is available and is NOT a defined option
        bool valueIsAvailable = valueIndex < args.Length &&
                                !consumedIndices.Contains(valueIndex) &&
                                (!args[valueIndex].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) ||
                                 !IsDefinedOption(args[valueIndex], optionMatchers));

        if (!valueIsAvailable)
        {
          // No value available
          if (!option.ParameterIsOptional)
          {
            // Value is required but not provided
            LoggerMessages.RequiredOptionValueNotProvided(logger, option.ToDisplayString(), null);
            return false;
          }
          // Value is optional and not provided - parameter will be null
          LoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName ?? "", null);
        }
        else
        {
          // Value is available - extract it
          if (option.ParameterName is not null)
          {
            extractedValues[option.ParameterName] = args[valueIndex];
            LoggerMessages.ExtractedParameter(logger, option.ParameterName, args[valueIndex], null);
          }

          consumedIndices.Add(valueIndex); // Mark option value as consumed
        }
      }
      else
      {
        // Boolean option - no value needed
        if (option.ParameterName is not null)
        {
          extractedValues[option.ParameterName] = "true";
          LoggerMessages.BooleanOptionSet(logger, option.ParameterName, null);
        }
      }

      return true;
    }

    // Option not found anywhere in unconsumed arguments
    if (option.IsOptional)
    {
      // Optional option not provided
      defaultsUsed++;
      SetDefaultOptionValue(option, extractedValues, logger);
      return true;
    }

    // Required option not found
    LoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
    return false;
  }

  private static bool MatchRepeatedOptionWithIndices
  (
    OptionMatcher option,
    string[] args,
    HashSet<int> consumedIndices,
    Dictionary<string, string> extractedValues,
    ILogger logger,
    ref int defaultsUsed
  )
  {
    var collectedValues = new List<string>();

    // Scan all args for occurrences of this repeated option
    for (int i = 0; i < args.Length; i++)
    {
      // Skip if already consumed
      if (consumedIndices.Contains(i))
        continue;

      // Check if current arg matches the option
      if (option.TryMatch(args[i], out _))
      {
        consumedIndices.Add(i); // Mark this index as consumed

        if (option.ExpectsValue)
        {
          // Option expects a value
          bool valueIsAvailable = i + 1 < args.Length &&
                                  !args[i + 1].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal);

          if (!valueIsAvailable)
          {
            // No value available
            if (!option.ParameterIsOptional)
            {
              // Value is required but not provided
              LoggerMessages.RequiredOptionValueNotProvided(logger, option.ToDisplayString(), null);
              return false;
            }
            // Value is optional - skip this occurrence
          }
          else
          {
            // Collect the value and mark its index as consumed
            collectedValues.Add(args[i + 1]);
            consumedIndices.Add(i + 1);
          }
        }
        else
        {
          // Boolean repeated option - just count occurrences
          collectedValues.Add("true");
        }
      }
    }

    // Store collected values
    if (collectedValues.Count > 0 && option.ParameterName is not null)
    {
      // Store as space-separated (will be split by parameter binder)
      extractedValues[option.ParameterName] = string.Join(" ", collectedValues);
      return true;
    }

    // No occurrences found
    if (option.IsOptional)
    {
      defaultsUsed++;
      SetDefaultOptionValue(option, extractedValues, logger);
      return true;
    }

    LoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
    return false;
  }

  private static void SetDefaultOptionValue
  (
    OptionMatcher option,
    Dictionary<string, string> extractedValues,
    ILogger logger
  )
  {
    if (option.ParameterName is null)
      return;

    if (!option.ExpectsValue)
    {
      // Boolean option defaults to false
      extractedValues[option.ParameterName] = "false";
      LoggerMessages.OptionalBooleanOptionNotProvided(logger, option.ParameterName, null);
    }
    else if (option.IsRepeated)
    {
      // Repeated option defaults to empty array
      extractedValues[option.ParameterName] = "";
      LoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName, null);
    }
    else
    {
      // Other value options default to null (handled by binding)
      LoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName, null);
    }
  }

  private static bool CheckRequiredOptions(Endpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues, ILogger logger, out int optionsConsumed, out int defaultsUsed)
  {
    optionsConsumed = 0;
    defaultsUsed = 0;
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
          // Optional option not provided - that's OK, just set it to false/null/empty
          defaultsUsed++; // Track that we're using a default value
          if (optionSegment.ParameterName is not null)
          {
            // For boolean options, set to "false" when not provided
            if (!optionSegment.ExpectsValue)
            {
              extractedValues[optionSegment.ParameterName] = "false";
              LoggerMessages.OptionalBooleanOptionNotProvided(logger, optionSegment.ParameterName, null);
            }
            // For repeated options, set to empty string (will be parsed as empty array)
            else if (optionSegment.IsRepeated)
            {
              extractedValues[optionSegment.ParameterName] = "";
              LoggerMessages.OptionalValueOptionNotProvided(logger, optionSegment.ParameterName, null);
            }
            // For other value options, the parameter will be null (handled by binding)
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
