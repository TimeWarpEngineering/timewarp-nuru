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
  private readonly CompletionProvider CompletionProvider;
  private readonly ReplHistory History;

  /// <summary>
  /// Creates a new instance of ReplCommands.
  /// </summary>
  /// <param name="session">The REPL session instance.</param>
  /// <param name="nuruApp">The NuruApp instance.</param>
  /// <param name="options">REPL configuration options.</param>
  /// <param name="terminal">Terminal interface for I/O.</param>
  /// <param name="completionProvider">Completion provider for command suggestions.</param>
  /// <param name="history">Command history manager.</param>
  internal ReplCommands
  (
    ReplSession session,
    NuruApp nuruApp,
    ReplOptions options,
    ITerminal terminal,
    CompletionProvider completionProvider,
    ReplHistory history
  )
  {
    Session = session ?? throw new ArgumentNullException(nameof(session));
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    Options = options ?? throw new ArgumentNullException(nameof(options));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    CompletionProvider = completionProvider ?? throw new ArgumentNullException(nameof(completionProvider));
    History = history ?? throw new ArgumentNullException(nameof(history));
  }

  /// <summary>
  /// Shows REPL help information including built-in commands and keyboard shortcuts.
  /// </summary>
  public async Task ShowReplHelpAsync()
  {
    if (Options.EnableColors)
    {
      await Terminal.WriteLineAsync(AnsiColors.BrightBlue + "REPL Commands:" + AnsiColors.Reset).ConfigureAwait(false);
    }
    else
    {
      await Terminal.WriteLineAsync("REPL Commands:").ConfigureAwait(false);
    }

    await Terminal.WriteLineAsync("  exit, quit, q     - Exit the REPL").ConfigureAwait(false);
    await Terminal.WriteLineAsync("  help, ?           - Show this help").ConfigureAwait(false);
    await Terminal.WriteLineAsync("  history           - Show command history").ConfigureAwait(false);
    await Terminal.WriteLineAsync("  clear, cls        - Clear the screen").ConfigureAwait(false);
    await Terminal.WriteLineAsync("  clear-history     - Clear command history").ConfigureAwait(false);
    await Terminal.WriteLineAsync().ConfigureAwait(false);

    await Terminal.WriteLineAsync("Any other input is executed as an application command.").ConfigureAwait(false);
    await Terminal.WriteLineAsync("Use Ctrl+C to cancel current operation or Ctrl+D to exit.").ConfigureAwait(false);

    await ShowAvailableCommandsAsync().ConfigureAwait(false);
  }

  /// <summary>
  /// Shows available application commands using completion provider.
  /// </summary>
  private async Task ShowAvailableCommandsAsync()
  {
    await Terminal.WriteLineAsync("\nAvailable Application Commands:").ConfigureAwait(false);
    try
    {
      CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: NuruApp.Endpoints);
      IEnumerable<CompletionCandidate> completionsEnumerable =
        CompletionProvider.GetCompletions(context, NuruApp.Endpoints);
      List<CompletionCandidate> completions = [.. completionsEnumerable];

      if (completions.Count > 0)
      {
        foreach (CompletionCandidate cand in completions.OrderBy(c => c.Value))
        {
          string desc = string.IsNullOrEmpty(cand.Description) ? "" : $" - {cand.Description}";
          await Terminal.WriteLineAsync($"  {cand.Value}{desc}").ConfigureAwait(false);
        }
      }
      else
      {
        await Terminal.WriteLineAsync("  No commands available.").ConfigureAwait(false);
      }
    }
    catch (InvalidOperationException)
    {
      await Terminal.WriteLineAsync("  (Completions unavailable - check configuration)").ConfigureAwait(false);
    }
    catch (ArgumentException)
    {
      await Terminal.WriteLineAsync("  (Completions unavailable - check configuration)").ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Shows command history.
  /// </summary>
  public async Task ShowHistoryAsync()
  {
    if (History.Count == 0)
    {
      await Terminal.WriteLineAsync("No commands in history.").ConfigureAwait(false);
      return;
    }

    await Terminal.WriteLineAsync("Command History:").ConfigureAwait(false);
    IReadOnlyList<string> items = History.AsReadOnly;
    for (int i = 0; i < items.Count; i++)
    {
      await Terminal.WriteLineAsync($"  {i + 1}: {items[i]}").ConfigureAwait(false);
    }
  }

  /// <summary>
  /// Exits the REPL loop.
  /// </summary>
  public Task ExitAsync()
  {
    Session.Stop();
    return Task.CompletedTask;
  }

  /// <summary>
  /// Clears the command history.
  /// </summary>
  public Task ClearHistoryAsync()
  {
    History.Clear();
    return Task.CompletedTask;
  }

  /// <summary>
  /// Clears the terminal screen.
  /// </summary>
  public Task ClearScreenAsync()
  {
    Terminal.Clear();
    return Task.CompletedTask;
  }
}
