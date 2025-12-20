namespace TimeWarp.Terminal;

/// <summary>
/// Nested fluent builder for constructing <see cref="Table"/> instances within a fluent chain.
/// </summary>
/// <typeparam name="TParent">The parent builder type to return to.</typeparam>
/// <remarks>
/// <para>
/// This builder wraps <see cref="TableBuilder"/> and implements <see cref="INestedBuilder{TParent}"/>
/// to enable fluent API patterns where table configuration returns to the parent context.
/// </para>
/// <para>
/// For standalone table building, use <see cref="TableBuilder"/> directly.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Nested usage in fluent chain
/// terminal.Table(t => t
///     .AddColumn("Name")
///     .AddColumn("Value")
///     .AddRow("Status", "OK")
///     .Border(BorderStyle.Rounded)
///     .Done());                    // Builds table, renders, returns terminal
/// </code>
/// </example>
public sealed class NestedTableBuilder<TParent> : INestedBuilder<TParent>
  where TParent : class
{
  private readonly TableBuilder _inner = new();
  private readonly TParent _parent;
  private readonly Action<Table> _onBuild;

  /// <summary>
  /// Initializes a new instance of the <see cref="NestedTableBuilder{TParent}"/> class.
  /// </summary>
  /// <param name="parent">The parent builder to return to when <see cref="Done"/> is called.</param>
  /// <param name="onBuild">Callback invoked with the built table when <see cref="Done"/> is called.</param>
  internal NestedTableBuilder(TParent parent, Action<Table> onBuild)
  {
    _parent = parent;
    _onBuild = onBuild;
  }

  /// <summary>
  /// Adds a column with the specified header.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> AddColumn(string header)
  {
    _inner.AddColumn(header);
    return this;
  }

  /// <summary>
  /// Adds a column with the specified header and alignment.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <param name="alignment">The column alignment.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> AddColumn(string header, Alignment alignment)
  {
    _inner.AddColumn(header, alignment);
    return this;
  }

  /// <summary>
  /// Adds a pre-configured column to the table.
  /// </summary>
  /// <param name="column">The column to add.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> AddColumn(TableColumn column)
  {
    _inner.AddColumn(column);
    return this;
  }

  /// <summary>
  /// Adds multiple columns with the specified headers.
  /// </summary>
  /// <param name="headers">The column header texts.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> AddColumns(params string[] headers)
  {
    _inner.AddColumns(headers);
    return this;
  }

  /// <summary>
  /// Adds a row of data to the table.
  /// </summary>
  /// <param name="cells">The cell values for the row.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> AddRow(params string[] cells)
  {
    _inner.AddRow(cells);
    return this;
  }

  /// <summary>
  /// Sets the border style for the table.
  /// </summary>
  /// <param name="style">The border style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> Border(BorderStyle style)
  {
    _inner.Border(style);
    return this;
  }

  /// <summary>
  /// Sets the border color for the table.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> BorderColor(string color)
  {
    _inner.BorderColor(color);
    return this;
  }

  /// <summary>
  /// Hides the header row.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> HideHeaders()
  {
    _inner.HideHeaders();
    return this;
  }

  /// <summary>
  /// Shows separator lines between data rows.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> ShowRowSeparators()
  {
    _inner.ShowRowSeparators();
    return this;
  }

  /// <summary>
  /// Expands the table to fill the terminal width.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public NestedTableBuilder<TParent> Expand()
  {
    _inner.Expand();
    return this;
  }

  /// <summary>
  /// Builds the table, passes it to the parent via callback, and returns to the parent builder.
  /// </summary>
  /// <returns>The parent builder for continued chaining.</returns>
  public TParent Done()
  {
    Table table = _inner.Build();
    _onBuild(table);
    return _parent;
  }
}
