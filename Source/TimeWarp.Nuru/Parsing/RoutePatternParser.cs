namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Parses route pattern strings into ParsedRoute objects.
/// </summary>
internal static class RoutePatternParser
{
  private static readonly Regex ParameterRegex = new(@"\{(\*)?([^}:|?]+)(\?)?(:([^}|]+))?(\|([^}]+))?\}", RegexOptions.Compiled);

  /// <summary>
  /// Tokenizes a route pattern, preserving descriptions that contain spaces.
  /// </summary>
  private static List<string> TokenizePattern(string pattern)
  {
    var tokens = new List<string>();
    var currentToken = new System.Text.StringBuilder();
    bool inBraces = false;
    bool inOptionDescription = false;
    int braceDepth = 0;

    for (int i = 0; i < pattern.Length; i++)
    {
      char c = pattern[i];

      if (c == '{')
      {
        inBraces = true;
        braceDepth++;
      }
      else if (c == '}')
      {
        braceDepth--;
        if (braceDepth == 0)
        {
          inBraces = false;
          // If we were in an option description and hit the closing brace of a parameter,
          // we need to check if the next character starts a new token
          if (inOptionDescription && i + 1 < pattern.Length && pattern[i + 1] == '|')
          {
            // This is the case where we have --option {param}|Description
            // The description continues after the parameter
            currentToken.Append(c);
            continue;
          }
        }
      }
      else if (c == '|' && !inBraces)
      {
        // Start of option description
        inOptionDescription = true;
      }
      else if (c == ' ' && !inBraces && !inOptionDescription)
      {
        // Space outside of braces and descriptions - this is a token separator
        if (currentToken.Length > 0)
        {
          tokens.Add(currentToken.ToString());
          currentToken.Clear();
        }

        continue;
      }
      else if (c == ' ' && inOptionDescription)
      {
        // Check if we're at the start of a new option/parameter
        if (i + 1 < pattern.Length && (pattern[i + 1] == '-' || pattern[i + 1] == '{'))
        {
          // This space ends the description
          inOptionDescription = false;
          if (currentToken.Length > 0)
          {
            tokens.Add(currentToken.ToString());
            currentToken.Clear();
          }

          continue;
        }
      }

      currentToken.Append(c);
    }

    // Add the last token
    if (currentToken.Length > 0)
    {
      tokens.Add(currentToken.ToString());
    }

    return tokens;
  }

  /// <summary>
  /// Parses a route pattern string into a ParsedRoute object.
  /// </summary>
  /// <param name="routePattern">The route pattern to parse (e.g., "git commit --amend").</param>
  /// <returns>A parsed representation of the route.</returns>
  public static ParsedRoute Parse(string routePattern)
  {
    ArgumentNullException.ThrowIfNull(routePattern);

    // First, tokenize the pattern to handle descriptions with spaces
    List<string> parts = TokenizePattern(routePattern);

    var segments = new List<RouteSegment>();
    var requiredOptions = new List<string>();
    var optionSegments = new List<OptionSegment>();
    string? catchAllParameterName = null;
    int specificity = 0;

    for (int i = 0; i < parts.Count; i++)
    {
      string part = parts[i];

      if (part.StartsWith(CommonStrings.DoubleDash, StringComparison.Ordinal) || part.StartsWith(CommonStrings.SingleDash, StringComparison.Ordinal))
      {
        // This is an option - parse syntax: --long,-s|Description
        string optionName;
        string? shortAlias = null;
        string? description = null;

        // First check for description (pipe separator)
        string optionPart = part;
        int pipeIndex = part.IndexOf('|', StringComparison.Ordinal);
        if (pipeIndex > 0)
        {
          optionPart = part[..pipeIndex];
          description = part[(pipeIndex + 1)..];
        }

        // Then check for short alias (comma separator)
        if (optionPart.Contains(',', StringComparison.Ordinal))
        {
          string[] aliasParts = optionPart.Split(',');
          optionName = aliasParts[0];
          shortAlias = aliasParts.Length > 1 ? aliasParts[1] : null;
        }
        else
        {
          optionName = optionPart;
        }

        requiredOptions.Add(optionName);
        specificity += 10; // Options increase specificity

        // Check if next part is a parameter for this option
        bool expectsValue = false;
        string? valueParameterName = null;

        if (i + 1 < parts.Count && parts[i + 1].StartsWith('{'))
        {
          expectsValue = true;
          i++; // Move to parameter

          // The parameter part might have an option description after it
          string paramPart = parts[i];
          string? optionDescriptionSuffix = null;

          // Check if there's a pipe after the closing brace
          int closingBraceIndex = paramPart.IndexOf('}', StringComparison.Ordinal);
          if (closingBraceIndex > 0 && closingBraceIndex + 1 < paramPart.Length && paramPart[closingBraceIndex + 1] == '|')
          {
            // Split the parameter part from the option description
            optionDescriptionSuffix = paramPart[(closingBraceIndex + 2)..];
            paramPart = paramPart[..(closingBraceIndex + 1)];

            // If we already had a description from the option part, append this
            if (description is not null)
            {
              description = description + " " + optionDescriptionSuffix;
            }
            else
            {
              description = optionDescriptionSuffix;
            }
          }

          Match paramMatch = ParameterRegex.Match(paramPart);
          if (paramMatch.Success)
          {
            string paramName = paramMatch.Groups[2].Value;
            bool isCatchAll = paramMatch.Groups[1].Value == "*";
            bool isOptional = paramMatch.Groups[3].Value == "?";
            string? typeConstraint = paramMatch.Groups[5].Success ? paramMatch.Groups[5].Value : null;
            string? paramDescription = paramMatch.Groups[7].Success ? paramMatch.Groups[7].Value : null;

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

        // Add option segment with alias and description
        optionSegments.Add(new OptionSegment(optionName, expectsValue, valueParameterName, shortAlias, description));
      }
      else if (part.StartsWith('{'))
      {
        // This is a positional parameter
        Match paramMatch = ParameterRegex.Match(part);
        if (paramMatch.Success)
        {
          string paramName = paramMatch.Groups[2].Value;
          bool isCatchAll = paramMatch.Groups[1].Value == "*";
          bool isOptional = paramMatch.Groups[3].Value == "?";
          string? typeConstraint = paramMatch.Groups[5].Success ? paramMatch.Groups[5].Value : null;
          string? description = paramMatch.Groups[7].Success ? paramMatch.Groups[7].Value : null;

          // Parameter information is stored in the ParameterSegment

          if (isCatchAll)
          {
            catchAllParameterName = paramName;
            // Catch-all reduces specificity
            specificity -= 20;

            // Add catch-all as a parameter segment
            segments.Add(new ParameterSegment(paramName, isCatchAll: true, typeConstraint, description, isOptional));
          }
          else
          {
            // Add regular parameter segment
            segments.Add(new ParameterSegment(paramName, isCatchAll: false, typeConstraint, description, isOptional));
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
