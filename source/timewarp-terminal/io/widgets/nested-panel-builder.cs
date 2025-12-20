namespace TimeWarp.Terminal;

/// <summary>
/// Nested fluent builder for constructing <see cref="Panel"/> instances within a fluent chain.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This builder wraps <see cref="PanelBuilder"/> and implements <see cref="INestedBuilder{TParent}"/>
/// to enable fluent API patterns where panel configuration returns to the parent context.
/// </para>
/// <para>
/// For standalone panel building, use <see cref="PanelBuilder"/> directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Nested usage in fluent chain
/// terminal.Panel(p => p
///     .Header("Notice")
///     .Content("Important information")
///     .Border(BorderStyle.Rounded)
///     .Done());                    // Builds panel, renders, returns terminal
/// </code>
/// </example>
public sealed class NestedPanelBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly PanelBuilder _inner = new();
  private readonly TParent _parent;
  private readonly Action<Panel> _onBuild;

  /// <summary>
  /// Initializes a new instance of the <see cref="NestedPanelBuilder{TParent}"/> class.
  /// </summary>
  /// <param name="parent">The parent builder to return to when <see cref="Done"/> is called.</param>
  /// <param name="onBuild">Callback invoked with the built panel when <see cref="Done"/> is called.</param>
  internal NestedPanelBuilder(TParent parent, Action<Panel> onBuild)
  {
    _parent = parent;
    _onBuild = onBuild;
  }

  /// <summary>
  /// Sets the header for the panel.
  /// </summary>
  /// <param name="header">The header to display in the top border.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> Header(string header)
  {
    _inner.Header(header);
    return this;
  }

  /// <summary>
  /// Sets the content for the panel.
  /// </summary>
  /// <param name="content">The content to display inside the panel.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> Content(string content)
  {
    _inner.Content(content);
    return this;
  }

  /// <summary>
  /// Sets the border style for the panel.
  /// </summary>
  /// <param name="style">The border style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> Border(BorderStyle style)
  {
    _inner.Border(style);
    return this;
  }

  /// <summary>
  /// Sets the border color for the panel.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> BorderColor(string color)
  {
    _inner.BorderColor(color);
    return this;
  }

  /// <summary>
  /// Sets both horizontal and vertical padding for the panel.
  /// </summary>
  /// <param name="horizontal">The horizontal padding (left and right).</param>
  /// <param name="vertical">The vertical padding (top and bottom).</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> Padding(int horizontal, int vertical)
  {
    _inner.Padding(horizontal, vertical);
    return this;
  }

  /// <summary>
  /// Sets the horizontal padding for the panel.
  /// </summary>
  /// <param name="padding">The horizontal padding (left and right).</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> PaddingHorizontal(int padding)
  {
    _inner.PaddingHorizontal(padding);
    return this;
  }

  /// <summary>
  /// Sets the vertical padding for the panel.
  /// </summary>
  /// <param name="padding">The vertical padding (top and bottom).</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> PaddingVertical(int padding)
  {
    _inner.PaddingVertical(padding);
    return this;
  }

  /// <summary>
  /// Sets a fixed width for the panel.
  /// </summary>
  /// <param name="width">The width in characters.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> Width(int width)
  {
    _inner.Width(width);
    return this;
  }

  /// <summary>
  /// Sets whether to wrap long text at word boundaries.
  /// </summary>
  /// <param name="wrap">True to enable word wrapping, false to disable.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedPanelBuilder<TParent> WordWrap(bool wrap)
  {
    _inner.WordWrap(wrap);
    return this;
  }

  /// <summary>
  /// Builds the panel, passes it to the parent via callback, and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    Panel panel = _inner.Build();
    _onBuild(panel);
    return _parent;
  }
}
