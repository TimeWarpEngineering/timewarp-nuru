namespace TimeWarp.Nuru;

/// <summary>
/// Represents the match state of a single route against the input.
/// </summary>
/// <remarks>
/// <para>
/// This record captures how well a route matches the current input:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="IsViable"/>: Whether this route could still match</description></item>
/// <item><description><see cref="SegmentsMatched"/>: How many route segments have been consumed</description></item>
/// <item><description><see cref="ArgsConsumed"/>: How many input words have been consumed</description></item>
/// <item><description><see cref="OptionsUsed"/>: Which options are already present in the input</description></item>
/// <item><description><see cref="NextCandidates"/>: What can come next for this route</description></item>
/// </list>
/// </remarks>
/// <param name="Endpoint">The endpoint being matched.</param>
/// <param name="IsViable">True if route could still match with more input.</param>
/// <param name="IsExactMatch">True if route matches exactly with no more required input.</param>
/// <param name="SegmentsMatched">Number of route segments successfully matched.</param>
/// <param name="ArgsConsumed">Number of input arguments consumed.</param>
/// <param name="OptionsUsed">Set of option names already present in input.</param>
/// <param name="NextCandidates">What can come next for this route.</param>
public record RouteMatchState(
  Endpoint Endpoint,
  bool IsViable,
  bool IsExactMatch,
  int SegmentsMatched,
  int ArgsConsumed,
  IReadOnlySet<string> OptionsUsed,
  IReadOnlyList<NextCandidate> NextCandidates
)
{
  /// <summary>
  /// Creates a non-viable match state for a route that doesn't match.
  /// </summary>
  public static RouteMatchState NotViable(Endpoint endpoint) =>
    new(
      endpoint,
      IsViable: false,
      IsExactMatch: false,
      SegmentsMatched: 0,
      ArgsConsumed: 0,
      OptionsUsed: new HashSet<string>(),
      NextCandidates: []
    );
}
