namespace TimeWarp.Terminal;

/// <summary>
/// Fluent builder for creating <see cref="Table"/> instances.
/// </summary>
/// <example>
/// <code>
/// var table = new TableBuilder()
///     .AddColumn("Name")
///     .AddColumn("Stars", Alignment.Right)
///     .AddRow("CleanArchitecture", "16.5k")
///     .Border(BorderStyle.Rounded)
///     .Build();
/// </code>
/// </example>
public sealed class TableBuilder : IBuilder<Table>
{
  private readonly Table _table = new();

  /// <summary>
  /// Adds a column with the specified header.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder AddColumn(string header)
  {
    _table.AddColumn(header);
    return this;
  }

  /// <summary>
  /// Adds a column with the specified header and alignment.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <param name="alignment">The column alignment.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder AddColumn(string header, Alignment alignment)
  {
    _table.AddColumn(header, alignment);
    return this;
  }

  /// <summary>
  /// Adds a pre-configured column to the table.
  /// </summary>
  /// <param name="column">The column to add.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder AddColumn(TableColumn column)
  {
    _table.AddColumn(column);
    return this;
  }

  /// <summary>
  /// Adds multiple columns with the specified headers.
  /// </summary>
  /// <param name="headers">The column header texts.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder AddColumns(params string[] headers)
  {
    _table.AddColumns(headers);
    return this;
  }

  /// <summary>
  /// Adds a row of data to the table.
  /// </summary>
  /// <param name="cells">The cell values for the row.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder AddRow(params string[] cells)
  {
    _table.AddRow(cells);
    return this;
  }

  /// <summary>
  /// Sets the border style for the table.
  /// </summary>
  /// <param name="style">The border style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder Border(BorderStyle style)
  {
    _table.Border = style;
    return this;
  }

  /// <summary>
  /// Sets the border color for the table.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder BorderColor(string color)
  {
    _table.BorderColor = color;
    return this;
  }

  /// <summary>
  /// Hides the header row.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder HideHeaders()
  {
    _table.ShowHeaders = false;
    return this;
  }

  /// <summary>
  /// Shows separator lines between data rows.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder ShowRowSeparators()
  {
    _table.ShowRowSeparators = true;
    return this;
  }

  /// <summary>
  /// Expands the table to fill the terminal width.
  /// </summary>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder Expand()
  {
    _table.Expand = true;
    return this;
  }

  /// <summary>
  /// Sets whether the table should shrink columns to fit the terminal width.
  /// When true, columns are proportionally reduced if the table would exceed terminal width.
  /// Defaults to <c>true</c>.
  /// </summary>
  /// <param name="value">True to enable shrinking, false to allow overflow.</param>
  /// <returns>This builder for method chaining.</returns>
  public TableBuilder Shrink(bool value = true)
  {
    _table.Shrink = value;
    return this;
  }

  /// <summary>
  /// Builds the configured <see cref="Table"/> instance.
  /// </summary>
  /// <returns>The configured table.</returns>
  public Table Build() => _table;

  /// <summary>
  /// Converts the builder to a <see cref="Table"/>.
  /// Alternate method for languages that don't support implicit operators.
  /// </summary>
  /// <returns>The configured table.</returns>
  public Table ToTable() => Build();
}
