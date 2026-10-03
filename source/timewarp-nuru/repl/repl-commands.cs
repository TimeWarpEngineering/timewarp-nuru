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
  /// Shows command history asynchronously.
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
  /// Clears the command history.
  /// </summary>
  public void ClearHistory()
  {
    History.Clear();
  }

  /// <summary>
  /// Clears the command history asynchronously.
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

  /// <summary>
  /// Prints the key bindings for a built-in profile.
  /// </summary>
  /// <param name="args">The REPL tokens, starting with <c>key-bindings</c>.</param>
  /// <param name="activeProfileName">The profile the session is using, when the user does not pass <c>--profile</c>.</param>
  public void ShowKeyBindings(string[] args, string activeProfileName)
  {
    ArgumentNullException.ThrowIfNull(args);

    string? profile = null;
    string? key = null;
    string? function = null;
    bool detailed = false;

    for (int index = 1; index < args.Length; index++)
    {
      string arg = args[index];
      if (arg is "--detailed" or "-d")
      {
        detailed = true;
        continue;
      }

      if (arg is "--profile" or "-p")
      {
        if (!TryTakeValue(args, ref index, out profile))
        {
          WriteKeyBindingsUsage("Missing value for --profile.");
          return;
        }

        continue;
      }

      if (arg is "--key" or "-k")
      {
        if (!TryTakeValue(args, ref index, out key))
        {
          WriteKeyBindingsUsage("Missing value for --key.");
          return;
        }

        continue;
      }

      if (arg is "--function" or "-f")
      {
        if (!TryTakeValue(args, ref index, out function))
        {
          WriteKeyBindingsUsage("Missing value for --function.");
          return;
        }

        continue;
      }

      if (profile is null && KeyBindingCatalog.IsKnownProfile(arg))
      {
        profile = arg;
        continue;
      }

      WriteKeyBindingsUsage($"Unknown argument '{arg}'.");
      return;
    }

    string profileName = profile ?? activeProfileName;
    try
    {
      Terminal.WriteLine(KeyBindingCatalog.Render(profileName, key, function, detailed));
    }
    catch (ArgumentException exception)
    {
      Terminal.WriteErrorLine(exception.Message);
    }
  }

  private void WriteKeyBindingsUsage(string message)
  {
    Terminal.WriteErrorLine(message);
    Terminal.WriteErrorLine
    (
      "Usage: key-bindings [--profile <name>] [--key <chord>] [--function <name>] [--detailed]"
    );
  }

  private static bool TryTakeValue(string[] args, ref int index, out string? value)
  {
    if (index + 1 >= args.Length || args[index + 1].StartsWith('-'))
    {
      value = null;
      return false;
    }

    index++;
    value = args[index];
    return true;
  }
}
