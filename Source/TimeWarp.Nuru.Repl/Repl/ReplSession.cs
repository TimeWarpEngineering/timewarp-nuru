namespace TimeWarp.Nuru.Repl;

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// </summary>
internal sealed class ReplSession
{
  private readonly ILoggerFactory LoggerFactory;
  private readonly NuruApp NuruApp;
  private readonly ReplOptions ReplOptions;
  private readonly ReplHistory History;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly ITerminal Terminal;
  private bool Running;

  /// <summary>
  /// Gets the current active REPL session instance, if any.
  /// </summary>
  public static ReplSession? CurrentSession { get; private set; }

  /// <summary>
  /// Creates a new REPL mode instance.
  /// </summary>
  /// <param name="nuruApp">The NuruApp instance to execute commands against.</param>
  /// <param name="replOptions">Optional configuration for the REPL.</param>
  internal ReplSession
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
    Terminal = nuruApp.Terminal;
    History = new ReplHistory(ReplOptions, Terminal);
  }

  /// <summary>
  /// Runs a REPL session asynchronously.
  /// </summary>
  /// <param name="nuruApp">The NuruApp instance to execute commands against.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="loggerFactory">Logger factory for logging.</param>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  /// <returns>The exit code of the last executed command, or 0 if no commands were executed.</returns>
  public static async Task<int> RunAsync
  (
    NuruApp nuruApp,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken = default
  )
  {
    CurrentSession = new ReplSession(nuruApp, replOptions, loggerFactory);

    try
    {
      return await CurrentSession.RunInstanceAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      CurrentSession = null;
    }
  }

  /// <summary>
  /// Runs this REPL instance.
  /// </summary>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  /// <returns>The exit code of the last executed command, or 0 if no commands were executed.</returns>
  private async Task<int> RunInstanceAsync(CancellationToken cancellationToken = default)
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
      Terminal.WriteLine(ReplOptions.WelcomeMessage);

    // Load history if persistence is enabled
    if (ReplOptions.PersistHistory) History.Load();

    // Handle Ctrl+C gracefully - still uses System.Console for event subscription
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
    // Read input
    string? input = ReadCommandInput();

    // Handle EOF (Ctrl+D on Unix, Ctrl+Z on Windows)
    if (input is null)
    {
      await Terminal.WriteLineAsync().ConfigureAwait(false);
      Running = false;
      return 0;
    }

    // Skip empty input
    string trimmedInput = input.Trim();
    if (string.IsNullOrEmpty(trimmedInput)) return 0;

    History.Add(trimmedInput);

    // Parse and execute command - routes handle everything including REPL commands
    string[] args = CommandLineParser.Parse(trimmedInput);
    if (args.Length == 0) return 0;

    return await ExecuteCommandAsync(args).ConfigureAwait(false);
  }

  private string? ReadCommandInput()
  {
    if (ReplOptions.EnableArrowHistory)
    {
      var consoleReader =
        new ReplConsoleReader
          (
            History.AsReadOnly,
            new CompletionProvider(TypeConverterRegistry, LoggerFactory),
            NuruApp.Endpoints,
            ReplOptions,
            LoggerFactory,
            Terminal
          );
      return consoleReader.ReadLine(ReplOptions.Prompt);
    }

    return Terminal.ReadLine();
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
        Terminal.WriteLine(AnsiColors.Gray + $"Exit code: {exitCode}" + AnsiColors.Reset);
      else
        Terminal.WriteLine($"Exit code: {exitCode}");
    }

    if (ReplOptions.ShowTiming)
    {
      if (ReplOptions.EnableColors)
        Terminal.WriteLine(AnsiColors.Gray + $"({elapsedMs}ms)" + AnsiColors.Reset);
      else
        Terminal.WriteLine($"({elapsedMs}ms)");
    }

    if (!success)
    {
      string message = errorMessage ?? $"Command failed with exit code {exitCode}";
      if (ReplOptions.EnableColors)
        Terminal.WriteLine(AnsiColors.Red + message + AnsiColors.Reset);
      else
        Terminal.WriteLine(message);
    }
    else if (!ReplOptions.ContinueOnError && exitCode != 0)
    {
      string message = $"Command failed with exit code {exitCode}. Exiting REPL.";
      if (ReplOptions.EnableColors)
        Terminal.WriteLine(AnsiColors.Red + message + AnsiColors.Reset);
      else
        Terminal.WriteLine(message);
    }
  }

  private void CleanupRepl()
  {
    Console.CancelKeyPress -= OnCancelKeyPress;

    // Save history if persistence is enabled
    if (ReplOptions.PersistHistory)
      History.Save();

    // Display goodbye message
    if (!string.IsNullOrEmpty(ReplOptions.GoodbyeMessage))
      Terminal.WriteLine(ReplOptions.GoodbyeMessage);
  }

  private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
  {
    e.Cancel = true; // Prevent immediate termination
    Running = false;
    Terminal.WriteLine();
  }

  /// <summary>
  /// Shows REPL help information.
  /// </summary>
  public void ShowReplHelp()
  {
    if (ReplOptions.EnableColors)
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
    Running = false;
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
