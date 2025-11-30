namespace TimeWarp.Nuru.Completion;

using Microsoft.Extensions.Logging;

/// <summary>
/// Unified completion engine using pipeline architecture.
/// </summary>
/// <remarks>
/// <para>
/// This engine orchestrates the three-stage completion pipeline:
/// </para>
/// <list type="number">
/// <item><description><see cref="InputTokenizer"/>: Parse raw input into tokens</description></item>
/// <item><description><see cref="RouteMatchEngine"/>: Match tokens against routes</description></item>
/// <item><description><see cref="CandidateGenerator"/>: Generate candidates from matches</description></item>
/// </list>
/// <para>
/// The pipeline design ensures a single code path for all completion scenarios,
/// eliminating the ad hoc branching that caused inconsistent behavior.
/// </para>
/// </remarks>
public sealed class CompletionEngine : ICompletionEngine
{
  private readonly IRouteMatchEngine MatchEngine;
  private readonly ICandidateGenerator CandidateGenerator;
  private readonly ILogger<CompletionEngine>? Logger;

  /// <summary>
  /// Shared singleton instance for scenarios not using DI.
  /// </summary>
  public static CompletionEngine Instance { get; } = new();

  /// <summary>
  /// Creates a new instance of <see cref="CompletionEngine"/>.
  /// </summary>
  /// <param name="matchEngine">The route match engine (optional, uses default if null).</param>
  /// <param name="candidateGenerator">The candidate generator (optional, uses default if null).</param>
  /// <param name="logger">Optional logger for diagnostic output.</param>
  public CompletionEngine(
    IRouteMatchEngine? matchEngine = null,
    ICandidateGenerator? candidateGenerator = null,
    ILogger<CompletionEngine>? logger = null)
  {
    MatchEngine = matchEngine ?? RouteMatchEngine.Instance;
    CandidateGenerator = candidateGenerator ?? Completion.CandidateGenerator.Instance;
    Logger = logger;
  }

  /// <inheritdoc/>
  public IReadOnlyCollection<CompletionCandidate> GetCompletions(
    CompletionContext context,
    EndpointCollection endpoints)
  {
    ArgumentNullException.ThrowIfNull(context);
    ArgumentNullException.ThrowIfNull(endpoints);

    // Stage 1: Tokenize input
    ParsedInput parsedInput = InputTokenizer.FromArgs(context.Args, context.HasTrailingSpace);

    if (Logger is not null)
    {
      LoggerMessages.TokenizedInput(
        Logger,
        string.Join(", ", parsedInput.CompletedWords),
        parsedInput.PartialWord ?? "(null)",
        parsedInput.HasTrailingSpace,
        null);
    }

    // Stage 2: Match against all routes
    IReadOnlyList<RouteMatchState> matchStates = MatchEngine.Match(parsedInput, endpoints);

    int viableCount = 0;
    foreach (RouteMatchState state in matchStates)
    {
      if (state.IsViable)
      {
        viableCount++;
      }
    }

    if (Logger is not null)
    {
      LoggerMessages.MatchedRoutes(Logger, viableCount, matchStates.Count, null);
    }

    // Stage 3: Generate candidates from viable matches
    IReadOnlyCollection<CompletionCandidate> candidates = CandidateGenerator.Generate(
      matchStates,
      parsedInput.PartialWord);

    if (Logger is not null)
    {
      LoggerMessages.GeneratedCandidates(Logger, candidates.Count, null);
    }

    return candidates;
  }

  /// <summary>
  /// Get completions from raw input string.
  /// </summary>
  /// <param name="input">The raw command-line input.</param>
  /// <param name="endpoints">The collection of all registered endpoints.</param>
  /// <returns>Sorted, deduplicated completion candidates.</returns>
  public IReadOnlyCollection<CompletionCandidate> GetCompletions(
    string input,
    EndpointCollection endpoints)
  {
    ArgumentNullException.ThrowIfNull(endpoints);

    // Parse input - trailing space is determined by the input itself
    ParsedInput parsedInput = InputTokenizer.Parse(input ?? "");

    if (Logger is not null)
    {
      LoggerMessages.ParsedRawInput(
        Logger,
        input ?? "",
        string.Join(", ", parsedInput.CompletedWords),
        parsedInput.PartialWord ?? "(null)",
        null);
    }

    // Match against all routes
    IReadOnlyList<RouteMatchState> matchStates = MatchEngine.Match(parsedInput, endpoints);

    // Generate candidates
    return CandidateGenerator.Generate(matchStates, parsedInput.PartialWord);
  }
}
