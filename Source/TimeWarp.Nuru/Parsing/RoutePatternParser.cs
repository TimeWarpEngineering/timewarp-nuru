namespace TimeWarp.Nuru.Parsing;

/// <summary>
/// Parses route pattern strings into ParsedRoute objects.
/// </summary>
public static class RoutePatternParser
{
  // private static readonly Regex ParameterRegex = new(@"\{(\*)?([^}:|?]+)(\?)?(:([^}|]+))?(\|([^}]+))?\}", RegexOptions.Compiled);

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
    // Use the new improved parser
    return ImprovedRoutePatternParser.Parse(routePattern);
  }
}
