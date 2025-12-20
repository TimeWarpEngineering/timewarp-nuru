namespace TimeWarp.Nuru;

/// <summary>
/// Route pattern parsing methods for the delegate command generator.
/// </summary>
public partial class NuruDelegateCommandGenerator
{
  /// <summary>
  /// Parses a route pattern to extract parameter names.
  /// </summary>
  private static RouteParameterInfo ParseRoutePattern(string pattern)
  {
    List<string> positionalParams = [];
    Dictionary<string, string> optionParams = []; // option long form -> param name

    if (string.IsNullOrWhiteSpace(pattern))
      return new RouteParameterInfo(positionalParams, optionParams);

    // Simple parsing - look for {paramName} and --option patterns
    int i = 0;
    while (i < pattern.Length)
    {
      // Skip whitespace
      while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
        i++;

      if (i >= pattern.Length)
        break;

      // Check for option (--name or -n)
      if (i < pattern.Length - 1 && pattern[i] == '-' && pattern[i + 1] == '-')
      {
        // Long option: --force, --config {mode}
        i += 2; // skip --
        int optStart = i;
        while (i < pattern.Length && (char.IsLetterOrDigit(pattern[i]) || pattern[i] == '-'))
          i++;

        string optName = pattern[optStart..i];

        // Check for aliases (--force,-f)
        while (i < pattern.Length && pattern[i] == ',')
        {
          i++; // skip comma
          if (i < pattern.Length && pattern[i] == '-')
          {
            i++; // skip -
            while (i < pattern.Length && char.IsLetterOrDigit(pattern[i]))
              i++;
          }
        }

        // Check for optional marker (?) - skip it
        if (i < pattern.Length && pattern[i] == '?')
        {
          i++;
        }

        // Check for parameter value: {paramName}
        while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
          i++;

        if (i < pattern.Length && pattern[i] == '{')
        {
          i++; // skip {

          // Check for catch-all
          if (i < pattern.Length && pattern[i] == '*')
            i++;

          int paramStart = i;
          while (i < pattern.Length && pattern[i] != '}' && pattern[i] != ':' && pattern[i] != '?')
            i++;

          string paramName = pattern[paramStart..i];

          // Skip type constraint and optional marker
          while (i < pattern.Length && pattern[i] != '}')
            i++;

          if (i < pattern.Length)
            i++; // skip }

          optionParams[optName] = paramName;
        }
        else
        {
          // Flag option (no value) - the option name itself becomes a bool parameter
          optionParams[optName] = optName;
        }
      }
      else if (pattern[i] == '{')
      {
        // Positional parameter: {name}, {name:type}, {name?}
        i++; // skip {

        // Check for catch-all
        if (i < pattern.Length && pattern[i] == '*')
          i++;

        int paramStart = i;
        while (i < pattern.Length && pattern[i] != '}' && pattern[i] != ':' && pattern[i] != '?')
          i++;

        string paramName = pattern[paramStart..i];
        positionalParams.Add(paramName);

        // Skip to end of parameter
        while (i < pattern.Length && pattern[i] != '}')
          i++;

        if (i < pattern.Length)
          i++; // skip }
      }
      else if (pattern[i] == '-')
      {
        // Short option only: -v (less common, skip for now)
        i++;
        while (i < pattern.Length && char.IsLetterOrDigit(pattern[i]))
          i++;
      }
      else
      {
        // Literal - skip
        while (i < pattern.Length && !char.IsWhiteSpace(pattern[i]) && pattern[i] != '{')
          i++;
      }
    }

    return new RouteParameterInfo(positionalParams, optionParams);
  }

  /// <summary>
  /// Finds if a delegate parameter matches a route parameter.
  /// </summary>
  private static RouteParamMatch? FindRouteParameter(string delegateParamName, RouteParameterInfo routeParams)
  {
    // Check positional parameters (case-insensitive)
    foreach (string positional in routeParams.PositionalParams)
    {
      if (string.Equals(positional, delegateParamName, StringComparison.OrdinalIgnoreCase))
        return new RouteParamMatch(positional, IsOption: false);
    }

    // Check option parameters (case-insensitive)
    foreach (KeyValuePair<string, string> option in routeParams.OptionParams)
    {
      if (string.Equals(option.Value, delegateParamName, StringComparison.OrdinalIgnoreCase))
        return new RouteParamMatch(option.Value, IsOption: true);
    }

    return null;
  }

  /// <summary>
  /// Generates a class name from the route pattern and message type.
  /// </summary>
  private static string GenerateClassName(string pattern, GeneratedMessageType messageType)
  {
    string suffix = messageType == GeneratedMessageType.Query ? "_Generated_Query" : "_Generated_Command";

    if (string.IsNullOrWhiteSpace(pattern))
      return $"Default{suffix}";

    // Extract first literal(s) from pattern
    List<string> literals = [];
    int i = 0;

    while (i < pattern.Length)
    {
      // Skip whitespace
      while (i < pattern.Length && char.IsWhiteSpace(pattern[i]))
        i++;

      if (i >= pattern.Length)
        break;

      // Skip options and parameters
      if (pattern[i] == '-' || pattern[i] == '{')
        break;

      // Extract literal
      int start = i;
      while (i < pattern.Length && !char.IsWhiteSpace(pattern[i]) && pattern[i] != '{' && pattern[i] != '-')
        i++;

      if (i > start)
        literals.Add(pattern[start..i]);
    }

    if (literals.Count == 0)
      return $"Default{suffix}";

    // Convert to PascalCase and join
    string prefix = string.Concat(literals.Select(ToPascalCase));
    return $"{prefix}{suffix}";
  }

  /// <summary>
  /// Converts a string to PascalCase.
  /// </summary>
  private static string ToPascalCase(string input)
  {
    if (string.IsNullOrEmpty(input))
      return input;

    // Handle kebab-case: some-name → SomeName
    if (input.Contains('-', StringComparison.Ordinal))
    {
      return string.Concat(input.Split('-')
        .Where(s => s.Length > 0)
        .Select(s => char.ToUpperInvariant(s[0]) + s[1..]));
    }

    return char.ToUpperInvariant(input[0]) + input[1..];
  }
}
