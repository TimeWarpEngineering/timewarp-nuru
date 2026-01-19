namespace TimeWarp.Nuru;

/// <summary>
/// Provides route information for REPL features like tab completion and syntax highlighting.
/// The source generator implements this interface with compile-time extracted route data.
/// </summary>
public interface IReplRouteProvider
{
  /// <summary>
  /// Gets all known command prefixes for completion and syntax highlighting.
  /// These are the leading literal segments of all routes (e.g., ["greet", "git commit", "status"]).
  /// </summary>
  IReadOnlyList<string> GetCommandPrefixes();

  /// <summary>
  /// Gets completion candidates for the current input.
  /// </summary>
  /// <param name="args">The parsed command-line arguments so far.</param>
  /// <param name="hasTrailingSpace">Whether the input ends with a space (completing next arg vs current).</param>
  /// <returns>Completion candidates matching the current input state.</returns>
  IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace);

  /// <summary>
  /// Checks if a token is a known command or subcommand prefix.
  /// Used for syntax highlighting to color known commands differently.
  /// </summary>
  /// <param name="token">The token to check.</param>
  /// <returns>True if the token matches a known command prefix.</returns>
  bool IsKnownCommand(string token);
}

/// <summary>
/// A fallback route provider that provides no completion data.
/// Used when the source generator hasn't emitted a real provider.
/// </summary>
internal sealed class EmptyReplRouteProvider : IReplRouteProvider
{
  public static readonly EmptyReplRouteProvider Instance = new();

  private EmptyReplRouteProvider() { }

  public IReadOnlyList<string> GetCommandPrefixes() => [];

  public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace) => [];

  public bool IsKnownCommand(string token) => false;
}
