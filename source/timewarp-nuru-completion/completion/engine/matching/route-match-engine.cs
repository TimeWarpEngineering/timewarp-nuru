namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Matches parsed input against registered routes and computes match states.
/// </summary>
/// <remarks>
/// <para>
/// This is the second stage of the completion pipeline. It takes tokenized input
/// and determines which routes are viable matches and what completions should be offered.
/// </para>
/// <para>
/// The engine:
/// </para>
/// <list type="bullet">
/// <item><description>Matches completed words against route segments</description></item>
/// <item><description>Tracks which options have been used</description></item>
/// <item><description>Determines what can come next for each viable route</description></item>
/// <item><description>Filters non-viable routes early for efficiency</description></item>
/// </list>
/// </remarks>
public sealed class RouteMatchEngine : IRouteMatchEngine
{
  /// <summary>
  /// Gets the singleton instance.
  /// </summary>
  public static RouteMatchEngine Instance { get; } = new();

  private RouteMatchEngine() { }

  /// <inheritdoc />
  public IReadOnlyList<RouteMatchState> Match(ParsedInput input, EndpointCollection endpoints)
  {
    ArgumentNullException.ThrowIfNull(input);
    ArgumentNullException.ThrowIfNull(endpoints);

    List<RouteMatchState> results = [];

    foreach (Endpoint endpoint in endpoints.Endpoints)
    {
      RouteMatchState state = ComputeMatchState(input, endpoint);
      results.Add(state);
    }

    return results;
  }

  /// <summary>
  /// Computes the match state for a single route against the input.
  /// </summary>
  private static RouteMatchState ComputeMatchState(ParsedInput input, Endpoint endpoint)
  {
    CompiledRoute route = endpoint.CompiledRoute;
    IReadOnlyList<RouteMatcher> positionalSegments = route.PositionalMatchers;
    IReadOnlyList<OptionMatcher> options = route.OptionMatchers;

    int segmentIndex = 0;
    int argIndex = 0;
    HashSet<string> optionsUsed = new(StringComparer.OrdinalIgnoreCase);

    // Process all completed words using index-based loop for proper skip handling
    while (argIndex < input.CompletedWords.Length)
    {
      string word = input.CompletedWords[argIndex];

      // First, check if this word is an option
      OptionMatcher? matchedOption = TryMatchOption(word, options, optionsUsed);
      if (matchedOption is not null)
      {
        optionsUsed.Add(matchedOption.MatchPattern);
        if (matchedOption.AlternateForm is not null)
        {
          optionsUsed.Add(matchedOption.AlternateForm);
        }

        argIndex++;

        // If option expects a value and there are more completed words, consume next word
        if (matchedOption.ExpectsValue && argIndex < input.CompletedWords.Length)
        {
          argIndex++;
        }

        continue;
      }

      // Not an option - try to match against positional segments
      if (segmentIndex >= positionalSegments.Count)
      {
        // We have more words than segments can consume
        // Check if route has catch-all
        if (route.HasCatchAll)
        {
          argIndex++;
          continue;
        }

        // Too many arguments - route not viable
        return RouteMatchState.NotViable(endpoint);
      }

      RouteMatcher segment = positionalSegments[segmentIndex];

      if (segment is LiteralMatcher literal)
      {
        // Literal must match exactly (case-insensitive for completion)
        if (!string.Equals(word, literal.Value, StringComparison.OrdinalIgnoreCase))
        {
          return RouteMatchState.NotViable(endpoint);
        }

        segmentIndex++;
        argIndex++;
      }
      else if (segment is ParameterMatcher parameter)
      {
        // Parameter consumes any word
        if (parameter.IsCatchAll)
        {
          // Catch-all consumes all remaining words
          argIndex = input.CompletedWords.Length;
          segmentIndex++;
          break;
        }

        segmentIndex++;
        argIndex++;
      }
    }

    // Determine what can come next
    List<NextCandidate> nextCandidates = ComputeNextCandidates(
      positionalSegments,
      options,
      segmentIndex,
      optionsUsed,
      input.PartialWord
    );

    // Check if this is an exact match (all required segments consumed)
    bool isExactMatch = IsExactMatch(positionalSegments, segmentIndex);

    return new RouteMatchState(
      endpoint,
      IsViable: true,
      IsExactMatch: isExactMatch,
      SegmentsMatched: segmentIndex,
      ArgsConsumed: argIndex,
      OptionsUsed: optionsUsed,
      NextCandidates: nextCandidates
    );
  }

