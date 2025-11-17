namespace TimeWarp.Nuru.Completion;

using System.Linq;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Parsing;

/// <summary>
/// Handles the __complete callback route for dynamic shell completion.
/// </summary>
internal static class DynamicCompletionHandler
{
  /// <summary>
  /// Processes a completion request and outputs candidates to stdout.
  /// </summary>
  /// <param name="cursorIndex">The zero-based index of the word being completed.</param>
  /// <param name="words">All words on the command line.</param>
  /// <param name="registry">The completion source registry.</param>
  /// <param name="endpoints">The collection of registered endpoints for context.</param>
  /// <returns>Exit code (0 for success).</returns>
  public static int HandleCompletion
  (
    int cursorIndex,
    string[] words,
    CompletionSourceRegistry registry,
    EndpointCollection endpoints
  )
  {
    // Build completion context (using existing CompletionContext record)
    var context = new CompletionContext(
      Args: words,
      CursorPosition: cursorIndex,
      Endpoints: endpoints
    );

    // Get completions from the appropriate source
    IEnumerable<CompletionCandidate> items = GetCompletions(context, registry);

    // Determine the directive to use (default to NoFileComp for string parameters)
    CompletionDirective directive = CompletionDirective.NoFileComp;

    // Output completions to stdout (one per line)
    foreach (CompletionCandidate item in items)
    {
      if (!string.IsNullOrEmpty(item.Description))
      {
        Console.WriteLine($"{item.Value}\t{item.Description}");
      }
      else
      {
        Console.WriteLine(item.Value);
      }
    }

    // Output directive code (Cobra-style)
    Console.WriteLine($":{(int)directive}");

    // Output diagnostic to stderr (not visible to shell, useful for debugging)
    Console.Error.WriteLine($"Completion ended with directive: {directive}");

    return 0;
  }

  /// <summary>
  /// Gets completions for the current context by consulting the registry and falling back to defaults.
  /// </summary>
  private static IEnumerable<CompletionCandidate> GetCompletions(
    CompletionContext context,
    CompletionSourceRegistry registry)
  {
    // Try to detect if we're completing a specific parameter
    if (TryGetParameterInfo(context, out string? paramName, out Type? paramType))
    {
      // First, check if a completion source is registered for this specific parameter name
      if (paramName is not null)
      {
        ICompletionSource? customSource = registry.GetSourceForParameter(paramName);
        if (customSource is not null)
        {
          return customSource.GetCompletions(context);
        }
      }

      // Second, check if a completion source is registered for this parameter's type
      if (paramType is not null)
      {
        ICompletionSource? typeSource = registry.GetSourceForType(paramType);
        if (typeSource is not null)
        {
          return typeSource.GetCompletions(context);
        }
      }
    }

    // Fall back to default source (analyzes registered routes)
    var defaultSource = new DefaultCompletionSource();
    return defaultSource.GetCompletions(context);
  }

  /// <summary>
  /// Attempts to determine which parameter is being completed based on the context.
  /// </summary>
  /// <param name="context">The completion context.</param>
  /// <param name="parameterName">The name of the parameter being completed, if detected.</param>
  /// <param name="parameterType">The type of the parameter being completed, if detected.</param>
  /// <returns>True if a parameter was detected; otherwise, false.</returns>
  internal static bool TryGetParameterInfo(CompletionContext context, out string? parameterName, out Type? parameterType)
  {
    parameterName = null;
    parameterType = null;

    // Need at least the app name and one command word
    if (context.Args.Length < 2)
    {
      return false;
    }

    // Get the words typed so far (excluding app name at index 0)
    string[] commandWords = [.. context.Args.Skip(1).Take(context.CursorPosition - 1)];

    // Try to match against registered endpoints to find parameter info
    foreach (Endpoint endpoint in context.Endpoints)
    {
      // Try to match the typed words against this endpoint's pattern
      if (TryMatchEndpoint(endpoint, commandWords, out string? detectedParam, out Type? detectedType))
      {
        parameterName = detectedParam;
        parameterType = detectedType;
        return true;
      }
    }

    return false;
  }

  /// <summary>
  /// Attempts to match typed words against an endpoint pattern and detect the parameter being completed.
  /// </summary>
  internal static bool TryMatchEndpoint(Endpoint endpoint, string[] typedWords, out string? parameterName, out Type? parameterType)
  {
    parameterName = null;
    parameterType = null;

    int wordIndex = 0;
    int matcherIndex = 0;

    // Match positional segments (literals and parameters)
    while (matcherIndex < endpoint.CompiledRoute.PositionalMatchers.Count && wordIndex < typedWords.Length)
    {
      RouteMatcher matcher = endpoint.CompiledRoute.PositionalMatchers[matcherIndex];

      if (matcher is LiteralMatcher literal)
      {
        // Literal must match exactly
        if (!string.Equals(typedWords[wordIndex], literal.Value, StringComparison.Ordinal))
        {
          return false; // Doesn't match this endpoint
        }

        wordIndex++;
        matcherIndex++;
      }
      else if (matcher is ParameterMatcher parameter)
      {
        // Parameter consumes the word
        wordIndex++;
        matcherIndex++;
      }
      else
      {
        matcherIndex++;
      }
    }

    // If the endpoint has a catch-all, we can't determine specific parameter
    if (endpoint.CompiledRoute.HasCatchAll)
    {
      return false;
    }

    // Check if the next matcher is a parameter (what we're about to complete)
    if (matcherIndex < endpoint.CompiledRoute.PositionalMatchers.Count)
    {
      RouteMatcher nextMatcher = endpoint.CompiledRoute.PositionalMatchers[matcherIndex];
      if (nextMatcher is ParameterMatcher param)
      {
        parameterName = param.Name;
        parameterType = GetParameterType(endpoint, param.Name);
        return true;
      }
    }

    // Check if we're completing an option parameter (e.g., --version <value>)
    if (typedWords.Length > 0)
    {
      string lastWord = typedWords[^1];
      if (lastWord.StartsWith('-'))
      {
        // Find the option matcher for this flag
        foreach (OptionMatcher option in endpoint.CompiledRoute.OptionMatchers)
        {
          if (option.ExpectsValue &&
              (string.Equals(lastWord, option.MatchPattern, StringComparison.Ordinal) ||
               (option.AlternateForm is not null && string.Equals(lastWord, option.AlternateForm, StringComparison.Ordinal))))
          {
            parameterName = option.ParameterName;
            parameterType = GetParameterType(endpoint, option.ParameterName);
            return true;
          }
        }
      }
    }

    return false;
  }

  /// <summary>
  /// Gets the parameter type from an endpoint's method signature.
  /// </summary>
  private static Type? GetParameterType(Endpoint endpoint, string? parameterName)
  {
    if (parameterName is null || endpoint.Method is null)
    {
      return null;
    }

    // Get the parameter from the method signature
    System.Reflection.ParameterInfo? parameter = endpoint.Method.GetParameters()
      .FirstOrDefault(p => string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));

    return parameter?.ParameterType;
  }
}
