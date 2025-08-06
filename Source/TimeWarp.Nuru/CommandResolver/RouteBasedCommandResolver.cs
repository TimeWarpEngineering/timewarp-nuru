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

    ParserConsole.WriteLine($"\nResolving command: '{string.Join(" ", args)}'");
    ParserConsole.WriteLine($"Total endpoints to check: {endpoints.Count}");

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
      ParserConsole.WriteLine($"\n[{endpointIndex}] Checking endpoint: '{endpoint.RoutePattern}'");
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
            ParserConsole.WriteLine("  ✓ MATCH! Catch-all route matched");
            LogExtractedValues(extractedValues);
            return (endpoint, extractedValues);
          }

          // For non-catch-all routes, ensure all arguments were consumed
          int totalConsumed = consumedArgs + optionsConsumed;
          if (totalConsumed == args.Length)
          {
            ParserConsole.WriteLine($"  ✓ MATCH! All {totalConsumed} arguments consumed");
            LogExtractedValues(extractedValues);
            return (endpoint, extractedValues);
          }
          else
          {
            ParserConsole.WriteLine($"  ✗ Not all args consumed: {totalConsumed}/{args.Length}");
          }
        }
        else
        {
          ParserConsole.WriteLine("  ✗ Required options not matched");
        }
      }
      else
      {
        ParserConsole.WriteLine("  ✗ Positional segments not matched");
      }
    }

    ParserConsole.WriteLine($"\n✗ No matching route found for: '{string.Join(" ", args)}'");
    return null;
  }

  private static void LogExtractedValues(Dictionary<string, string> extractedValues)
  {
    if (extractedValues.Count > 0)
    {
      ParserConsole.WriteLine("  Extracted values:");
      foreach (KeyValuePair<string, string> kvp in extractedValues)
      {
        ParserConsole.WriteLine($"    {kvp.Key} = '{kvp.Value}'");
      }
    }
  }

  private static bool MatchPositionalSegments(RouteEndpoint endpoint, string[] args,
      Dictionary<string, string> extractedValues, out int consumedArgs)
  {
    consumedArgs = 0;
    IReadOnlyList<RouteMatcher> template = endpoint.CompiledRoute.PositionalMatchers;

    ParserConsole.WriteLine($"  Matching {template.Count} positional segments against {args.Length} arguments");

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
          ParserConsole.WriteLine($"    [{i}] Catch-all '{param.Name}' = '{catchAllValue}'");
        }
        else
        {
          ParserConsole.WriteLine($"    [{i}] Catch-all '{param.Name}' = (no args to consume)");
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
          ParserConsole.WriteLine($"    [{i}] Optional parameter '{optionalParam.Name}' - no value provided");
          continue;
        }

        ParserConsole.WriteLine($"    [{i}] Not enough args for segment '{segment.ToDisplayString()}'");
        return false; // Not enough args
      }

      if (args[i].StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // Hit an option - check if current segment is optional
        if (segment is ParameterMatcher optionalParam && optionalParam.IsOptional)
        {
          // Optional parameter followed by an option - skip it
          ParserConsole.WriteLine($"    [{i}] Optional parameter '{optionalParam.Name}' - skipped (hit option '{args[i]}')");
          continue;
        }

        ParserConsole.WriteLine($"    [{i}] Required segment '{segment.ToDisplayString()}' but hit option '{args[i]}'");
        return false; // Required parameter but hit an option
      }

      string argToMatch = args[i];
      ParserConsole.WriteLine($"    [{i}] Trying to match '{argToMatch}' against {segment.ToDisplayString()}");

      if (!segment.TryMatch(argToMatch, out string? value))
      {
        ParserConsole.WriteLine("        ✗ No match");
        return false;
      }

      if (value is not null && segment is ParameterMatcher ps)
      {
        extractedValues[ps.Name] = value;
        ParserConsole.WriteLine($"        ✓ Matched! Extracted '{ps.Name}' = '{value}'");
      }
      else if (segment is LiteralMatcher)
      {
        ParserConsole.WriteLine("        ✓ Literal matched");
      }

      consumedArgs++;
    }

    ParserConsole.WriteLine($"  Positional matching complete. Consumed {consumedArgs} args.");
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
              ParserConsole.WriteLine($"        Boolean option '{optionSegment.ParameterName}' = true");
            }

            optionsConsumed++; // Just the option
          }

          break;
        }
      }

      if (!found)
      {
        ParserConsole.WriteLine($"      ✗ Required option not found: {optionSegment.ToDisplayString()}");
        return false;
      }
    }

    ParserConsole.WriteLine($"  Options matching complete. Consumed {optionsConsumed} args.");
    return true;
  }
}
