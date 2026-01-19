namespace TimeWarp.Nuru;

/// <summary>
/// Default implementation of <see cref="ICandidateGenerator"/>.
/// </summary>
/// <remarks>
/// <para>
/// This implementation:
/// </para>
/// <list type="bullet">
/// <item><description>Aggregates <see cref="NextCandidate"/>s from all viable route matches</description></item>
/// <item><description>Filters by partial word using case-insensitive prefix matching</description></item>
/// <item><description>Removes duplicates by value</description></item>
/// <item><description>Sorts by type priority (commands first, options last)</description></item>
/// <item><description>Sorts alphabetically within each type group</description></item>
/// </list>
/// </remarks>
public sealed class CandidateGenerator : ICandidateGenerator
{
  /// <summary>
  /// Shared singleton instance for scenarios not using DI.
  /// </summary>
  public static CandidateGenerator Instance { get; } = new();

  /// <inheritdoc/>
  public IReadOnlyCollection<CompletionCandidate> Generate(
    IEnumerable<RouteMatchState> matchStates,
    string? partialWord)
  {
    ArgumentNullException.ThrowIfNull(matchStates);

    List<CompletionCandidate> candidates = [];
    HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

    foreach (RouteMatchState state in matchStates)
    {
      if (!state.IsViable)
      {
        continue;
      }

      foreach (NextCandidate next in state.NextCandidates)
      {
        AddCandidateIfMatch(candidates, seen, next, partialWord);
      }
    }

    // Sort by type priority, then alphabetically
    candidates.Sort(CompareCompletionCandidates);

    return candidates.AsReadOnly();
  }

  /// <summary>
  /// Add a candidate if it matches the partial word filter and hasn't been seen.
  /// </summary>
  /// <remarks>
  /// Parameter candidates are NOT filtered by partial word here because they may
  /// represent enum types that need to be expanded later. The CompletionProvider
  /// expands enum parameters and filters them appropriately.
  /// </remarks>
  private static void AddCandidateIfMatch(
    List<CompletionCandidate> candidates,
    HashSet<string> seen,
    NextCandidate next,
    string? partialWord)
  {
    // Parameters are passed through without filtering - they may be enum types
    // that need expansion in CompletionProvider. The enum values will be filtered there.
    if (next.Kind == CandidateKind.Parameter)
    {
      if (seen.Add(next.Value))
      {
        candidates.Add(new CompletionCandidate(
          next.Value,
          next.Description,
          MapKindToType(next.Kind, next.ParameterType),
          next.ParameterType));
      }

      return;
    }

    // Check primary value for non-parameter candidates
    if (MatchesPartial(next.Value, partialWord) && seen.Add(next.Value))
    {
      candidates.Add(new CompletionCandidate(
        next.Value,
        next.Description,
        MapKindToType(next.Kind, next.ParameterType),
        null));
    }

    // Check alternate value (e.g., short form "-v" for "--verbose")
    if (next.AlternateValue is not null &&
        MatchesPartial(next.AlternateValue, partialWord) &&
        seen.Add(next.AlternateValue))
    {
      candidates.Add(new CompletionCandidate(
        next.AlternateValue,
        next.Description,
        CompletionType.Option));
    }
  }

  /// <summary>
  /// Check if a value matches the partial word (case-insensitive prefix match).
  /// </summary>
  private static bool MatchesPartial(string value, string? partialWord)
  {
    if (string.IsNullOrEmpty(partialWord))
    {
      return true;
    }

    return value.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Map <see cref="CandidateKind"/> to <see cref="CompletionType"/>.
  /// </summary>
  private static CompletionType MapKindToType(CandidateKind kind, string? parameterType)
  {
    return kind switch
    {
      CandidateKind.Literal => CompletionType.Command,
      CandidateKind.Option => CompletionType.Option,
      CandidateKind.Parameter => MapParameterType(parameterType),
      _ => CompletionType.Parameter
    };
  }

  /// <summary>
  /// Map parameter type string to <see cref="CompletionType"/>.
  /// </summary>
  private static CompletionType MapParameterType(string? parameterType)
  {
    // Future: could detect file/directory/enum from parameterType
    // For now, return generic Parameter type
    return parameterType switch
    {
      "file" => CompletionType.File,
      "directory" or "dir" => CompletionType.Directory,
      _ => CompletionType.Parameter
    };
  }

  /// <summary>
  /// Compare two completion candidates for sorting.
  /// </summary>
  private static int CompareCompletionCandidates(CompletionCandidate x, CompletionCandidate y)
  {
    int typeOrder = GetTypeSortOrder(x.Type).CompareTo(GetTypeSortOrder(y.Type));
    if (typeOrder != 0)
    {
      return typeOrder;
    }

    return string.Compare(x.Value, y.Value, StringComparison.OrdinalIgnoreCase);
  }

  /// <summary>
  /// Get sort order for completion type.
  /// </summary>
  /// <remarks>
  /// Priority order:
  /// <list type="number">
  /// <item><description>Commands - most likely what user wants</description></item>
  /// <item><description>Enum values - constrained valid values</description></item>
  /// <item><description>Parameters - type hints</description></item>
  /// <item><description>Files/Directories - path completions</description></item>
  /// <item><description>Custom - user-defined completions</description></item>
  /// <item><description>Options - flags come last</description></item>
  /// </list>
  /// </remarks>
  private static int GetTypeSortOrder(CompletionType type)
  {
    return type switch
    {
      CompletionType.Command => 0,
      CompletionType.Enum => 1,
      CompletionType.Parameter => 2,
      CompletionType.File => 3,
      CompletionType.Directory => 4,
      CompletionType.Custom => 5,
      CompletionType.Option => 6,
      _ => 99
    };
  }
}
