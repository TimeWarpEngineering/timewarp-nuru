namespace TimeWarp.Terminal;

/// <summary>
/// Represents a bordered box (panel) for terminal output.
/// Can optionally include a header in the top border.
/// </summary>
/// <example>
/// <code>
/// // Simple panel
/// var panel = new Panel { Content = "Hello World" };
///
/// // Panel with header
/// var panel = new Panel
/// {
///     Header = "Notice",
///     Content = "Important information here",
///     Border = BorderStyle.Rounded
/// };
///
/// // Panel with styling and padding
/// var panel = new Panel
/// {
///     Header = "Results".Cyan().Bold(),
///     Content = "Line 1\nLine 2",
///     Border = BorderStyle.Double,
///     PaddingHorizontal = 2,
///     PaddingVertical = 1
/// };
/// </code>
/// </example>
public sealed class Panel
{
  /// <summary>
  /// Gets or sets the optional header to display in the top border.
  /// Can include ANSI color codes.
  /// </summary>
  public string? Header { get; set; }

  /// <summary>
  /// Gets or sets the content to display inside the panel.
  /// Can be multi-line (use \n) and can include ANSI color codes.
  /// </summary>
  public string? Content { get; set; }

  /// <summary>
  /// Gets or sets the border style for the panel.
  /// Defaults to <see cref="BorderStyle.Rounded"/>.
  /// </summary>
  public BorderStyle Border { get; set; } = BorderStyle.Rounded;

  /// <summary>
  /// Gets or sets the ANSI color code for the border.
  /// Defaults to no color (uses terminal default).
  /// </summary>
  public string? BorderColor { get; set; }

  /// <summary>
  /// Gets or sets the horizontal padding (left and right) inside the panel.
  /// Defaults to 1.
  /// </summary>
  public int PaddingHorizontal { get; set; } = 1;

  /// <summary>
  /// Gets or sets the vertical padding (top and bottom) inside the panel.
  /// Defaults to 0.
  /// </summary>
  public int PaddingVertical { get; set; }

  /// <summary>
  /// Gets or sets the fixed width of the panel.
  /// If null, uses the terminal width.
  /// </summary>
  public int? Width { get; set; }

  /// <summary>
  /// Gets or sets whether to wrap long text at word boundaries to fit within the panel width.
  /// Defaults to true.
  /// </summary>
  public bool WordWrap { get; set; } = true;

  /// <summary>
  /// Renders the panel to an array of strings (one per line).
  /// </summary>
  /// <param name="terminalWidth">The terminal width to use if <see cref="Width"/> is not set.</param>
  /// <returns>The rendered panel lines.</returns>
  public string[] Render(int terminalWidth = 80)
  {
    if (Border == BorderStyle.None)
    {
      return RenderWithoutBorder();
    }

    return RenderWithBorder(terminalWidth);
  }

  private string[] RenderWithoutBorder()
  {
    if (string.IsNullOrEmpty(Content))
      return [];

    return Content.Split('\n');
  }

  private string[] RenderWithBorder(int terminalWidth)
  {
    int width = Width ?? terminalWidth;

    // Ensure minimum width (corners + at least 1 character content area)
    width = Math.Max(width, 4);

    char topLeft = BoxChars.GetTopLeft(Border);
    char topRight = BoxChars.GetTopRight(Border);
    char bottomLeft = BoxChars.GetBottomLeft(Border);
    char bottomRight = BoxChars.GetBottomRight(Border);
    char horizontal = BoxChars.GetHorizontal(Border);
    char vertical = BoxChars.GetVertical(Border);

    // Content area width = total width - 2 borders - 2×horizontal padding
    int contentAreaWidth = width - 2 - (2 * PaddingHorizontal);
    if (contentAreaWidth < 1) contentAreaWidth = 1;

    // Split content into lines and optionally wrap
    List<string> contentLines = [];
    if (!string.IsNullOrEmpty(Content))
    {
      string[] rawLines = Content.Split('\n');
      foreach (string rawLine in rawLines)
      {
        if (WordWrap)
        {
          // Wrap each line that exceeds the content area width
          contentLines.AddRange(AnsiStringUtils.WrapText(rawLine, contentAreaWidth));
        }
        else
        {
          contentLines.Add(rawLine);
        }
      }
    }

    List<string> result = [];

    // Render top border with optional header
    result.Add(RenderTopBorder(width, topLeft, topRight, horizontal));

    // Render vertical padding rows
    for (int i = 0; i < PaddingVertical; i++)
    {
      result.Add(RenderEmptyContentRow(vertical, contentAreaWidth));
    }

    // Render content rows
    foreach (string line in contentLines)
    {
      result.Add(RenderContentRow(line, vertical, contentAreaWidth));
    }

    // Handle empty content
    if (contentLines.Count == 0)
    {
      result.Add(RenderEmptyContentRow(vertical, contentAreaWidth));
    }

    // Render vertical padding rows
    for (int i = 0; i < PaddingVertical; i++)
    {
      result.Add(RenderEmptyContentRow(vertical, contentAreaWidth));
    }

    // Render bottom border
    result.Add(RenderBottomBorder(width, bottomLeft, bottomRight, horizontal));

    return [.. result];
  }

