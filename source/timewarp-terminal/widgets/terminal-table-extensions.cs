namespace TimeWarp.Terminal;

/// <summary>
/// Extension methods for writing <see cref="Table"/> widgets to an <see cref="ITerminal"/>.
/// </summary>
/// <example>
/// <code>
/// // Simple table with columns and rows
/// terminal.WriteTable(table => table
///     .AddColumn("Name")
///     .AddColumn("Stars", Alignment.Right)
///     .AddRow("CleanArchitecture", "16.5k")
///     .AddRow("GuardClauses", "3.2k"));
///
/// // Pre-built table
/// terminal.WriteTable(myTable);
/// </code>
/// </example>
public static class TerminalTableExtensions
{
  /// <summary>
  /// Writes a table configured via a builder action to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="configure">An action to configure the table using a <see cref="TableBuilder"/>.</param>
  /// <example>
  /// <code>
  /// terminal.WriteTable(table => table
  ///     .AddColumns("Package", "Downloads", "Version")
  ///     .AddRow("Ardalis.GuardClauses", "12M", "5.0.0")
  ///     .AddRow("Ardalis.Result", "8M", "10.0.0")
  ///     .Border(BorderStyle.Rounded));
  /// </code>
  /// </example>
  public static void WriteTable(this ITerminal terminal, Action<TableBuilder> configure)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(configure);

    TableBuilder builder = new();
    configure(builder);

    Table table = builder.Build();
    WriteTableInternal(terminal, table);
  }

  /// <summary>
  /// Writes a pre-configured <see cref="Table"/> to the terminal.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="table">The table to write.</param>
  public static void WriteTable(this ITerminal terminal, Table table)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(table);

    WriteTableInternal(terminal, table);
  }

  private static void WriteTableInternal(ITerminal terminal, Table table)
  {
    string[] lines = table.Render(terminal.WindowWidth);
    foreach (string line in lines)
    {
      terminal.WriteLine(line);
    }
  }
}
