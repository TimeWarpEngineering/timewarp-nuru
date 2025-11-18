namespace TimeWarp.Nuru.Repl;

using System.Diagnostics;

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// </summary>
internal sealed class ReplMode
{
  private readonly NuruApp App;
  private readonly ReplOptions Options;
  private readonly List<string> History = [];
  private readonly ITypeConverterRegistry TypeConverterRegistryField;
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
    TypeConverterRegistryField = app.TypeConverterRegistry;
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
        if (Options.EnableColors)
        {
          string coloredPrompt = "\x1b[32m" + Options.Prompt + "\x1b[0m";
          Console.Write(coloredPrompt);
        }
        else
        {
          Console.Write(Options.Prompt);
        }

        // Read input
        string? input;
        if (Options.EnableArrowHistory)
        {
          input = ReadInputWithHistory();
        }
        else
        {
          input = Console.ReadLine();
        }

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

        var sw = Stopwatch.StartNew();
        try
        {
          lastExitCode = await App.RunAsync(args).ConfigureAwait(false);
          sw.Stop();

          if (Options.ShowExitCode)
          {
            if (Options.EnableColors)
            {
              Console.WriteLine($"\x1b[90mExit code: {lastExitCode}\x1b[0m");
            }
            else
            {
              Console.WriteLine($"Exit code: {lastExitCode}");
            }
          }

          if (Options.ShowTiming)
          {
            if (Options.EnableColors)
            {
              Console.WriteLine($"\x1b[90m({sw.ElapsedMilliseconds}ms)\x1b[0m");
            }
            else
            {
              Console.WriteLine($"({sw.ElapsedMilliseconds}ms)");
            }
          }

          if (!Options.ContinueOnError && lastExitCode != 0)
          {
            if (Options.EnableColors)
            {
              Console.WriteLine($"\x1b[31mCommand failed with exit code {lastExitCode}. Exiting REPL.\x1b[0m");
            }
            else
            {
              Console.WriteLine($"Command failed with exit code {lastExitCode}. Exiting REPL.");
            }

            break;
          }
        }
        catch (InvalidOperationException ex)
        {
          sw.Stop();
          if (Options.EnableColors)
          {
            Console.WriteLine($"\x1b[31mError executing command: {ex.Message}\x1b[0m");
          }
          else
          {
            Console.WriteLine($"Error executing command: {ex.Message}");
          }

          lastExitCode = 1;

          if (Options.ShowTiming)
          {
            if (Options.EnableColors)
            {
              Console.WriteLine($"\x1b[90m({sw.ElapsedMilliseconds}ms)\x1b[0m");
            }
            else
            {
              Console.WriteLine($"({sw.ElapsedMilliseconds}ms)");
            }
          }

          if (!Options.ContinueOnError)
          {
            break;
          }
        }
        catch (ArgumentException ex)
        {
          sw.Stop();
          if (Options.EnableColors)
          {
            Console.WriteLine($"\x1b[31mInvalid argument: {ex.Message}\x1b[0m");
          }
          else
          {
            Console.WriteLine($"Invalid argument: {ex.Message}");
          }

          lastExitCode = 1;

          if (Options.ShowTiming)
          {
            if (Options.EnableColors)
            {
              Console.WriteLine($"\x1b[90m({sw.ElapsedMilliseconds}ms)\x1b[0m");
            }
            else
            {
              Console.WriteLine($"({sw.ElapsedMilliseconds}ms)");
            }
          }

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

  private string ReadInputWithHistory()
  {
    string currentLine = "";
    int historyIndex = History.Count;

    while (true)
    {
      ConsoleKeyInfo keyInfo = Console.ReadKey(true);

      if (keyInfo.Key == ConsoleKey.Enter)
      {
        Console.WriteLine();
        return currentLine;
      }
      else if (keyInfo.Key == ConsoleKey.UpArrow)
      {
        if (historyIndex > 0)
        {
          historyIndex--;
          currentLine = History[historyIndex];
          // Clear current line and rewrite
          Console.SetCursorPosition(0, Console.CursorTop);
          Console.Write(new string(' ', Console.WindowWidth));
          Console.SetCursorPosition(0, Console.CursorTop);
          Console.Write(currentLine);
        }
      }
      else if (keyInfo.Key == ConsoleKey.DownArrow)
      {
        historyIndex++;
        if (historyIndex >= History.Count)
        {
          historyIndex = History.Count;
          currentLine = "";
        }
        else
        {
          currentLine = History[historyIndex];
        }
        // Clear and rewrite
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(currentLine);
      }
      else if (keyInfo.Key == ConsoleKey.Backspace && currentLine.Length > 0)
      {
        currentLine = currentLine[0..^1];
        Console.Write("\b \b");
      }
      else if (!char.IsControl(keyInfo.KeyChar))
      {
        currentLine += keyInfo.KeyChar;
        Console.Write(keyInfo.KeyChar);
      }
      // Ignore other keys for basic implementation
    }
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

  private void ShowReplHelp()
  {
    if (Options.EnableColors)
    {
      Console.WriteLine("\x1b[1;34mREPL Commands:\x1b[0m");
    }
    else
    {
      Console.WriteLine("REPL Commands:");
    }

    Console.WriteLine("  exit, quit, q     - Exit the REPL");
    Console.WriteLine("  help, ?           - Show this help");
    Console.WriteLine("  history           - Show command history");
    Console.WriteLine("  clear, cls        - Clear the screen");
    Console.WriteLine("  clear-history     - Clear command history");
    Console.WriteLine();

    Console.WriteLine("Any other input is executed as an application command.");
    Console.WriteLine("Use Ctrl+C to cancel current operation or Ctrl+D to exit.");

    // Show available application commands using CompletionProvider
    Console.WriteLine("\nAvailable Application Commands:");
    try
    {
      CompletionProvider provider = new(TypeConverterRegistryField);
      CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: App.Endpoints);
      IEnumerable<CompletionCandidate> completionsEnumerable = provider.GetCompletions(context, App.Endpoints);
      List<CompletionCandidate> completions = [.. completionsEnumerable];

      if (completions.Count > 0)
      {
        foreach (CompletionCandidate cand in completions.OrderBy(c => c.Value))
        {
          string desc = string.IsNullOrEmpty(cand.Description) ? "" : $" - {cand.Description}";
          Console.WriteLine($"  {cand.Value}{desc}");
        }
      }
      else
      {
        Console.WriteLine("  No commands available.");
      }
    }
    catch (InvalidOperationException)
    {
      // Ignore completion errors for basic help
      Console.WriteLine("  (Completions unavailable - check configuration)");
    }
    catch (ArgumentException)
    {
      // Ignore completion errors for basic help
      Console.WriteLine("  (Completions unavailable - check configuration)");
    }
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

    // Use per-app history in ~/.nuru/history/ directory
    string nuruDir = Path.Combine(
      Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
      ".nuru"
    );
    string historyDir = Path.Combine(nuruDir, "history");

    // Ensure directory exists
    Directory.CreateDirectory(historyDir);

    // Use consistent app name detection
    string appName = AppNameDetector.GetEffectiveAppName();
    return Path.Combine(historyDir, appName);
  }
}