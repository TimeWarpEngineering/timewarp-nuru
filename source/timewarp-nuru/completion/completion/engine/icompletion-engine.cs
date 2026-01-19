namespace TimeWarp.Nuru;

/// <summary>
/// Interface for the completion engine that generates completion candidates.
/// </summary>
/// <remarks>
/// <para>
/// The completion engine uses a three-stage pipeline:
/// </para>
/// <list type="number">
/// <item><description>Tokenize input into completed words and partial word</description></item>
/// <item><description>Match against all registered routes</description></item>
/// <item><description>Generate and filter completion candidates</description></item>
/// </list>
/// </remarks>
public interface ICompletionEngine
{
  /// <summary>
  /// Get completion candidates for the given context.
  /// </summary>
  /// <param name="context">The completion context containing args and cursor position.</param>
  /// <param name="endpoints">The collection of all registered endpoints.</param>
  /// <returns>
  /// A read-only collection of completion candidates, sorted by type
  /// priority (commands first, options last) and alphabetically within
  /// each type group.
  /// </returns>
  IReadOnlyCollection<CompletionCandidate> GetCompletions(
    CompletionContext context,
    EndpointCollection endpoints);
}
