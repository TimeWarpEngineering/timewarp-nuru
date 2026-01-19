namespace TimeWarp.Nuru;

/// <summary>
/// Interface for tokenizing command-line input for completion analysis.
/// </summary>
/// <remarks>
/// <para>
/// Implementations of this interface are responsible for parsing raw input
/// into structured tokens that the completion engine can analyze.
/// </para>
/// <para>
/// The default implementation is the static <see cref="InputTokenizer"/> class.
/// Custom implementations can be provided for special parsing requirements.
/// </para>
/// </remarks>
public interface IInputTokenizer
{
  /// <summary>
  /// Tokenizes raw command-line input for completion analysis.
  /// </summary>
  /// <param name="input">The raw command-line input string.</param>
  /// <param name="hasTrailingSpace">True if the input ends with whitespace.</param>
  /// <returns>A <see cref="ParsedInput"/> representing the tokenized input.</returns>
  ParsedInput Tokenize(string input, bool hasTrailingSpace);
}

/// <summary>
/// Default implementation of <see cref="IInputTokenizer"/> that wraps the static <see cref="InputTokenizer"/>.
/// </summary>
public sealed class DefaultInputTokenizer : IInputTokenizer
{
  /// <summary>
  /// Gets the singleton instance.
  /// </summary>
  public static DefaultInputTokenizer Instance { get; } = new();

  private DefaultInputTokenizer() { }

  /// <inheritdoc />
  public ParsedInput Tokenize(string input, bool hasTrailingSpace)
  {
    // If hasTrailingSpace is explicitly provided, we may need to adjust
    // what InputTokenizer.Parse returns
    ParsedInput parsed = InputTokenizer.Parse(input);

    // If caller says there's trailing space but Parse didn't detect it
    // (e.g., input was trimmed before being passed), adjust the result
    if (hasTrailingSpace && !parsed.HasTrailingSpace && parsed.PartialWord is not null)
    {
      // Move partial word to completed words
      string[] newCompleted = [.. parsed.CompletedWords, parsed.PartialWord];
      return new ParsedInput(newCompleted, null, true);
    }

    return parsed;
  }
}
