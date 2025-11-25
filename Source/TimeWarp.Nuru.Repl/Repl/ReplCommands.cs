namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Implements built-in REPL commands.
/// </summary>
internal sealed class ReplCommands
{
  private readonly ReplSession Session;
  private readonly NuruApp NuruApp;
  private readonly ReplOptions Options;
  private readonly ITerminal Terminal;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly ReplHistory History;

  /// <summary>
  /// Creates a new instance of ReplCommands.
  /// </summary>
  /// <param name="session">The REPL session instance.</param>
  /// <param name="nuruApp">The NuruApp instance.</param>
  /// <param name="options">REPL configuration options.</param>
  /// <param name="terminal">Terminal interface for I/O.</param>
  /// <param name="typeConverterRegistry">Type converter registry for completions.</param>
  /// <param name="history">Command history manager.</param>
  internal ReplCommands
  (
    ReplSession session,
    NuruApp nuruApp,
    ReplOptions options,
    ITerminal terminal,
    ITypeConverterRegistry typeConverterRegistry,
    ReplHistory history
  )
  {
    Session = session ?? throw new ArgumentNullException(nameof(session));
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    TypeConverterRegistry = typeConverterRegistry ?? throw new ArgumentNullException(nameof(typeConverterRegistry));
    History = history ?? throw new ArgumentNullException(nameof(history));
  }

  /// <summary>
  /// Shows REPL help information.
  /// </summary>
  public void ShowReplHelp()
  {
    if (Options.EnableColors)
    {
      Terminal.WriteLine(AnsiColors.BrightBlue + "REPL Commands:" + AnsiColors.Reset);
    }
    else
    {
      Terminal.WriteLine("REPL Commands:");
    }

    Terminal.WriteLine("  exit, quit, q     - Exit the REPL");
    Terminal.WriteLine("  help, ?           - Show this help");
    Terminal.WriteLine("  history           - Show command history");
    Terminal.WriteLine("  clear, cls        - Clear the screen");
    Terminal.WriteLine("  clear-history     - Clear command history");
    Terminal.WriteLine();

    Terminal.WriteLine("Any other input is executed as an application command.");
    Terminal.WriteLine("Use Ctrl+C to cancel current operation or Ctrl+D to exit.");

    // Show available application commands using CompletionProvider
    Terminal.WriteLine("\nAvailable Application Commands:");
    try
    {
      CompletionProvider provider = new(TypeConverterRegistry);
      CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: NuruApp.Endpoints);
      IEnumerable<CompletionCandidate> completionsEnumerable = provider.GetCompletions(context, NuruApp.Endpoints);
      List<CompletionCandidate> completions = [.. completionsEnumerable];

      if (completions.Count > 0)
      {
        foreach (CompletionCandidate cand in completions.OrderBy(c => c.Value))
        {
          string desc = string.IsNullOrEmpty(cand.Description) ? "" : $" - {cand.Description}";
          Terminal.WriteLine($"  {cand.Value}{desc}");
        }
      }
      else
      {
        Terminal.WriteLine("  No commands available.");
      }
    }
    catch (InvalidOperationException)
    {
      // Ignore completion errors for basic help
      Terminal.WriteLine("  (Completions unavailable - check configuration)");
    }
    catch (ArgumentException)
    {
      // Ignore completion errors for basic help
      Terminal.WriteLine("  (Completions unavailable - check configuration)");
    }
  }

  /// <summary>
  /// Shows command history.
  /// </summary>
  public void ShowHistory()
  {
    if (History.Count == 0)
    {
      Terminal.WriteLine("No commands in history.");
      return;
    }

    Terminal.WriteLine("Command History:");
    IReadOnlyList<string> items = History.AsReadOnly;
    for (int i = 0; i < items.Count; i++)
    {
      Terminal.WriteLine($"  {i + 1}: {items[i]}");
    }
  }

  /// <summary>
  /// Exits the REPL loop.
  /// </summary>
  public void Exit()
  {
    Session.Stop();
  }

  /// <summary>
  /// Clears the command history.
  /// </summary>
  public void ClearHistory() => History.Clear();

  /// <summary>
  /// Clears the terminal screen.
  /// </summary>
  public void ClearScreen()
  {
    Terminal.Clear();
  }
}
