namespace TimeWarp.Terminal;

/// <summary>
/// Nested fluent builder for constructing <see cref="Rule"/> instances within a fluent chain.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This builder wraps <see cref="RuleBuilder"/> and implements <see cref="INestedBuilder{TParent}"/>
/// to enable fluent API patterns where rule configuration returns to the parent context.
/// </para>
/// <para>
/// For standalone rule building, use <see cref="RuleBuilder"/> directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Nested usage in fluent chain
/// terminal.Rule(r => r
///     .Title("Configuration")
///     .Style(LineStyle.Doubled)
///     .Color(AnsiColors.Cyan)
///     .Done());                    // Builds rule, renders, returns terminal
/// </code>
/// </example>
public sealed class NestedRuleBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly RuleBuilder _inner = new();
  private readonly TParent _parent;
  private readonly Action<Rule> _onBuild;

  /// <summary>
  /// Initializes a new instance of the <see cref="NestedRuleBuilder{TParent}"/> class.
  /// </summary>
  /// <param name="parent">The parent builder to return to when <see cref="Done"/> is called.</param>
  /// <param name="onBuild">Callback invoked with the built rule when <see cref="Done"/> is called.</param>
  internal NestedRuleBuilder(TParent parent, Action<Rule> onBuild)
  {
    _parent = parent;
    _onBuild = onBuild;
  }

  /// <summary>
  /// Sets the title for the rule.
  /// </summary>
  /// <param name="title">The title to display centered in the rule.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedRuleBuilder<TParent> Title(string title)
  {
    _inner.Title(title);
    return this;
  }

  /// <summary>
  /// Sets the line style for the rule.
  /// </summary>
  /// <param name="style">The line style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedRuleBuilder<TParent> Style(LineStyle style)
  {
    _inner.Style(style);
    return this;
  }

  /// <summary>
  /// Sets the color for the rule line.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedRuleBuilder<TParent> Color(string color)
  {
    _inner.Color(color);
    return this;
  }

  /// <summary>
  /// Sets a fixed width for the rule.
  /// </summary>
  /// <param name="width">The width in characters.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedRuleBuilder<TParent> Width(int width)
  {
    _inner.Width(width);
    return this;
  }

  /// <summary>
  /// Builds the rule, passes it to the parent via callback, and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    Rule rule = _inner.Build();
    _onBuild(rule);
    return _parent;
  }
}
