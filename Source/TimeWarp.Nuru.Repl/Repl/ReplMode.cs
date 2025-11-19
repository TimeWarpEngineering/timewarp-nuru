namespace TimeWarp.Nuru.Repl;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using TimeWarp.Nuru.Logging;
using TimeWarp.Nuru.Repl.Input;

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// </summary>
public sealed class ReplMode
{
  private readonly ILoggerFactory LoggerFactory;
  private readonly NuruApp NuruApp;
  private readonly ReplOptions ReplOptions;
  private readonly List<string> History = [];
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private bool Running;

  /// <summary>
  /// Creates a new REPL mode instance.
  /// </summary>
  /// <param name="nuruApp">The NuruApp instance to execute commands against.</param>
  /// <param name="replOptions">Optional configuration for the REPL.</param>
  public ReplMode
  (
    NuruApp nuruApp,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory
  )
  {
    NuruApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    ReplOptions = replOptions ?? new ReplOptions();
    TypeConverterRegistry = nuruApp.TypeConverterRegistry;
    LoggerFactory = loggerFactory;
  }

  /// <summary>
  /// Starts the REPL loop, reading and executing commands until exit.
  /// </summary>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  /// <returns>The exit code of the last executed command, or 0 if no commands were executed.</returns>
  public async Task<int> RunAsync(CancellationToken cancellationToken = default)
  {
    InitializeRepl();

    int result = await ProcessCommandLoopAsync(cancellationToken).ConfigureAwait(false);

    CleanupRepl();

    return result;
  }

  private void InitializeRepl()
  {
    Running = true;

    // Display welcome message
    if (!string.IsNullOrEmpty(ReplOptions.WelcomeMessage))
      Console.WriteLine(ReplOptions.WelcomeMessage);

    // Load history if persistence is enabled
    if (ReplOptions.PersistHistory) LoadHistory();

    // Handle Ctrl+C gracefully
    Console.CancelKeyPress += OnCancelKeyPress;
  }

  private async Task<int> ProcessCommandLoopAsync(CancellationToken cancellationToken)
  {
    int lastExitCode = 0;

    while (Running && !cancellationToken.IsCancellationRequested)
    {
      lastExitCode = await ProcessSingleCommandAsync().ConfigureAwait(false);
    }

    return lastExitCode;
  }

  private async Task<int> ProcessSingleCommandAsync()
  {
    // Display prompt
    // DisplayPrompt();

    // Read input
    string? input = ReadCommandInput();

    // Handle EOF (Ctrl+D on Unix, Ctrl+Z on Windows)
    if (input is null)
    {
      Console.WriteLine();
      Running = false;
      return 0;
    }

    // Skip empty input
    string trimmedInput = input.Trim();
    if (string.IsNullOrEmpty(trimmedInput)) return 0;

    AddToHistory(trimmedInput);

    // Parse and execute command - routes handle everything including REPL commands
    string[] args = CommandLineParser.Parse(trimmedInput);
    if (args.Length == 0) return 0;

    return await ExecuteCommandAsync(args).ConfigureAwait(false);
  }

  private void DisplayPrompt()
  {
    Console.Write(GetFormattedPrompt());
  }

  private string GetFormattedPrompt()
  {
    return ReplOptions.EnableColors
      ? AnsiColors.Green + ReplOptions.Prompt + AnsiColors.Reset
      : ReplOptions.Prompt;
  }

  private void RewriteLineWithPrompt(string content)
  {
    string prompt = GetFormattedPrompt();
    // Clear current line and rewrite with prompt
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(new string(' ', Console.WindowWidth));
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(prompt + content);
  }

  private string? ReadCommandInput()
  {
    if (ReplOptions.EnableArrowHistory)
    {
      var consoleReader =
        new ReplConsoleReader
          (
            History,
            new CompletionProvider(TypeConverterRegistry, LoggerFactory),
            NuruApp.Endpoints,
            ReplOptions.EnableColors,
            ReplOptions.Prompt,
            LoggerFactory
          );
      return consoleReader.ReadLine(ReplOptions.Prompt);
    }

    return Console.ReadLine();
  }

