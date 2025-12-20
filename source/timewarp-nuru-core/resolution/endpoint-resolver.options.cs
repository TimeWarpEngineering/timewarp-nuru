namespace TimeWarp.Nuru;

/// <summary>
/// EndpointResolver - option matching logic for flags, values, and repeated options.
/// </summary>
internal static partial class EndpointResolver
{
  private static bool MatchOptionSegment
  (
    OptionMatcher option,
    string[] args,
    Dictionary<string, string> extractedValues,
    bool seenEndOfOptions,
    IReadOnlyList<OptionMatcher> optionMatchers,
    ILogger logger,
    ref int defaultsUsed,
    Span<bool> consumedIndices,
    ref int consumedCount
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

      ParsingLoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
      return false;
    }

    // Search through ALL unconsumed arguments for this option (position-independent)
    // This allows options to appear in any order AFTER positional arguments
    int foundIndex = -1;
    for (int i = 0; i < args.Length; i++)
    {
      // Skip already consumed indices
      if (consumedIndices[i])
        continue;

      if (option.TryMatch(args[i], out _))
      {
        foundIndex = i;
        break;
      }
    }

    if (foundIndex >= 0)
    {
      // Option found at foundIndex - mark as consumed
      consumedIndices[foundIndex] = true;
      consumedCount++;

      if (option.ExpectsValue)
      {
        // Option expects a value - look at the next arg after the option
        int valueIndex = foundIndex + 1;

        // Check if next arg is available and is NOT a defined option
        bool valueIsAvailable = valueIndex < args.Length &&
                                !consumedIndices[valueIndex] &&
                                (!args[valueIndex].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal) ||
                                 !IsDefinedOption(args[valueIndex], optionMatchers));

        if (!valueIsAvailable)
        {
          // No value available
          if (!option.ParameterIsOptional)
          {
            // Value is required but not provided
            ParsingLoggerMessages.RequiredOptionValueNotProvided(logger, option.ToDisplayString(), null);
            return false;
          }
          // Value is optional and not provided - parameter will be null
          ParsingLoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName ?? "", null);
        }
        else
        {
          // Value is available - extract it
          if (option.ParameterName is not null)
          {
            extractedValues[option.ParameterName] = args[valueIndex];
            ParsingLoggerMessages.ExtractedParameter(logger, option.ParameterName, args[valueIndex], null);
          }

          // Mark option value as consumed
          consumedIndices[valueIndex] = true;
          consumedCount++;
        }
      }
      else
      {
        // Boolean option - no value needed
        if (option.ParameterName is not null)
        {
          extractedValues[option.ParameterName] = "true";
          ParsingLoggerMessages.BooleanOptionSet(logger, option.ParameterName, null);
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
    ParsingLoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
    return false;
  }

  private static bool MatchRepeatedOptionWithIndices
  (
    OptionMatcher option,
    string[] args,
    Span<bool> consumedIndices,
    ref int consumedCount,
    Dictionary<string, string> extractedValues,
    ILogger logger,
    ref int defaultsUsed
  )
  {
    // Lazy-create list only when we find a match (avoids allocation in common case)
    List<string>? collectedValues = null;

    // Scan all args for occurrences of this repeated option
    for (int i = 0; i < args.Length; i++)
    {
      // Skip if already consumed
      if (consumedIndices[i])
        continue;

      // Check if current arg matches the option
      if (option.TryMatch(args[i], out _))
      {
        // Mark this index as consumed
        consumedIndices[i] = true;
        consumedCount++;

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
              ParsingLoggerMessages.RequiredOptionValueNotProvided(logger, option.ToDisplayString(), null);
              return false;
            }
            // Value is optional - skip this occurrence
          }
          else
          {
            // Collect the value and mark its index as consumed
            collectedValues ??= [];
            collectedValues.Add(args[i + 1]);
            consumedIndices[i + 1] = true;
            consumedCount++;
          }
        }
        else
        {
          // Boolean repeated option - just count occurrences
          collectedValues ??= [];
          collectedValues.Add("true");
        }
      }
    }

    // Store collected values
    if (collectedValues is { Count: > 0 } && option.ParameterName is not null)
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

    ParsingLoggerMessages.RequiredOptionNotFound(logger, option.ToDisplayString(), null);
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
      ParsingLoggerMessages.OptionalBooleanOptionNotProvided(logger, option.ParameterName, null);
    }
    else if (option.IsRepeated)
    {
      // Repeated option defaults to empty array
      extractedValues[option.ParameterName] = "";
      ParsingLoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName, null);
    }
    else
    {
      // Other value options default to null (handled by binding)
      ParsingLoggerMessages.OptionalValueOptionNotProvided(logger, option.ParameterName, null);
    }
  }
}
