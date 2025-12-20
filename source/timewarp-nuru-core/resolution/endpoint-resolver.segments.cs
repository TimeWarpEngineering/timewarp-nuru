namespace TimeWarp.Nuru;

/// <summary>
/// EndpointResolver - segment matching logic for literals, parameters, and validation.
/// </summary>
internal static partial class EndpointResolver
{
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

    ParsingLoggerMessages.MatchingPositionalSegments(logger, template.Count, args.Length, null);

    // Pre-pass: Handle repeated options first and mark consumed indices
    // Use stack allocation for typical CLI args (zero heap allocation for <= 64 args)
    Span<bool> consumedIndices = args.Length <= 64
      ? stackalloc bool[args.Length]
      : new bool[args.Length];
    int consumedCount = 0;
    IReadOnlyList<OptionMatcher> repeatedOptions = endpoint.CompiledRoute.RepeatedOptions;

    foreach (OptionMatcher repeatedOption in repeatedOptions)
    {
      if (!MatchRepeatedOptionWithIndices(repeatedOption, args, consumedIndices, ref consumedCount, extractedValues, logger, ref defaultsUsed))
      {
        totalConsumed = consumedCount;
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
        if (!MatchOptionSegment(option, args, extractedValues, seenEndOfOptions, endpoint.CompiledRoute.OptionMatchers, logger, ref defaultsUsed, consumedIndices, ref consumedCount))
        {
          totalConsumed = consumedCount;
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
          if (!consumedIndices[idx])
          {
            consumedIndices[idx] = true;
            consumedCount++;
          }
        }

        totalConsumed = consumedCount;
        return true;
      }

      // Skip args that were consumed by repeated options
      while (consumedArgs < args.Length && consumedIndices[consumedArgs])
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
        totalConsumed = consumedCount;
        return false;
      }

      // Match the positional segment against the argument
      if (!MatchRegularSegment(segment, args[consumedArgs], extractedValues, logger))
      {
        totalConsumed = consumedCount;
        return false;
      }

      // Track this index as consumed
      if (!consumedIndices[consumedArgs])
      {
        consumedIndices[consumedArgs] = true;
        consumedCount++;
      }

      consumedArgs++;
    }

    totalConsumed = consumedCount;
    ParsingLoggerMessages.PositionalMatchingComplete(logger, consumedArgs, null);
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
        ParsingLoggerMessages.OptionalParameterNoValue(logger, optionalParam.Name, null);
        return SegmentValidationResult.Skip;
      }

      ParsingLoggerMessages.NotEnoughArgumentsForSegment(logger, segment.ToDisplayString(), null);
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
          ParsingLoggerMessages.OptionalParameterSkippedHitOption(logger, optionalParam.Name, args[argIndex], null);
          return SegmentValidationResult.Skip;
        }

        ParsingLoggerMessages.RequiredSegmentExpectedButFoundOption(logger, segment.ToDisplayString(), args[argIndex], null);
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
    ParsingLoggerMessages.AttemptingToMatch(logger, arg, segment.ToDisplayString(), null);

    if (!segment.TryMatch(arg, out string? value))
    {
      ParsingLoggerMessages.FailedToMatch(logger, arg, segment.ToDisplayString(), null);
      return false;
    }

    if (value is not null && segment is ParameterMatcher ps)
    {
      extractedValues[ps.Name] = value;
      ParsingLoggerMessages.ExtractedParameter(logger, ps.Name, value, null);
    }
    else if (segment is LiteralMatcher)
    {
      ParsingLoggerMessages.LiteralMatched(logger, segment.ToDisplayString(), null);
    }

    return true;
  }
}
