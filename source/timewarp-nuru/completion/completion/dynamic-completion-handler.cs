namespace TimeWarp.Nuru;

/// <summary>
/// Handles the __complete callback route for dynamic shell completion.
/// Uses source-generated <see cref="IShellCompletionProvider"/> for static completion data.
/// </summary>
public static class DynamicCompletionHandler
{
  /// <summary>
  /// Processes a completion request and outputs candidates to stdout.
  /// </summary>
  /// <param name="context">The completion context.</param>
  /// <param name="registry">The completion source registry for custom sources.</param>
  /// <param name="provider">The source-generated completion provider.</param>
  /// <returns>Exit code (0 for success).</returns>
  public static int HandleCompletion
  (
    CompletionContext context,
    CompletionSourceRegistry registry,
    IShellCompletionProvider provider
  )
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(registry);
    ArgumentNullException.ThrowIfNull(provider);

    // Get completions - prioritize custom sources, then use provider
    IEnumerable<CompletionCandidate> items = GetCompletions(context, registry, provider);

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
  /// Gets completions by consulting custom sources first, then falling back to the provider.
  /// </summary>
  private static IEnumerable<CompletionCandidate> GetCompletions
  (
    CompletionContext context,
    CompletionSourceRegistry registry,
    IShellCompletionProvider provider
  )
  {
    // Try to detect if we're completing a specific parameter using the source-generated provider
    if (provider.TryGetParameterInfo(context.CursorPosition, context.Args, out string? paramName, out string? paramTypeName))
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
      if (paramTypeName is not null)
      {
        Type? paramType = Type.GetType(paramTypeName);
        if (paramType is not null)
        {
          ICompletionSource? typeSource = registry.GetSourceForType(paramType);
          if (typeSource is not null)
          {
            return typeSource.GetCompletions(context);
          }
        }
      }
    }

    // Use source-generated provider for static completions (AOT-friendly path)
    return provider.GetCompletions(context.CursorPosition, context.Args);
  }
}
