namespace TimeWarp.Nuru;

/// <summary>
/// Handles the __complete callback route for dynamic shell completion.
/// Uses source-generated <see cref="IShellCompletionProvider"/> for static completion data.
/// </summary>
public static class DynamicCompletionHandler
{
  /// <summary>
  /// Processes a completion request and outputs candidates.
  /// </summary>
  /// <param name="context">The completion context.</param>
  /// <param name="registry">The completion source registry for custom sources.</param>
  /// <param name="provider">The source-generated completion provider.</param>
  /// <param name="terminal">The terminal for output.</param>
  /// <returns>Exit code (0 for success).</returns>
  public static int HandleCompletion
  (
    CompletionContext context,
    CompletionSourceRegistry registry,
    IShellCompletionProvider provider,
    ITerminal terminal
  )
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(registry);
    ArgumentNullException.ThrowIfNull(provider);
    ArgumentNullException.ThrowIfNull(terminal);

    // Get completions - prioritize custom sources, then use provider
    List<CompletionCandidate> items = [.. GetCompletions(context, registry, provider)];

    CompletionDirective directive = ResolveDirective(items);

    // Output completions (one per line). File/Directory candidates with no value are
    // delegation markers only: the directive tells the shell to run its own path completion.
    foreach (CompletionCandidate item in items)
    {
      if (string.IsNullOrEmpty(item.Value))
      {
        continue;
      }

      if (!string.IsNullOrEmpty(item.Description))
      {
        terminal.WriteLine($"{item.Value}\t{item.Description}");
      }
      else
      {
        terminal.WriteLine(item.Value);
      }
    }

    // Output directive code (Cobra-style)
    terminal.WriteLine($":{(int)directive}");

    // Output diagnostic to stderr (not visible to shell, useful for debugging)
    terminal.WriteErrorLine($"Completion ended with directive: {directive}");

    return 0;
  }

  /// <summary>
  /// Combines the directive flags requested by candidates with the file-completion policy.
  /// </summary>
  /// <remarks>
  /// <see cref="CompletionDirective.NoFileComp"/> is added unless a <see cref="CompletionType.File"/> or
  /// <see cref="CompletionType.Directory"/> candidate is present; a candidate may still request it explicitly.
  /// When only directory candidates request path completion, <see cref="CompletionDirective.FilterDirs"/> is added.
  /// </remarks>
  /// <param name="candidates">The completion candidates for this request.</param>
  /// <returns>The directive written to the shell script.</returns>
  public static CompletionDirective ResolveDirective(IEnumerable<CompletionCandidate> candidates)
  {
    ArgumentNullException.ThrowIfNull(candidates);

    CompletionDirective directive = CompletionDirective.None;
    bool wantsFiles = false;
    bool wantsDirectories = false;

    foreach (CompletionCandidate candidate in candidates)
    {
      directive |= candidate.Directive;
      wantsFiles |= candidate.Type == CompletionType.File;
      wantsDirectories |= candidate.Type == CompletionType.Directory;
    }

    if (!wantsFiles && !wantsDirectories)
    {
      directive |= CompletionDirective.NoFileComp;
    }
    else if (wantsDirectories && !wantsFiles)
    {
      directive |= CompletionDirective.FilterDirs;
    }

    return directive;
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
    if (provider.TryGetParameterInfo(context.CursorPosition, context.Args, out string? paramName, out Type? paramType))
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

    // Use source-generated provider for static completions (AOT-friendly path)
    return provider.GetCompletions(context.CursorPosition, context.Args);
  }
}
