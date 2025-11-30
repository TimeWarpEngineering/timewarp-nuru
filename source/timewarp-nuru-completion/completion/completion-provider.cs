namespace TimeWarp.Nuru.Completion;

using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

/// <summary>
/// Provides completion candidates by analyzing route patterns and current input.
/// </summary>
/// <remarks>
/// <para>
/// This class delegates to <see cref="CompletionEngine"/> which uses a three-stage pipeline:
/// </para>
/// <list type="number">
/// <item><description><see cref="InputTokenizer"/>: Parse input into tokens</description></item>
/// <item><description><see cref="RouteMatchEngine"/>: Match tokens against routes</description></item>
/// <item><description><see cref="CandidateGenerator"/>: Generate candidates from matches</description></item>
/// </list>
/// <para>
/// Additionally handles enum expansion by resolving parameter type constraints
/// to actual CLR types and enumerating enum values.
/// </para>
/// </remarks>
public class CompletionProvider
{
  private readonly CompletionEngine Engine;
  private readonly ITypeConverterRegistry TypeConverterRegistry;

  /// <summary>
  /// Creates a new instance of <see cref="CompletionProvider"/>.
  /// </summary>
  /// <param name="typeConverterRegistry">The type converter registry for resolving parameter types.</param>
  /// <param name="loggerFactory">Optional logger factory for diagnostic output.</param>
  public CompletionProvider(ITypeConverterRegistry typeConverterRegistry, ILoggerFactory? loggerFactory = null)
  {
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));

    ILogger<CompletionEngine>? logger = loggerFactory?.CreateLogger<CompletionEngine>();
    Engine = new CompletionEngine(logger: logger);
  }

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
  public ReadOnlyCollection<CompletionCandidate> GetCompletions(
    CompletionContext context,
    EndpointCollection endpoints)
  {
    IReadOnlyCollection<CompletionCandidate> candidates = Engine.GetCompletions(context, endpoints);

    // Expand enum parameters to individual enum values
    List<CompletionCandidate> expanded = ExpandEnumCandidates(candidates, context);

    return new ReadOnlyCollection<CompletionCandidate>(expanded);
  }

  /// <summary>
  /// Expands parameter candidates with enum type constraints into individual enum value candidates.
  /// </summary>
  private List<CompletionCandidate> ExpandEnumCandidates(
    IReadOnlyCollection<CompletionCandidate> candidates,
    CompletionContext context)
  {
    List<CompletionCandidate> result = [];
    string? partialWord = context.Args.Length > 0 && !context.HasTrailingSpace
      ? context.Args[^1]
      : null;

    foreach (CompletionCandidate candidate in candidates)
    {
      // Check if this is a parameter candidate that might be an enum
      if (candidate.Type == CompletionType.Parameter && candidate.ParameterType is not null)
      {
        // Try to get the type converter for this constraint
        IRouteTypeConverter? converter = TypeConverterRegistry.GetConverterByConstraint(candidate.ParameterType);
        if (converter?.TargetType?.IsEnum == true)
        {
          // Expand to enum values
          foreach (string enumName in Enum.GetNames(converter.TargetType))
          {
            // Filter by partial word if present (case-insensitive)
            if (string.IsNullOrEmpty(partialWord) ||
                enumName.StartsWith(partialWord, StringComparison.OrdinalIgnoreCase))
            {
              result.Add(new CompletionCandidate(
                enumName,
                $"{converter.TargetType.Name}.{enumName}",
                CompletionType.Enum
              ));
            }
          }

          continue; // Don't add the original parameter candidate
        }
      }

      // Not an enum - add as-is
      result.Add(candidate);
    }

    // Re-sort after adding enum values (enums should come after commands but before other parameters)
    result.Sort((a, b) =>
    {
      int typeCompare = GetTypeSortOrder(a.Type).CompareTo(GetTypeSortOrder(b.Type));
      if (typeCompare != 0) return typeCompare;
      return string.Compare(a.Value, b.Value, StringComparison.OrdinalIgnoreCase);
    });

    return result;
  }

  /// <summary>
  /// Get sort order for completion types.
  /// Commands and Enum values come first, Options come last.
  /// </summary>
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
