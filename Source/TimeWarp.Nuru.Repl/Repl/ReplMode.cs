namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// </summary>
internal sealed class ReplMode
{
  private readonly NuruApp App;
  private readonly ReplOptions Options;
  private readonly List<string> History = [];
  private bool Running;

  /// <summary>
  /// Creates a new REPL mode instance.
  /// </summary>
  /// <param name="app">The NuruApp instance to execute commands against.</param>
  /// <param name="options">Optional configuration for the REPL.</param>
  public ReplMode(NuruApp app, ReplOptions? options = null)
  {
    App = app ?? throw new ArgumentNullException(nameof(app));
    Options = options ?? new ReplOptions();
  }

  /// <summary>
  /// Starts the REPL loop, reading and executing commands until exit.
  /// </summary>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  /// <returns>The exit code of the last executed command, or 0 if no commands were executed.</returns>
  public async Task<int> RunAsync(CancellationToken cancellationToken = default)
  {
    Running = true;
    int lastExitCode = 0;

    // Display welcome message
    if (!string.IsNullOrEmpty(Options.WelcomeMessage))
    {
      Console.WriteLine(Options.WelcomeMessage);
    }

    // Load history if persistence is enabled
    if (Options.PersistHistory)
    {
      LoadHistory();
    }

    // Handle Ctrl+C gracefully
    Console.CancelKeyPress += OnCancelKeyPress;

    try
    {
      while (Running && !cancellationToken.IsCancellationRequested)
      {
        // Display prompt
        Console.Write(Options.Prompt);

        // Read input
        string? input = Console.ReadLine();

        // Handle EOF (Ctrl+D on Unix, Ctrl+Z on Windows)
        if (input is null)
        {
          Console.WriteLine();
          break;
        }

        // Skip empty input
        string trimmedInput = input.Trim();
        if (string.IsNullOrEmpty(trimmedInput))
        {
          continue;
        }

        // Add to history
        AddToHistory(trimmedInput);

        // Check for special REPL commands
        if (await HandleSpecialCommandAsync(trimmedInput).ConfigureAwait(false))
        {
          continue;
        }

        // Parse and execute command
        string[] args = CommandLineParser.Parse(trimmedInput);
        if (args.Length == 0)
        {
          continue;
        }

        try
        {
          lastExitCode = await App.RunAsync(args).ConfigureAwait(false);

          if (Options.ShowExitCode)
          {
            Console.WriteLine($"Exit code: {lastExitCode}");
          }

          if (!Options.ContinueOnError && lastExitCode != 0)
          {
            Console.WriteLine($"Command failed with exit code {lastExitCode}. Exiting REPL.");
            break;
          }
        }
        catch (InvalidOperationException ex)
        {
          Console.WriteLine($"Error executing command: {ex.Message}");
          lastExitCode = 1;

          if (!Options.ContinueOnError)
          {
            break;
          }
        }
        catch (ArgumentException ex)
        {
          Console.WriteLine($"Invalid argument: {ex.Message}");
          lastExitCode = 1;

          if (!Options.ContinueOnError)
          {
            break;
          }
        }
      }
    }
    finally
    {
      Console.CancelKeyPress -= OnCancelKeyPress;

      // Save history if persistence is enabled
      if (Options.PersistHistory)
      {
        SaveHistory();
      }

      // Display goodbye message
      if (!string.IsNullOrEmpty(Options.GoodbyeMessage))
      {
        Console.WriteLine(Options.GoodbyeMessage);
      }
    }

    return lastExitCode;
  }

  private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
  {
    e.Cancel = true; // Prevent immediate termination
    Running = false;
    Console.WriteLine();
  }

  private async Task<bool> HandleSpecialCommandAsync(string input)
  {
    string command = input.ToLowerInvariant();

    switch (command)
    {
      case "exit":
      case "quit":
      case "q":
        Running = false;
        return true;

      case "help":
      case "?":
        ShowReplHelp();
        return true;

      case "history":
        ShowHistory();
        return true;

      case "clear":
      case "cls":
        Console.Clear();
        return true;

      case "clear-history":
        History.Clear();
        Console.WriteLine("History cleared.");
        return true;

      default:
        return false;
    }
  }

  private static void ShowReplHelp()
  {
    Console.WriteLine("REPL Commands:");
    Console.WriteLine("  exit, quit, q     - Exit the REPL");
    Console.WriteLine("  help, ?           - Show this help");
    Console.WriteLine("  history           - Show command history");
    Console.WriteLine("  clear, cls        - Clear the screen");
    Console.WriteLine("  clear-history     - Clear command history");
    Console.WriteLine();
    Console.WriteLine("Any other input is executed as an application command.");
    Console.WriteLine("Use Ctrl+C to cancel current operation or Ctrl+D to exit.");
  }

  private void ShowHistory()
  {
    if (History.Count == 0)
    {
      Console.WriteLine("No commands in history.");
      return;
    }

    Console.WriteLine("Command History:");
    for (int i = 0; i < History.Count; i++)
    {
      Console.WriteLine($"  {i + 1}: {History[i]}");
    }
  }

  private void AddToHistory(string command)
  {
    // Don't add if same as last command
    if (History.Count > 0 && History[^1] == command)
    {
      return;
    }

    History.Add(command);

    // Trim history if it exceeds max size
    while (History.Count > Options.MaxHistorySize)
    {
      History.RemoveAt(0);
    }
  }

  private void LoadHistory()
  {
    string historyPath = GetHistoryFilePath();
    if (!File.Exists(historyPath))
    {
      return;
    }

    try
    {
      string[] lines = File.ReadAllLines(historyPath);
      foreach (string line in lines.TakeLast(Options.MaxHistorySize))
      {
        if (!string.IsNullOrWhiteSpace(line))
        {
          History.Add(line);
        }
      }
    }
    catch (IOException ex)
    {
      Console.WriteLine($"Warning: Could not load history: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
      Console.WriteLine($"Warning: Could not load history: {ex.Message}");
    }
  }

  private void SaveHistory()
  {
    string historyPath = GetHistoryFilePath();

    try
    {
      string? directory = Path.GetDirectoryName(historyPath);
      if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      File.WriteAllLines(historyPath, History);
    }
    catch (IOException ex)
    {
      Console.WriteLine($"Warning: Could not save history: {ex.Message}");
    }
    catch (UnauthorizedAccessException ex)
    {
      Console.WriteLine($"Warning: Could not save history: {ex.Message}");
    }
  }

  private string GetHistoryFilePath()
  {
    if (!string.IsNullOrEmpty(Options.HistoryFilePath))
    {
      return Options.HistoryFilePath;
    }

    // Default to user's home directory
    string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    return Path.Combine(homeDir, ".nuru_history");
  }
}