  private string RenderTopBorder(int width, char topLeft, char topRight, char horizontal)
  {
    string colorStart = !string.IsNullOrEmpty(BorderColor) ? BorderColor : "";
    string colorEnd = !string.IsNullOrEmpty(BorderColor) ? AnsiColors.Reset : "";

    if (string.IsNullOrEmpty(Header))
    {
      // Simple top border without header
      string line = new(horizontal, width - 2);
      return $"{colorStart}{topLeft}{line}{topRight}{colorEnd}";
    }

    // Top border with header
    int headerVisibleLength = AnsiStringUtils.GetVisibleLength(Header);

    // Format: ╭─ Header ─────────────╮
    // Need: corner + dash + space + header + space + dashes + corner
    int minimumWidth = headerVisibleLength + 6; // 2 corners + 2 dashes + 2 spaces

    if (width < minimumWidth)
    {
      // Not enough space, just render simple border
      string line = new(horizontal, width - 2);
      return $"{colorStart}{topLeft}{line}{topRight}{colorEnd}";
    }

    // Calculate remaining space for dashes after header
    int remainingForDashes = width - 2 - 4 - headerVisibleLength; // -2 corners, -4 for "─ " and " ─"
    string rightDashes = new(horizontal, remainingForDashes);

    return $"{colorStart}{topLeft}{horizontal}{colorEnd} {Header} {colorStart}{rightDashes}{horizontal}{topRight}{colorEnd}";
  }

  private string RenderBottomBorder(int width, char bottomLeft, char bottomRight, char horizontal)
  {
    string colorStart = !string.IsNullOrEmpty(BorderColor) ? BorderColor : "";
    string colorEnd = !string.IsNullOrEmpty(BorderColor) ? AnsiColors.Reset : "";

    string line = new(horizontal, width - 2);
    return $"{colorStart}{bottomLeft}{line}{bottomRight}{colorEnd}";
  }

  private string RenderContentRow(string content, char vertical, int contentAreaWidth)
  {
    string colorStart = !string.IsNullOrEmpty(BorderColor) ? BorderColor : "";
    string colorEnd = !string.IsNullOrEmpty(BorderColor) ? AnsiColors.Reset : "";

    string padding = new(' ', PaddingHorizontal);

    // Pad or truncate content to fit content area
    string paddedContent = AnsiStringUtils.PadRightVisible(content, contentAreaWidth);

    return $"{colorStart}{vertical}{colorEnd}{padding}{paddedContent}{padding}{colorStart}{vertical}{colorEnd}";
  }

  private string RenderEmptyContentRow(char vertical, int contentAreaWidth)
  {
    return RenderContentRow("", vertical, contentAreaWidth);
  }
}

/// <summary>
/// Fluent builder for creating <see cref="Panel"/> instances.
/// </summary>
/// <example>
/// <code>
/// var panel = new PanelBuilder()
///     .Header("Notice")
///     .Content("Important information")
///     .Border(BorderStyle.Rounded)
///     .Padding(2, 1)
///     .Build();
/// </code>
/// </example>
public sealed class PanelBuilder : IBuilder<Panel>
{
  private readonly Panel _panel = new();

  /// <summary>
  /// Sets the header for the panel.
  /// </summary>
  /// <param name="header">The header to display in the top border.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder Header(string header)
  {
    _panel.Header = header;
    return this;
  }

  /// <summary>
  /// Sets the content for the panel.
  /// </summary>
  /// <param name="content">The content to display inside the panel.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder Content(string content)
  {
    _panel.Content = content;
    return this;
  }

  /// <summary>
  /// Sets the border style for the panel.
  /// </summary>
  /// <param name="style">The border style to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder Border(BorderStyle style)
  {
    _panel.Border = style;
    return this;
  }

  /// <summary>
  /// Sets the border color for the panel.
  /// </summary>
  /// <param name="color">The ANSI color code to use.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder BorderColor(string color)
  {
    _panel.BorderColor = color;
    return this;
  }

  /// <summary>
  /// Sets both horizontal and vertical padding for the panel.
  /// </summary>
  /// <param name="horizontal">The horizontal padding (left and right).</param>
  /// <param name="vertical">The vertical padding (top and bottom).</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder Padding(int horizontal, int vertical)
  {
    _panel.PaddingHorizontal = horizontal;
    _panel.PaddingVertical = vertical;
    return this;
  }

  /// <summary>
  /// Sets the horizontal padding for the panel.
  /// </summary>
  /// <param name="padding">The horizontal padding (left and right).</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder PaddingHorizontal(int padding)
  {
    _panel.PaddingHorizontal = padding;
    return this;
  }

  /// <summary>
  /// Sets the vertical padding for the panel.
  /// </summary>
  /// <param name="padding">The vertical padding (top and bottom).</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder PaddingVertical(int padding)
  {
    _panel.PaddingVertical = padding;
    return this;
  }

  /// <summary>
  /// Sets a fixed width for the panel.
  /// </summary>
  /// <param name="width">The width in characters.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder Width(int width)
  {
    _panel.Width = width;
    return this;
  }

  /// <summary>
  /// Sets whether to wrap long text at word boundaries.
  /// </summary>
  /// <param name="wrap">True to enable word wrapping, false to disable.</param>
  /// <returns>This builder for method chaining.</returns>
  public PanelBuilder WordWrap(bool wrap)
  {
    _panel.WordWrap = wrap;
    return this;
  }

  /// <summary>
  /// Builds the configured <see cref="Panel"/> instance.
  /// </summary>
  /// <returns>The configured panel.</returns>
  public Panel Build() => _panel;

  /// <summary>
  /// Converts the builder to a <see cref="Panel"/>.
  /// Alternate method for languages that don't support implicit operators.
  /// </summary>
  /// <returns>The configured panel.</returns>
  public Panel ToPanel() => Build();
}
