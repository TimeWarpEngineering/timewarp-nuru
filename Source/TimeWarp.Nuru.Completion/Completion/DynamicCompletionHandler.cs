namespace TimeWarp.Nuru.Completion;

using TimeWarp.Nuru;

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
    // Phase 3: Simple implementation - use DefaultCompletionSource for now
    // Phase 4 will add parameter-aware completion source lookup from registry
    _ = registry; // Will be used in Phase 4 for custom completion sources

    // Try to detect what we're completing based on cursor position and context
    // For now, just use the default source which analyzes registered routes
    var defaultSource = new DefaultCompletionSource();
    return defaultSource.GetCompletions(context);

    // Future enhancement: Check if we're completing a specific parameter
    // and look up custom completion sources from the registry
    // Example:
    // if (TryGetParameterName(context, out string paramName))
    // {
    //   ICompletionSource? customSource = registry.GetSourceForParameter(paramName);
    //   if (customSource is not null)
    //   {
    //     return customSource.GetCompletions(context);
    //   }
    // }
  }
}
