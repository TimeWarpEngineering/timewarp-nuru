namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Generates completion candidates from route match states.
/// </summary>
/// <remarks>
/// <para>
/// The CandidateGenerator is the third stage of the completion pipeline:
/// </para>
/// <list type="number">
/// <item><description>InputTokenizer: Parse raw input into tokens</description></item>
/// <item><description>RouteMatchEngine: Match tokens against routes</description></item>
/// <item><description><b>CandidateGenerator: Generate candidates from matches</b></description></item>
/// </list>
/// <para>
/// This component aggregates candidates from all viable route matches,
/// applies partial word filtering, removes duplicates, and sorts results.
/// </para>
/// </remarks>
public interface ICandidateGenerator
{
  /// <summary>
  /// Generate completion candidates from viable route matches.
  /// </summary>
  /// <param name="matchStates">Route match states from the RouteMatchEngine.</param>
  /// <param name="partialWord">
  /// The partial word to filter by (null means show all candidates).
  /// Filtering is case-insensitive prefix matching.
  /// </param>
  /// <returns>
  /// A read-only collection of completion candidates, sorted by type
  /// priority (commands first, options last) and alphabetically within
  /// each type group.
  /// </returns>
  /// <example>
  /// <code>
  /// IReadOnlyCollection&lt;CompletionCandidate&gt; candidates = generator.Generate(matchStates, partialWord: "st");
  /// // Returns candidates starting with "st": "status", "stop", etc.
  /// </code>
  /// </example>
  IReadOnlyCollection<CompletionCandidate> Generate(
    IEnumerable<RouteMatchState> matchStates,
    string? partialWord);
}
