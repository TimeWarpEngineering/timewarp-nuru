namespace TimeWarp.Nuru.Completion;

/// <summary>
/// Represents tokenized command-line input ready for completion analysis.
/// </summary>
/// <remarks>
/// <para>
/// This record captures the structure of user input for completion purposes:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="CompletedWords"/>: Words the user has finished typing (space after them)</description></item>
/// <item><description><see cref="PartialWord"/>: The word being typed (null if cursor is after a space)</description></item>
/// <item><description><see cref="HasTrailingSpace"/>: Whether input ends with whitespace</description></item>
/// </list>
/// <para>
/// The key insight is that <see cref="PartialWord"/> is null when <see cref="HasTrailingSpace"/> is true,
/// clearly distinguishing "show all next possibilities" from "filter by what user typed".
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // User types "git " (with space) - wants to see all subcommands
/// var input = new ParsedInput(["git"], null, true);
///
/// // User types "git s" - wants subcommands starting with 's'
/// var input = new ParsedInput(["git"], "s", false);
///
/// // User types "backup --com" - wants options starting with '--com'
/// var input = new ParsedInput(["backup"], "--com", false);
/// </code>
/// </example>
/// <param name="CompletedWords">Words that are fully typed (followed by whitespace).</param>
/// <param name="PartialWord">The word currently being typed, or null if cursor is after whitespace.</param>
/// <param name="HasTrailingSpace">True if input ends with whitespace.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
  "Performance",
  "CA1819:Properties should not return arrays",
  Justification = "Record type with array property is intentional for immutable parsed input")]
public record ParsedInput(
  string[] CompletedWords,
  string? PartialWord,
  bool HasTrailingSpace
)
{
  /// <summary>
  /// Gets the total number of logical words in the input.
  /// </summary>
  /// <remarks>
  /// This counts completed words plus the partial word (if any).
  /// </remarks>
  public int TotalWordCount => CompletedWords.Length + (PartialWord is not null ? 1 : 0);

  /// <summary>
  /// Gets whether the input is empty (no words at all).
  /// </summary>
  public bool IsEmpty => CompletedWords.Length == 0 && PartialWord is null;

  /// <summary>
  /// Gets whether the user is typing an option (word starts with '-').
  /// </summary>
  public bool IsTypingOption => PartialWord?.StartsWith('-') == true;

  /// <summary>
  /// Gets whether the user is typing a long option (word starts with '--').
  /// </summary>
  public bool IsTypingLongOption => PartialWord?.StartsWith("--", StringComparison.Ordinal) == true;

  /// <summary>
  /// Represents empty input.
  /// </summary>
  public static ParsedInput Empty { get; } = new([], null, false);
}
