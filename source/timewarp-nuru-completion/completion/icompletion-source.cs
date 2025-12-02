namespace TimeWarp.Nuru;

/// <summary>
/// Provides dynamic completion suggestions for command-line parameters.
/// Implementations query runtime data sources (APIs, databases, configuration)
/// to provide context-aware completion candidates.
/// </summary>
/// <remarks>
/// This interface is used for dynamic completion (EnableDynamicCompletion).
/// The existing CompletionProvider handles static completion (EnableStaticCompletion).
/// </remarks>
public interface ICompletionSource
{
  /// <summary>
  /// Gets completion suggestions based on the current command-line context.
  /// </summary>
  /// <param name="context">The completion context including cursor position and parsed words.</param>
  /// <returns>An enumerable of completion candidates to suggest to the user.</returns>
  IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context);
}

/// <summary>
/// Directives that control shell completion behavior for dynamic completion.
/// Based on Cobra's completion directive system.
/// </summary>
[Flags]
public enum CompletionDirective
{
  /// <summary>
  /// No special directives - allow file completion as fallback.
  /// </summary>
  None = 0,

  /// <summary>
  /// Don't fall back to file/directory completion.
  /// Use this for parameters that accept non-file values (environment names, etc.).
  /// </summary>
  NoFileComp = 4,

  /// <summary>
  /// Don't add a space after the completion.
  /// Use this when the user needs to continue typing (e.g., completing a prefix).
  /// </summary>
  NoSpace = 8,

  /// <summary>
  /// Keep the original order of completions (don't sort alphabetically).
  /// Use this when order is meaningful (e.g., priority-sorted results).
  /// </summary>
  KeepOrder = 64,
}
