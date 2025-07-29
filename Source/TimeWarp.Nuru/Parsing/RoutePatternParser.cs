namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Parses route pattern strings into ParsedRoute objects.
/// </summary>
public static class RoutePatternParser
{
  private static readonly Regex ParameterRegex = new(@"\{(\*)?([^}:]+)(:([^}]+))?\}", RegexOptions.Compiled);

  /// <summary>
  /// Parses a route pattern string into a ParsedRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse (e.g., "git commit --amend").</param>
  /// <returns>A parsed representation of the route.</returns>
  public static ParsedRoute Parse(string routePattern)
  {
    if (string.IsNullOrWhiteSpace(routePattern))
      throw new ArgumentException("Route pattern cannot be null or empty.", nameof(routePattern));

    string[] parts = routePattern.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var segments = new List<RouteSegment>();
    var requiredOptions = new List<string>();
    var optionSegments = new List<OptionSegment>();
    string? catchAllParameterName = null;
    int specificity = 0;

    for (int i = 0; i < parts.Length; i++)
    {
      string part = parts[i];

      if (part.StartsWith(CommonStrings.DoubleDash, StringComparison.Ordinal) || part.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // This is an option - check for alias syntax (--long|-s)
        string optionName;
        string? shortAlias = null;

        if (part.Contains('|', StringComparison.Ordinal))
        {
          string[] optionParts = part.Split('|');
          optionName = optionParts[0];
          shortAlias = optionParts.Length > 1 ? optionParts[1] : null;
        }
        else
        {
          optionName = part;
        }

        requiredOptions.Add(optionName);
        specificity += 10; // Options increase specificity

        // Check if next part is a parameter for this option
        bool expectsValue = false;
        string? valueParameterName = null;

        if (i + 1 < parts.Length && parts[i + 1].StartsWith('{'))
        {
          expectsValue = true;
          i++; // Move to parameter
          Match paramMatch = ParameterRegex.Match(parts[i]);
          if (paramMatch.Success)
          {
            string paramName = paramMatch.Groups[2].Value;
            bool isCatchAll = paramMatch.Groups[1].Value == "*";
            string typeConstraint = paramMatch.Groups[4].Value;

            valueParameterName = paramName;

            if (isCatchAll)
            {
              catchAllParameterName = paramName;
            }
            else
            {
              specificity += 5; // Option parameters increase specificity
            }
          }
        }

        // Add option segment with alias
        optionSegments.Add(new OptionSegment(optionName, expectsValue, valueParameterName, shortAlias));
      }
      else if (part.StartsWith('{'))
      {
        // This is a positional parameter
        Match paramMatch = ParameterRegex.Match(part);
        if (paramMatch.Success)
        {
          string paramName = paramMatch.Groups[2].Value;
          bool isCatchAll = paramMatch.Groups[1].Value == "*";
          string typeConstraint = paramMatch.Groups[4].Value;

          // Parameter information is stored in the ParameterSegment

          if (isCatchAll)
          {
            catchAllParameterName = paramName;
            // Catch-all reduces specificity
            specificity -= 20;

            // Add catch-all as a parameter segment
            segments.Add(new ParameterSegment(paramName, isCatchAll: true, typeConstraint));
          }
          else
          {
            // Add regular parameter segment
            segments.Add(new ParameterSegment(paramName, isCatchAll: false, typeConstraint));
            specificity += 2; // Positional parameters slightly increase specificity
          }
        }
      }
      else
      {
        // This is a literal segment
        segments.Add(new LiteralSegment(part));
        specificity += 15; // Literal segments greatly increase specificity
      }
    }

    return new ParsedRoute
    {
      PositionalTemplate = segments.ToArray(),
      RequiredOptions = requiredOptions.ToArray(),
      OptionSegments = optionSegments.ToArray(),
      CatchAllParameterName = catchAllParameterName,
      Specificity = specificity
    };
  }
}
