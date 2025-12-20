namespace TimeWarp.Nuru;

/// <summary>
/// EndpointResolver - utility methods for option detection, catch-all handling, and logging.
/// </summary>
internal static partial class EndpointResolver
{
  private static void LogExtractedValues(Dictionary<string, string> extractedValues, ILogger logger)
  {
    if (extractedValues.Count > 0)
    {
      ParsingLoggerMessages.ExtractedValues(logger, null);
      foreach (KeyValuePair<string, string> kvp in extractedValues)
      {
        ParsingLoggerMessages.ExtractedValue(logger, kvp.Key, kvp.Value, null);
      }
    }
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
    // Pre-size based on remaining args
    List<string> catchAllArgs = new(capacity: args.Length - startPosition);
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
      ParsingLoggerMessages.CatchAllParameterCaptured(logger, param.Name, catchAllValue, null);
    }
    else
    {
      ParsingLoggerMessages.CatchAllParameterNoArgs(logger, param.Name, null);
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
}
