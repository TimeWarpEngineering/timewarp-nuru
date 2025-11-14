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

    // For now, return empty completions (Phase 2 will implement actual completion logic)
    // TODO: Implement route matching and parameter-aware completion using registry
    _ = registry; // Suppress unused parameter warning - will be used in Phase 2
    IEnumerable<CompletionCandidate> items = [];

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
}
