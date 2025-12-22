namespace TimeWarp.Terminal;

/// <summary>
/// Extension methods for writing <see cref="Rule"/> widgets to an <see cref="ITerminal"/>.
/// </summary>
/// <example>
/// <code>
/// // Simple horizontal line
/// terminal.WriteRule();
///
/// // With centered title
/// terminal.WriteRule("Section Title");
///
/// // With styling
/// terminal.WriteRule("Results".Cyan().Bold());
///
/// // Fluent builder
/// terminal.WriteRule(rule => rule
///     .Title("Configuration")
///     .Style(LineStyle.Doubled)
///     .Color(AnsiColors.Cyan));
/// </code>
/// </example>
public static class TerminalRuleExtensions
{
  /// <summary>
  /// Writes a simple horizontal rule to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="style">The line style to use. Defaults to <see cref="LineStyle.Thin"/>.</param>
  public static void WriteRule(this ITerminal terminal, LineStyle style = LineStyle.Thin)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    Rule rule = new() { Style = style };
    string rendered = rule.Render(terminal.WindowWidth);
    terminal.WriteLine(rendered);
  }

  /// <summary>
  /// Writes a horizontal rule with a centered title to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="title">The title to display centered in the rule. Can include ANSI styling.</param>
  /// <param name="style">The line style to use. Defaults to <see cref="LineStyle.Thin"/>.</param>
  public static void WriteRule(this ITerminal terminal, string title, LineStyle style = LineStyle.Thin)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    Rule rule = new() { Title = title, Style = style };
    string rendered = rule.Render(terminal.WindowWidth);
    terminal.WriteLine(rendered);
  }

  /// <summary>
  /// Writes a horizontal rule configured via a builder action to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="configure">An action to configure the rule using a <see cref="RuleBuilder"/>.</param>
  /// <example>
  /// <code>
/// terminal.WriteRule(rule => rule
///     .Title("Configuration")
///     .Style(LineStyle.Doubled)
///     .Color(AnsiColors.Cyan));
/// </code>
/// </example>
public static void WriteRule(this ITerminal terminal, Action<RuleBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(configure);

    RuleBuilder builder = new();
    configure(builder);

    Rule rule = builder.Build();
    string rendered = rule.Render(terminal.WindowWidth);
    terminal.WriteLine(rendered);
  }

  /// <summary>
  /// Writes a pre-configured <see cref="Rule"/> to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="rule">The rule to write.</param>
  public static void WriteRule(this ITerminal terminal, Rule rule)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(rule);

    string rendered = rule.Render(terminal.WindowWidth);
    terminal.WriteLine(rendered);
  }
}
