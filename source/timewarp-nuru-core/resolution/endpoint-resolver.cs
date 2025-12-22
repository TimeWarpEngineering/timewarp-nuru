namespace TimeWarp.Nuru;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
/// <remarks>
/// This class is split into partial classes for maintainability:
/// - endpoint-resolver.cs: Core resolution logic (Resolve, MatchRoute, SelectBestMatch)
/// - endpoint-resolver.segments.cs: Segment matching (MatchSegments, ValidateSegmentAvailability, MatchRegularSegment)
/// - endpoint-resolver.options.cs: Option handling (MatchOptionSegment, MatchRepeatedOptionWithIndices, SetDefaultOptionValue)
/// - endpoint-resolver.helpers.cs: Utility methods (IsDefinedOption, HandleCatchAllSegment, LogExtractedValues)
/// </remarks>
internal static partial class EndpointResolver
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
#if NURU_TIMING_DEBUG
    System.Diagnostics.Stopwatch swResolve = System.Diagnostics.Stopwatch.StartNew();
#endif

    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(endpoints);
    ArgumentNullException.ThrowIfNull(typeConverterRegistry);

    logger ??= NullLogger.Instance;

    // Only log if we have a real logger (avoid string.Join allocation when logging disabled)
    if (logger is not NullLogger)
    {
      ParsingLoggerMessages.ResolvingCommand(logger, string.Join(" ", args), null);
      ParsingLoggerMessages.CheckingAvailableRoutes(logger, endpoints.Count, null);
    }

#if NURU_TIMING_DEBUG
    long setupTicks = swResolve.ElapsedTicks;
#endif

    // Try to match against route endpoints
    // Pass NullLogger.Instance directly when logging is disabled to allow JIT to optimize away logging calls
    ILogger effectiveLogger = logger is NullLogger ? NullLogger.Instance : logger;
    (Endpoint endpoint, Dictionary<string, string> extractedValues)? matchResult =
      MatchRoute(args, endpoints, effectiveLogger);

#if NURU_TIMING_DEBUG
    long matchTicks = swResolve.ElapsedTicks;
    double ticksPerUs = System.Diagnostics.Stopwatch.Frequency / 1_000_000.0;
    Console.WriteLine($"[TIMING Resolve] Setup={(setupTicks / ticksPerUs):F0}us, MatchRoute={(matchTicks - setupTicks) / ticksPerUs:F0}us, Total={(matchTicks / ticksPerUs):F0}us");
#endif

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
    // Pre-size for common case: typically 0-2 matches
    List<RouteMatch> matches = new(capacity: 2);
    // Pre-size for typical CLI commands (4-8 parameters)
    Dictionary<string, string> extractedValues = new(capacity: 8, StringComparer.OrdinalIgnoreCase);

    int endpointIndex = 0;
    foreach (Endpoint endpoint in endpoints)
    {
      endpointIndex++;
      ParsingLoggerMessages.CheckingRoute(logger, endpointIndex, endpoints.Count, endpoint.RoutePattern, null);
      extractedValues.Clear(); // Clear for each attempt

      // Match all segments sequentially (literals, parameters, and options)
      if (MatchSegments(endpoint, args, extractedValues, logger, out int consumedArgs, out int defaultsUsed, out int totalConsumed))
      {
        // For catch-all routes, we don't need to check if all args were consumed
        if (endpoint.CompiledRoute.HasCatchAll)
        {
          ParsingLoggerMessages.MatchedCatchAllRoute(logger, endpoint.RoutePattern, null);
          LogExtractedValues(extractedValues, logger);
          // Store this match and continue checking other routes
          matches.Add(new RouteMatch(endpoint, new Dictionary<string, string>(extractedValues, StringComparer.OrdinalIgnoreCase), defaultsUsed));
          continue;
        }

        // For non-catch-all routes, ensure all arguments were consumed
        if (totalConsumed == args.Length)
        {
          ParsingLoggerMessages.MatchedRoute(logger, endpoint.RoutePattern, null);
          LogExtractedValues(extractedValues, logger);
          // Store this match and continue checking other routes
          matches.Add(new RouteMatch(endpoint, new Dictionary<string, string>(extractedValues, StringComparer.OrdinalIgnoreCase), defaultsUsed));
        }
        else
        {
          ParsingLoggerMessages.RouteConsumedPartialArgs(logger, endpoint.RoutePattern, totalConsumed, args.Length, null);
        }
      }
      else
      {
        ParsingLoggerMessages.RouteFailedAtPositionalMatching(logger, endpoint.RoutePattern, null);
      }
    }

    // If no matches found, return null
    if (matches.Count == 0)
    {
      if (logger is not NullLogger)
      {
        ParsingLoggerMessages.NoMatchingRouteFound(logger, string.Join(" ", args), null);
      }

      return null;
    }

    // Select the best match:
    // 1. Exact matches (0 defaults) always win - pick highest specificity among them
    // 2. If no exact matches, prefer routes with MORE defaults (user routes beat help routes)
    // 3. Among matches with same defaults, prefer higher specificity
    // Using loops instead of LINQ to avoid JIT overhead on cold start
    RouteMatch bestMatch = SelectBestMatch(matches);

    return (bestMatch.Endpoint, bestMatch.ExtractedValues);
  }

  private static RouteMatch SelectBestMatch(List<RouteMatch> matches)
  {
    // First, find exact matches (0 defaults) with highest specificity
    RouteMatch? bestExact = null;
    RouteMatch? bestWithDefaults = null;

    foreach (RouteMatch match in matches)
    {
      if (match.DefaultsUsed == 0)
      {
        // Exact match - compare by specificity
        if (bestExact is null || match.Endpoint.CompiledRoute.Specificity > bestExact.Endpoint.CompiledRoute.Specificity)
          bestExact = match;
      }
      else
      {
        // Match with defaults - compare by defaults count (more is better), then specificity
        if (bestWithDefaults is null ||
            match.DefaultsUsed > bestWithDefaults.DefaultsUsed ||
            (match.DefaultsUsed == bestWithDefaults.DefaultsUsed &&
             match.Endpoint.CompiledRoute.Specificity > bestWithDefaults.Endpoint.CompiledRoute.Specificity))
          bestWithDefaults = match;
      }
    }

    // Prefer exact matches over matches with defaults
    return bestExact ?? bestWithDefaults!;
  }
}