  private async Task<int> ExecuteCommandAsync(string[] args)
  {
    var stopwatch = Stopwatch.StartNew();
    try
    {
      int exitCode = await NuruApp.RunAsync(args).ConfigureAwait(false);
      stopwatch.Stop();

      DisplayCommandResult(exitCode, stopwatch.ElapsedMilliseconds, success: true);

      if (!ReplOptions.ContinueOnError && exitCode != 0)
      {
        Running = false;
      }

      return exitCode;
    }
    catch (InvalidOperationException ex)
    {
      return HandleCommandException(stopwatch, ex);
    }
    catch (ArgumentException ex)
    {
      return HandleCommandException(stopwatch, ex);
    }
  }

  private int HandleCommandException(Stopwatch stopwatch, Exception ex)
  {
    stopwatch.Stop();
    DisplayCommandResult(1, stopwatch.ElapsedMilliseconds, success: false, ex.Message);

    if (!ReplOptions.ContinueOnError) Running = false;

    return 1;
  }

  private void DisplayCommandResult(int exitCode, long elapsedMs, bool success, string? errorMessage = null)
  {
    if (ReplOptions.ShowExitCode && success)
    {
      if (ReplOptions.EnableColors)
        Console.WriteLine(AnsiColors.Gray + $"Exit code: {exitCode}" + AnsiColors.Reset);
      else
        Console.WriteLine($"Exit code: {exitCode}");
    }

    if (ReplOptions.ShowTiming)
    {
      if (ReplOptions.EnableColors)
        Console.WriteLine(AnsiColors.Gray + $"({elapsedMs}ms)" + AnsiColors.Reset);
      else
        Console.WriteLine($"({elapsedMs}ms)");
    }

    if (!success)
    {
      string message = errorMessage ?? $"Command failed with exit code {exitCode}";
      if (ReplOptions.EnableColors)
        Console.WriteLine(AnsiColors.Red + message + AnsiColors.Reset);
      else
        Console.WriteLine(message);
    }
    else if (!ReplOptions.ContinueOnError && exitCode != 0)
    {
      string message = $"Command failed with exit code {exitCode}. Exiting REPL.";
      if (ReplOptions.EnableColors)
        Console.WriteLine(AnsiColors.Red + message + AnsiColors.Reset);
      else
        Console.WriteLine(message);
    }
  }

  private void CleanupRepl()
  {
    Console.CancelKeyPress -= OnCancelKeyPress;

    // Save history if persistence is enabled
    if (ReplOptions.PersistHistory)
      SaveHistory();

    // Display goodbye message
    if (!string.IsNullOrEmpty(ReplOptions.GoodbyeMessage))
      Console.WriteLine(ReplOptions.GoodbyeMessage);
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
          RewriteLineWithPrompt(currentLine);
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

        RewriteLineWithPrompt(currentLine);
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

  /// <summary>
  /// Shows REPL help information.
  /// </summary>
  public void ShowReplHelp()
  {
    if (ReplOptions.EnableColors)
    {
      Console.WriteLine(AnsiColors.BrightBlue + "REPL Commands:" + AnsiColors.Reset);
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
      CompletionProvider provider = new(TypeConverterRegistry);
      CompletionContext context = new(Args: [], CursorPosition: 0, Endpoints: NuruApp.Endpoints);
      IEnumerable<CompletionCandidate> completionsEnumerable = provider.GetCompletions(context, NuruApp.Endpoints);
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

  /// <summary>
  /// Shows command history.
  /// </summary>
  public void ShowHistory()
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

  /// <summary>
  /// Exits the REPL loop.
  /// </summary>
  public void Exit()
  {
    Running = false;
  }

  /// <summary>
  /// Clears the command history.
  /// </summary>
  public void ClearHistory()
  {
    History.Clear();
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
    while (History.Count > ReplOptions.MaxHistorySize)
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
      foreach (string line in lines.TakeLast(ReplOptions.MaxHistorySize))
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
    if (!string.IsNullOrEmpty(ReplOptions.HistoryFilePath))
    {
      return ReplOptions.HistoryFilePath;
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