  /// <summary>
  /// Tries to match a word against available options.
  /// </summary>
  private static OptionMatcher? TryMatchOption(
    string word,
    IReadOnlyList<OptionMatcher> options,
    HashSet<string> optionsUsed)
  {
    // Must start with dash to be an option
    if (!word.StartsWith('-'))
    {
      return null;
    }

    foreach (OptionMatcher option in options)
    {
      if (option.TryMatch(word, out _))
      {
        // Check if already used (unless repeatable)
        if (!option.IsRepeated &&
            (optionsUsed.Contains(option.MatchPattern) ||
             (option.AlternateForm is not null && optionsUsed.Contains(option.AlternateForm))))
        {
          continue;
        }

        return option;
      }
    }

    return null;
  }

  /// <summary>
  /// Computes what candidates can come next for this route.
  /// </summary>
  private static List<NextCandidate> ComputeNextCandidates(
    IReadOnlyList<RouteMatcher> positionalSegments,
    IReadOnlyList<OptionMatcher> options,
    int segmentIndex,
    HashSet<string> optionsUsed,
    string? partialWord)
  {
    List<NextCandidate> candidates = [];

    // Add next positional segment if available
    if (segmentIndex < positionalSegments.Count)
    {
      RouteMatcher segment = positionalSegments[segmentIndex];

      if (segment is LiteralMatcher literal)
      {
        // Filter by partial word if present
        if (MatchesPartial(literal.Value, partialWord))
        {
          candidates.Add(new NextCandidate(
            CandidateKind.Literal,
            literal.Value,
            AlternateValue: null,
            Description: null,
            ParameterType: null,
            IsRequired: true
          ));
        }
      }
      else if (segment is ParameterMatcher parameter)
      {
        // Parameters are always candidates (user provides value)
        // Only add if not filtering for option prefix
        if (partialWord?.StartsWith('-') != true)
        {
          candidates.Add(new NextCandidate(
            CandidateKind.Parameter,
            $"<{parameter.Name}>",
            AlternateValue: null,
            Description: parameter.Description,
            ParameterType: parameter.Constraint,
            IsRequired: !parameter.IsOptional
          ));
        }
      }
    }

    // Only add options if there are no more literal segments remaining.
    // Options belong "after" the command structure (literals), so they should
    // only be suggested once all literals have been matched.
    // For example, "git log --count {n}" should only show --count after "git log ",
    // not after just "git ".
    bool hasRemainingLiterals = false;
    for (int i = segmentIndex; i < positionalSegments.Count; i++)
    {
      if (positionalSegments[i] is LiteralMatcher)
      {
        hasRemainingLiterals = true;
        break;
      }
    }

    if (hasRemainingLiterals)
    {
      return candidates;
    }

    // Add unused options
    foreach (OptionMatcher option in options)
    {
      // Skip if already used (unless repeatable)
      if (!option.IsRepeated &&
          (optionsUsed.Contains(option.MatchPattern) ||
           (option.AlternateForm is not null && optionsUsed.Contains(option.AlternateForm))))
      {
        continue;
      }

      // Check if option matches partial word
      bool matchesLong = MatchesPartial(option.MatchPattern, partialWord);
      bool matchesShort = option.AlternateForm is not null &&
                          MatchesPartial(option.AlternateForm, partialWord);

      if (matchesLong || matchesShort || partialWord is null)
      {
        // Add long form if it matches
        if (matchesLong || partialWord is null)
        {
          candidates.Add(new NextCandidate(
            CandidateKind.Option,
            option.MatchPattern,
            option.AlternateForm,
            option.Description,
            ParameterType: null,
            IsRequired: !option.IsOptional
          ));
        }

        // Add short form separately if it matches but long doesn't
        if (option.AlternateForm is not null && matchesShort && !matchesLong)
        {
          candidates.Add(new NextCandidate(
            CandidateKind.Option,
            option.AlternateForm,
            option.MatchPattern,
            option.Description,
            ParameterType: null,
            IsRequired: !option.IsOptional
          ));
        }
      }
    }

    return candidates;
  }

  /// <summary>
  /// Checks if a value matches the partial word filter.
  /// </summary>
  private static bool MatchesPartial(string value, string? partialWord)
  {
    if (partialWord is null)
    {
      return true;
    }

    return value.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Checks if all required segments have been matched.
  /// </summary>
  private static bool IsExactMatch(IReadOnlyList<RouteMatcher> positionalSegments, int segmentIndex)
  {
    // Check if we've consumed all segments or remaining are optional
    for (int i = segmentIndex; i < positionalSegments.Count; i++)
    {
      if (positionalSegments[i] is ParameterMatcher param && !param.IsOptional && !param.IsCatchAll)
      {
        return false;
      }

      if (positionalSegments[i] is LiteralMatcher)
      {
        return false;
      }
    }

    return true;
  }
}
