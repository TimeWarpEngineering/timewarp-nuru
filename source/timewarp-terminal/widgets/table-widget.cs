namespace TimeWarp.Terminal;

/// <summary>
/// Represents a table widget for rendering formatted columnar data with headers, alignment, and borders.
/// </summary>
/// <example>
/// <code>
/// // Simple table
/// var table = new Table()
///     .AddColumn("Name")
///     .AddColumn("Stars", Alignment.Right)
///     .AddColumn("Description")
///     .AddRow("CleanArchitecture", "16.5k", "Clean Architecture template")
///     .AddRow("GuardClauses", "3.2k", "Guard clause library");
///
/// terminal.WriteTable(table);
/// </code>
/// </example>
public sealed class Table
{
  private readonly List<TableColumn> _columns = [];
  private readonly List<string[]> _rows = [];

  /// <summary>
  /// Gets the columns defined for this table.
  /// </summary>
  public IReadOnlyList<TableColumn> Columns => _columns;

  /// <summary>
  /// Gets the data rows in this table.
  /// </summary>
  public IReadOnlyList<string[]> Rows => _rows;

  /// <summary>
  /// Gets or sets the border style for the table.
  /// Defaults to <see cref="BorderStyle.Square"/>.
  /// </summary>
  public BorderStyle Border { get; set; } = BorderStyle.Square;

  /// <summary>
  /// Gets or sets a value indicating whether to display the header row.
  /// Defaults to <c>true</c>.
  /// </summary>
  public bool ShowHeaders { get; set; } = true;

