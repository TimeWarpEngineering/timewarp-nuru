namespace TimeWarp.Terminal;

/// <summary>
/// Extension methods for writing <see cref="Panel"/> widgets to an <see cref="ITerminal"/>.
/// </summary>
/// <example>
/// <code>
/// // Simple panel with content
/// terminal.WritePanel("This is important information");
///
/// // Panel with header
/// terminal.WritePanel("Content here", header: "Notice");
///
/// // Fluent builder
/// terminal.WritePanel(panel => panel
///     .Header("Configuration")
///     .Content("Setting: value")
///     .Border(BorderStyle.Rounded)
///     .Padding(2, 1));
/// </code>
/// </example>
public static class TerminalPanelExtensions
{
  /// <summary>
  /// Writes a simple panel with content to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="content">The content to display inside the panel.</param>
  /// <param name="border">The border style to use. Defaults to <see cref="BorderStyle.Rounded"/>.</param>
  public static void WritePanel(this ITerminal terminal, string content, BorderStyle border = BorderStyle.Rounded)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    Panel panel = new() { Content = content, Border = border };
    WritePanelInternal(terminal, panel);
  }

  /// <summary>
  /// Writes a panel with a header and content to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="content">The content to display inside the panel.</param>
  /// <param name="header">The header to display in the top border.</param>
  /// <param name="border">The border style to use. Defaults to <see cref="BorderStyle.Rounded"/>.</param>
  public static void WritePanel(this ITerminal terminal, string content, string header, BorderStyle border = BorderStyle.Rounded)
  {
    ArgumentNullException.ThrowIfNull(terminal);

    Panel panel = new() { Content = content, Header = header, Border = border };
    WritePanelInternal(terminal, panel);
  }

  /// <summary>
  /// Writes a panel configured via a builder action to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="configure">An action to configure the panel using a <see cref="PanelBuilder"/>.</param>
  /// <example>
  /// <code>
  /// terminal.WritePanel(panel => panel
  ///     .Header("Configuration")
  ///     .Content("Setting: value")
  ///     .Border(BorderStyle.Rounded)
  ///     .Padding(2, 1));
  /// </code>
  /// </example>
  public static void WritePanel(this ITerminal terminal, Action<PanelBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(configure);

    PanelBuilder builder = new();
    configure(builder);

    Panel panel = builder.Build();
    WritePanelInternal(terminal, panel);
  }

  /// <summary>
  /// Writes a pre-configured <see cref="Panel"/> to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="panel">The panel to write.</param>
  public static void WritePanel(this ITerminal terminal, Panel panel)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(panel);

    WritePanelInternal(terminal, panel);
  }

  private static void WritePanelInternal(ITerminal terminal, Panel panel)
  {
    string[] lines = panel.Render(terminal.WindowWidth);
    foreach (string line in lines)
    {
      terminal.WriteLine(line);
    }
  }
}
