namespace TimeWarp.Nuru;

/// <summary>
/// Implements built-in REPL commands.
/// </summary>
internal sealed class ReplCommands
{
  private readonly ReplSession Session;
  private readonly ITerminal Terminal;
  private readonly ReplHistory History;

  /// <summary>
  /// Creates a new instance of ReplCommands.
  /// </summary>
  /// <param name="session">The REPL session instance.</param>
  /// <param name="terminal">Terminal interface for I/O.</param>
  /// <param name="history">Command history manager.</param>
  internal ReplCommands
  (
    ReplSession session,
    ITerminal terminal,
    ReplHistory history
  )
  {
    Session = session ?? throw new ArgumentNullException(nameof(session));
    Terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    History = history ?? throw new ArgumentNullException(nameof(history));
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