  /// <summary>
  /// Gets or sets a value indicating whether to display separator lines between data rows.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool ShowRowSeparators { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the table should expand to fill the terminal width.
  /// Defaults to <c>false</c>.
  /// </summary>
  public bool Expand { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether the table should shrink columns to fit the terminal width.
  /// When true, columns are proportionally reduced if the table would exceed terminal width.
  /// Wider columns shrink more aggressively than narrower ones.
  /// Defaults to <c>true</c>.
  /// </summary>
  public bool Shrink { get; set; } = true;

  /// <summary>
  /// Gets or sets the ANSI color code for the border.
  /// If null, uses the terminal default color.
  /// </summary>
  public string? BorderColor { get; set; }

  /// <summary>
  /// Adds a column with the specified header and default left alignment.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <returns>This table for method chaining.</returns>
  public Table AddColumn(string header)
  {
    _columns.Add(new TableColumn(header));
    return this;
  }

  /// <summary>
  /// Adds a column with the specified header and alignment.
  /// </summary>
  /// <param name="header">The column header text.</param>
  /// <param name="alignment">The column alignment.</param>
  /// <returns>This table for method chaining.</returns>
  public Table AddColumn(string header, Alignment alignment)
  {
    _columns.Add(new TableColumn(header, alignment));
    return this;
  }

  /// <summary>
  /// Adds a pre-configured column to the table.
  /// </summary>
  /// <param name="column">The column to add.</param>
  /// <returns>This table for method chaining.</returns>
  public Table AddColumn(TableColumn column)
  {
    ArgumentNullException.ThrowIfNull(column);
    _columns.Add(column);
    return this;
  }

  /// <summary>
  /// Adds multiple columns with the specified headers.
  /// </summary>
  /// <param name="headers">The column header texts.</param>
  /// <returns>This table for method chaining.</returns>
  public Table AddColumns(params string[] headers)
  {
    ArgumentNullException.ThrowIfNull(headers);

    foreach (string header in headers)
    {
      _columns.Add(new TableColumn(header));
    }

    return this;
  }

  /// <summary>
  /// Adds a row of data to the table.
  /// </summary>
  /// <param name="cells">The cell values for the row. Can include ANSI color codes.</param>
  /// <returns>This table for method chaining.</returns>
  /// <remarks>
  /// If fewer cells are provided than columns, remaining cells will be empty.
  /// If more cells are provided than columns, extra cells are ignored.
  /// </remarks>
  public Table AddRow(params string[] cells)
  {
    _rows.Add(cells);
    return this;
  }

  /// <summary>
  /// Renders the table to an array of strings (one per line).
  /// </summary>
  /// <param name="terminalWidth">The terminal width to use for expandable tables.</param>
  /// <returns>The rendered table lines.</returns>
  public string[] Render(int terminalWidth = 80)
  {
    if (_columns.Count == 0)
      return [];

    int[] columnWidths = CalculateColumnWidths(terminalWidth);

    if (Border == BorderStyle.None)
    {
      return RenderWithoutBorder(columnWidths);
    }

    return RenderWithBorder(columnWidths);
  }

  private int[] CalculateColumnWidths(int terminalWidth)
  {
    int[] widths = new int[_columns.Count];

    // Calculate natural width for each column (max of header and all cell values)
    for (int i = 0; i < _columns.Count; i++)
    {
      TableColumn column = _columns[i];

      // Start with header width
      int maxWidth = AnsiStringUtils.GetVisibleLength(column.Header);

      // Check all cell values in this column
      foreach (string[] row in _rows)
      {
        if (i < row.Length)
        {
          int cellWidth = AnsiStringUtils.GetVisibleLength(row[i]);
          maxWidth = Math.Max(maxWidth, cellWidth);
        }
      }

      // Apply max width constraint if set
      if (column.MaxWidth.HasValue)
      {
        maxWidth = Math.Min(maxWidth, column.MaxWidth.Value);
      }

      widths[i] = maxWidth;
    }

    // Calculate overhead: borders + padding + separators
    // │ cell │ cell │ = 2 outer borders + (n-1) inner separators + 2*n padding spaces
    int overhead = Border != BorderStyle.None
      ? 2 + (_columns.Count - 1) + (2 * _columns.Count)
      : (_columns.Count - 1) * 2; // Borderless: just column separators

    int contentWidth = widths.Sum();
    int totalWidth = overhead + contentWidth;

    // If expandable, distribute remaining width
    if (Expand && Border != BorderStyle.None && totalWidth < terminalWidth)
    {
      int extraWidth = terminalWidth - totalWidth;
      int perColumn = extraWidth / _columns.Count;
      int remainder = extraWidth % _columns.Count;

      for (int i = 0; i < _columns.Count; i++)
      {
        widths[i] += perColumn;
        if (i < remainder)
        {
          widths[i]++;
        }
      }
    }
    // If Shrink is enabled and table exceeds terminal width, shrink columns
    else if (Shrink && totalWidth > terminalWidth)
    {
      int excessWidth = totalWidth - terminalWidth;
      int availableContentWidth = terminalWidth - overhead;

      if (availableContentWidth > 0)
      {
        // Get minimum widths for each column (default 4 for ellipsis)
        int[] minWidths = new int[_columns.Count];
        for (int i = 0; i < _columns.Count; i++)
        {
          minWidths[i] = _columns[i].MinWidth ?? 4;
        }

        // Calculate how much each column can shrink (width above minimum)
        int[] shrinkableAmounts = new int[_columns.Count];
        int totalShrinkable = 0;
        for (int i = 0; i < _columns.Count; i++)
        {
          shrinkableAmounts[i] = Math.Max(0, widths[i] - minWidths[i]);
          totalShrinkable += shrinkableAmounts[i];
        }

        if (totalShrinkable > 0)
        {
          // Shrink proportionally based on shrinkable amount (wider columns shrink more)
          int remainingExcess = Math.Min(excessWidth, totalShrinkable);

          for (int i = 0; i < _columns.Count; i++)
          {
            if (shrinkableAmounts[i] > 0)
            {
              // Calculate this column's share of the shrinkage
              int shrinkAmount = (int)Math.Ceiling((double)shrinkableAmounts[i] / totalShrinkable * remainingExcess);
              shrinkAmount = Math.Min(shrinkAmount, shrinkableAmounts[i]); // Don't shrink below min
              widths[i] -= shrinkAmount;

              // Update totals for remaining columns
              totalShrinkable -= shrinkableAmounts[i];
              remainingExcess -= shrinkAmount;
            }
          }
        }
      }
    }

    return widths;
  }

  private string[] RenderWithoutBorder(int[] columnWidths)
  {
    List<string> lines = [];

    // Render header row if enabled
    if (ShowHeaders)
    {
      lines.Add(RenderDataRow([.. _columns.Select(c => c.Header)], columnWidths, isHeader: true));
    }

    // Render data rows
    foreach (string[] row in _rows)
    {
      lines.Add(RenderDataRow(row, columnWidths, isHeader: false));
    }

    return [.. lines];
  }

  private string[] RenderWithBorder(int[] columnWidths)
  {
    List<string> lines = [];

    char horizontal = BoxChars.GetHorizontal(Border);
    char vertical = BoxChars.GetVertical(Border);
    char topLeft = BoxChars.GetTopLeft(Border);
    char topRight = BoxChars.GetTopRight(Border);
    char bottomLeft = BoxChars.GetBottomLeft(Border);
    char bottomRight = BoxChars.GetBottomRight(Border);
    char topT = BoxChars.GetTopT(Border);
    char bottomT = BoxChars.GetBottomT(Border);
    char leftT = BoxChars.GetLeftT(Border);
    char rightT = BoxChars.GetRightT(Border);
    char cross = BoxChars.GetCross(Border);

    // Top border
    lines.Add(RenderHorizontalBorder(columnWidths, horizontal, topLeft, topRight, topT));

    // Header row if enabled
    if (ShowHeaders)
    {
      lines.Add(RenderCellRow([.. _columns.Select(c => c.Header)], columnWidths, vertical, isHeader: true));

      // Header separator
      lines.Add(RenderHorizontalBorder(columnWidths, horizontal, leftT, rightT, cross));
    }

    // Data rows
    for (int i = 0; i < _rows.Count; i++)
    {
      lines.Add(RenderCellRow(_rows[i], columnWidths, vertical, isHeader: false));

      // Row separator (except after last row)
      if (ShowRowSeparators && i < _rows.Count - 1)
      {
        lines.Add(RenderHorizontalBorder(columnWidths, horizontal, leftT, rightT, cross));
      }
    }

    // Bottom border
    lines.Add(RenderHorizontalBorder(columnWidths, horizontal, bottomLeft, bottomRight, bottomT));

    return [.. lines];
  }

  private string RenderHorizontalBorder(int[] columnWidths, char horizontal, char left, char right, char junction)
  {
    string colorStart = !string.IsNullOrEmpty(BorderColor) ? BorderColor : "";
    string colorEnd = !string.IsNullOrEmpty(BorderColor) ? AnsiColors.Reset : "";

    StringBuilder sb = new();
    sb.Append(colorStart);
    sb.Append(left);

    for (int i = 0; i < columnWidths.Length; i++)
    {
      // Each cell has padding (1 space on each side) plus content width
      sb.Append(horizontal, columnWidths[i] + 2);

      if (i < columnWidths.Length - 1)
      {
        sb.Append(junction);
      }
    }

    sb.Append(right);
    sb.Append(colorEnd);

    return sb.ToString();
  }

  private string RenderCellRow(string[] cells, int[] columnWidths, char vertical, bool isHeader)
  {
    string colorStart = !string.IsNullOrEmpty(BorderColor) ? BorderColor : "";
    string colorEnd = !string.IsNullOrEmpty(BorderColor) ? AnsiColors.Reset : "";

    StringBuilder sb = new();
    sb.Append(colorStart);
    sb.Append(vertical);
    sb.Append(colorEnd);

    for (int i = 0; i < columnWidths.Length; i++)
    {
      string cellValue = i < cells.Length ? cells[i] ?? "" : "";
      TableColumn column = _columns[i];

      // Apply header color if this is a header and the column has a header color
      if (isHeader && !string.IsNullOrEmpty(column.HeaderColor))
      {
        cellValue = column.HeaderColor + cellValue + AnsiColors.Reset;
      }

      // Truncate if necessary (preserving ANSI codes as much as possible)
      int visibleLength = AnsiStringUtils.GetVisibleLength(cellValue);
      if (visibleLength > columnWidths[i])
      {
        cellValue = TruncateWithEllipsis(cellValue, columnWidths[i], column.TruncateMode);
      }

      // Apply alignment
      string alignedCell = AlignCell(cellValue, columnWidths[i], column.Alignment);

      sb.Append(' '); // Left padding
      sb.Append(alignedCell);
      sb.Append(' '); // Right padding
      sb.Append(colorStart);
      sb.Append(vertical);
      sb.Append(colorEnd);
    }

    return sb.ToString();
  }

  private string RenderDataRow(string[] cells, int[] columnWidths, bool isHeader)
  {
    StringBuilder sb = new();

    for (int i = 0; i < columnWidths.Length; i++)
    {
      string cellValue = i < cells.Length ? cells[i] ?? "" : "";
      TableColumn column = _columns[i];

      // Apply header color if this is a header and the column has a header color
      if (isHeader && !string.IsNullOrEmpty(column.HeaderColor))
      {
        cellValue = column.HeaderColor + cellValue + AnsiColors.Reset;
      }

      // Truncate if necessary
      int visibleLength = AnsiStringUtils.GetVisibleLength(cellValue);
      if (visibleLength > columnWidths[i])
      {
        cellValue = TruncateWithEllipsis(cellValue, columnWidths[i], column.TruncateMode);
      }

      // Apply alignment
      string alignedCell = AlignCell(cellValue, columnWidths[i], column.Alignment);

      if (i > 0)
      {
        sb.Append("  "); // Column separator for borderless tables
      }

      sb.Append(alignedCell);
    }

    return sb.ToString();
  }

  private static string AlignCell(string content, int width, Alignment alignment)
  {
    return alignment switch
    {
      Alignment.Left => AnsiStringUtils.PadRightVisible(content, width),
      Alignment.Right => AnsiStringUtils.PadLeftVisible(content, width),
      Alignment.Center => AnsiStringUtils.CenterVisible(content, width),
      _ => AnsiStringUtils.PadRightVisible(content, width)
    };
  }

  private static string TruncateWithEllipsis(string text, int maxWidth, TruncateMode mode)
  {
    if (maxWidth <= 3)
    {
      return new string('.', maxWidth);
    }

    // Strip ANSI codes to get plain text
    string plainText = AnsiStringUtils.StripAnsiCodes(text);

    if (plainText.Length <= maxWidth)
    {
      return text; // No truncation needed
    }

    // Simple truncation (doesn't preserve ANSI codes perfectly, but works)
    return mode switch
    {
      TruncateMode.Start => "..." + plainText[^(maxWidth - 3)..],
      TruncateMode.Middle => TruncateMiddle(plainText, maxWidth),
      _ => plainText[..(maxWidth - 3)] + "..." // TruncateMode.End (default)
    };
  }

  private static string TruncateMiddle(string text, int maxWidth)
  {
    // For middle truncation, show roughly equal parts from start and end
    // "..." takes 3 chars, so we have (maxWidth - 3) chars to split
    int availableChars = maxWidth - 3;
    int startChars = (availableChars + 1) / 2; // Slightly favor start if odd
    int endChars = availableChars - startChars;

    return text[..startChars] + "..." + text[^endChars..];
  }
}
