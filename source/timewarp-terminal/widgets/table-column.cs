namespace TimeWarp.Terminal;

/// <summary>
/// Represents a column definition for a <see cref="Table"/> widget.
/// </summary>
/// <example>
/// <code>
/// // Simple column with left alignment
/// var column = new TableColumn("Name");
///
/// // Right-aligned column for numbers
/// var column = new TableColumn("Stars", Alignment.Right);
///
/// // Column with max width and styled header
/// var column = new TableColumn("Description")
/// {
///     MaxWidth = 30,
///     HeaderColor = AnsiColors.Cyan
/// };
/// </code>
/// </example>
public sealed class TableColumn
{
  /// <summary>
  /// Initializes a new instance of the <see cref="TableColumn"/> class.
  /// </summary>
  public TableColumn()
  {
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="TableColumn"/> class with a header.
  /// </summary>
  /// <param name="header">The column header text.</param>
  public TableColumn(string header)
  {
    Header = header;
  }

  /// <summary>
  /// Initializes a new instance of the <see cref="TableColumn"/> class with a header and alignment.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <param name="alignment">The column alignment.</param>
  public TableColumn(string header, Alignment alignment)
  {
    Header = header;
    Alignment = alignment;
  }

  /// <summary>
  /// Gets or sets the column header text.
  /// Can include ANSI color codes.
  /// </summary>
  public string Header { get; set; } = string.Empty;

  /// <summary>
  /// Gets or sets the horizontal alignment for column content.
  /// Defaults to <see cref="Alignment.Left"/>.
  /// </summary>
  public Alignment Alignment { get; set; } = Alignment.Left;

  /// <summary>
  /// Gets or sets the minimum width for the column.
  /// If set, the column will not shrink below this width when fitting to terminal width.
  /// If null, defaults to 4 to allow for "..." truncation.
  /// </summary>
  public int? MinWidth { get; set; }

  /// <summary>
  /// Gets or sets the maximum width for the column.
  /// If set, content exceeding this width will be truncated.
  /// If null, the column auto-sizes to fit content.
  /// </summary>
  public int? MaxWidth { get; set; }

  /// <summary>
  /// Gets or sets the ANSI color code for the header text.
  /// If null, uses the terminal default color.
  /// </summary>
  /// <example>
  /// <code>
  /// column.HeaderColor = AnsiColors.Cyan;
  /// column.HeaderColor = AnsiColors.BrightYellow;
  /// </code>
  /// </example>
  public string? HeaderColor { get; set; }

  /// <summary>
  /// Gets or sets where to place the ellipsis when truncating content.
  /// Defaults to <see cref="TruncateMode.End"/>.
  /// </summary>
  /// <example>
  /// <code>
  /// // Show end of path (useful for file paths)
  /// column.TruncateMode = TruncateMode.Start;  // "...timewarp-nuru/feature"
  ///
  /// // Show both start and end
  /// column.TruncateMode = TruncateMode.Middle; // "/home/...feature"
  /// </code>
  /// </example>
  public TruncateMode TruncateMode { get; set; } = TruncateMode.End;
}
