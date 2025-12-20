namespace TimeWarp.Terminal;

/// <summary>
/// Represents a horizontal rule (divider line) for terminal output.
/// Can optionally include a centered title.
/// </summary>
/// <example>
/// <code>
/// // Simple rule
/// var rule = new Rule();
///
/// // Rule with title
/// var rule = new Rule { Title = "Section" };
///
/// // Rule with styling
/// var rule = new Rule
/// {
///     Title = "Results",
///     Style = LineStyle.Doubled,
///     Color = AnsiColors.Cyan
/// };
/// </code>
/// </example>
public sealed class Rule
{
  /// <summary>
  /// Gets or sets the optional title to display centered in the rule.
  /// Can include ANSI color codes.
  /// </summary>
  public string? Title { get; set; }

  /// <summary>
  /// Gets or sets the line style for the rule.
  /// Defaults to <see cref="LineStyle.Thin"/>.
  /// </summary>
  public LineStyle Style { get; set; } = LineStyle.Thin;

  /// <summary>
  /// Gets or sets the ANSI color code for the rule line.
  /// Defaults to no color (uses terminal default).
  /// </summary>
  public string? Color { get; set; }

  /// <summary>
  /// Gets or sets the width of the rule.
  /// If null, uses the terminal width.
  /// </summary>
  public int? Width { get; set; }

  /// <summary>
  /// Renders the rule to a string.
  /// </summary>
  /// <param name="terminalWidth">The terminal width to use if <see cref="Width"/> is not set.</param>
  /// <returns>The rendered rule string.</returns>
  public string Render(int terminalWidth = 80)
  {
    int width = Width ?? terminalWidth;
    char lineChar = LineChars.GetHorizontal(Style);

    string line;
    if (string.IsNullOrEmpty(Title))
    {
      // Simple line without title
      line = new string(lineChar, width);
    }
    else
    {
      // Line with centered title
      int titleVisibleLength = AnsiStringUtils.GetVisibleLength(Title);

      // Need at least: 1 char + space + title + space + 1 char
      int minimumWidth = titleVisibleLength + 4;
      if (width < minimumWidth)
      {
        // Not enough space, just show the title
        line = Title;
      }
      else
      {
        // Calculate padding for centered title
        int availableForLines = width - titleVisibleLength - 2; // -2 for spaces around title
        int leftLineLength = availableForLines / 2;
        int rightLineLength = availableForLines - leftLineLength;

        string leftLine = new(lineChar, leftLineLength);
        string rightLine = new(lineChar, rightLineLength);

        line = $"{leftLine} {Title} {rightLine}";
      }
    }

    // Apply color if specified
    if (!string.IsNullOrEmpty(Color))
    {
      // Only colorize the line characters, not the title (title may have its own colors)
      if (string.IsNullOrEmpty(Title))
      {
        line = Color + line + AnsiColors.Reset;
      }
      else
      {
        // Colorize just the line parts, preserve title styling
        int titleVisibleLength = AnsiStringUtils.GetVisibleLength(Title);
        int availableForLines = width - titleVisibleLength - 2;
        int leftLineLength = availableForLines / 2;
        int rightLineLength = availableForLines - leftLineLength;

        string leftLine = new(lineChar, leftLineLength);
        string rightLine = new(lineChar, rightLineLength);

        line = $"{Color}{leftLine}{AnsiColors.Reset} {Title} {Color}{rightLine}{AnsiColors.Reset}";
      }
    }

    return line;
  }
}

/// <summary>
/// Fluent builder for creating <see cref="Rule"/> instances.
/// </summary>
/// <example>
/// <code>
/// var rule = new RuleBuilder()
///     .Title("Configuration")
///     .Style(LineStyle.Doubled)
///     .Color(AnsiColors.Cyan)
///     .Build();
/// </code>
/// </example>
public sealed class RuleBuilder : IBuilder<Rule>
{
  private readonly Rule _rule = new();

  /// <summary>
  /// Sets the title for the rule.
  /// </summary>
  /// <param name="title">The title to display centered in the rule.</param>
  /// <returns>This builder for method chaining.</returns>
  public RuleBuilder Title(string title)
  {
    _rule.Title = title;
    return this;
  }

  /// <summary>
  /// Sets the line style for the rule.
  /// </summary>
  /// <param name="style">The line style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public RuleBuilder Style(LineStyle style)
  {
    _rule.Style = style;
    return this;
  }

  /// <summary>
  /// Sets the color for the rule line.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public RuleBuilder Color(string color)
  {
    _rule.Color = color;
    return this;
  }

  /// <summary>
  /// Sets a fixed width for the rule.
  /// </summary>
  /// <param name="width">The width in characters.</param>
  /// <returns>This builder for method chaining.</returns>
  public RuleBuilder Width(int width)
  {
    _rule.Width = width;
    return this;
  }

  /// <summary>
  /// Builds the configured <see cref="Rule"/> instance.
  /// </summary>
  /// <returns>The configured rule.</returns>
  public Rule Build() => _rule;

  /// <summary>
  /// Converts the builder to a <see cref="Rule"/>.
  /// Alternate method for languages that don't support implicit operators.
  /// </summary>
  /// <returns>The configured rule.</returns>
  public Rule ToRule() => Build();
}
