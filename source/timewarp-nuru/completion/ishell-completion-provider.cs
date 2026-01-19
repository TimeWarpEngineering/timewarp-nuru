namespace TimeWarp.Nuru;

/// <summary>
/// Provides static completion data for shell completion.
/// The source generator implements this interface with compile-time extracted route data.
/// This is similar to <see cref="IReplRouteProvider"/> but optimized for shell completion's
/// `__complete` callback protocol.
/// </summary>
public interface IShellCompletionProvider
{
  /// <summary>
  /// Gets completion candidates for the current input context.
  /// </summary>
  /// <param name="cursorIndex">The zero-based index of the word being completed.</param>
  /// <param name="words">All words on the command line (including app name at index 0).</param>
  /// <returns>Completion candidates from static route data.</returns>
  IEnumerable<CompletionCandidate> GetCompletions(int cursorIndex, string[] words);

  /// <summary>
  /// Attempts to determine which parameter is being completed based on the input context.
  /// Used to look up custom <see cref="ICompletionSource"/> implementations.
  /// </summary>
  /// <param name="cursorIndex">The zero-based index of the word being completed.</param>
  /// <param name="words">All words on the command line.</param>
  /// <param name="parameterName">The name of the parameter being completed, if detected.</param>
  /// <param name="parameterTypeName">The fully qualified type name of the parameter, if detected.</param>
  /// <returns>True if a parameter was detected; otherwise, false.</returns>
  bool TryGetParameterInfo(int cursorIndex, string[] words, out string? parameterName, out string? parameterTypeName);
}

/// <summary>
/// A fallback provider that returns no completions.
/// Used when the source generator hasn't emitted a real provider.
/// </summary>
internal sealed class EmptyShellCompletionProvider : IShellCompletionProvider
{
  public static readonly EmptyShellCompletionProvider Instance = new();

  private EmptyShellCompletionProvider() { }

  public IEnumerable<CompletionCandidate> GetCompletions(int cursorIndex, string[] words) => [];

  public bool TryGetParameterInfo(int cursorIndex, string[] words, out string? parameterName, out string? parameterTypeName)
  {
    parameterName = null;
    parameterTypeName = null;
    return false;
  }
}
