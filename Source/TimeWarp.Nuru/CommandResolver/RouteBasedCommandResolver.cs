namespace TimeWarp.Nuru.CommandResolver;

/// <summary>
/// A command resolver that uses route patterns to match commands.
/// </summary>
public class RouteBasedCommandResolver
{
  private readonly EndpointCollection _endpoints;
  private readonly ITypeConverterRegistry _typeConverterRegistry;

  public RouteBasedCommandResolver(EndpointCollection endpoints, ITypeConverterRegistry typeConverterRegistry)
  {
    _endpoints = endpoints ?? throw new ArgumentNullException(nameof(endpoints));
    _typeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
  }

  public ResolverResult Resolve(IReadOnlyList<string> args)
  {
    // Try to match against route endpoints
    (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? matchResult = MatchRoute(args);

    if (matchResult is not null)
    {
      (RouteEndpoint endpoint, Dictionary<string, string> extractedValues) = matchResult.Value;

      return new ResolverResult
      {
        Success = true,
        MatchedEndpoint = endpoint,
        ExtractedValues = extractedValues
      };
    }

    return new ResolverResult
    {
      Success = false,
      ErrorMessage = "No matching command found"
    };
  }

  private (RouteEndpoint endpoint, Dictionary<string, string> extractedValues)? MatchRoute(IReadOnlyList<string> args)
  {
    foreach (RouteEndpoint endpoint in _endpoints)
    {
      var extractedValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

      // Check positional segments
      if (MatchPositionalSegments(endpoint, args, extractedValues, out int consumedArgs))
      {
        // Check if remaining args match required options
        var remainingArgs = args.Skip(consumedArgs).ToList();
        if (CheckRequiredOptions(endpoint, remainingArgs, extractedValues))
        {
          return (endpoint, extractedValues);
        }
      }
    }

    return null;
  }

  private bool MatchPositionalSegments(RouteEndpoint endpoint, IReadOnlyList<string> args,
      Dictionary<string, string> extractedValues, out int consumedArgs)
  {
    consumedArgs = 0;
    RouteSegment[] template = endpoint.ParsedRoute.PositionalTemplate;

    // Match each segment in the template
    for (int i = 0; i < template.Length; i++)
    {
      RouteSegment segment = template[i];

      // For catch-all segment (must be last), consume all remaining
      if (segment is ParameterSegment param && param.IsCatchAll)
      {
        consumedArgs = args.Count;
        if (i < args.Count)
        {
          extractedValues[param.Name] = string.Join(" ", args.Skip(i));
        }

        return true;
      }

      // Regular segment matching
      if (i >= args.Count || args[i].StartsWith("-"))
        return false; // Not enough args or hit an option

      if (!segment.TryMatch(args[i], out string? value))
        return false;

      if (value is not null && segment is ParameterSegment ps)
        extractedValues[ps.Name] = value;

      consumedArgs++;
    }

    return true;
  }

  private bool CheckRequiredOptions(RouteEndpoint endpoint, IReadOnlyList<string> remainingArgs,
      Dictionary<string, string> extractedValues)
  {
    OptionSegment[] optionSegments = endpoint.ParsedRoute.OptionSegments;

    // If no required options, we're good
    if (optionSegments.Length == 0)
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
            if (i + 1 >= remainingArgs.Count || remainingArgs[i + 1].StartsWith("-"))
            {
              return false;
            }

            // Extract the option value
            if (optionSegment.ValueParameterName is not null)
            {
              extractedValues[optionSegment.ValueParameterName] = remainingArgs[i + 1];
            }
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
