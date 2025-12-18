namespace TimeWarp.Nuru;

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// Implements IDisposable to ensure proper cleanup of resources (event handlers, history persistence).
/// </summary>
internal sealed class ReplSession : IDisposable
{
  private readonly ILoggerFactory LoggerFactory;
  private readonly NuruCoreApp NuruCoreApp;
  private readonly ReplOptions ReplOptions;
  private readonly ReplHistory History;
  private readonly ITypeConverterRegistry TypeConverterRegistry;
  private readonly ITerminal Terminal;
  private readonly CompletionProvider CompletionProvider;
  private readonly ReplCommands Commands;
  private bool Running;
  private bool Disposed;

  /// <summary>
  /// Gets the current active REPL session instance.
  /// This is guaranteed to be non-null when REPL commands execute.
  /// </summary>
  public static ReplSession CurrentSession { get; private set; } = null!;

  /// <summary>
  /// Gets the commands interface for this REPL session.
  /// </summary>
  internal ReplCommands GetCommands() => Commands;

  /// <summary>
  /// Creates a new REPL mode instance.
  /// </summary>
  /// <param name="nuruApp">The NuruCoreApp instance to execute commands against.</param>
  /// <param name="replOptions">Optional configuration for the REPL.</param>
  /// <param name="loggerFactory">Logger factory for logging operations.</param>
  internal ReplSession
  (
    NuruCoreApp nuruApp,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory
  )
  {
    NuruCoreApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    ReplOptions = replOptions ?? new ReplOptions();
    TypeConverterRegistry = nuruApp.TypeConverterRegistry;
    LoggerFactory = loggerFactory;
    Terminal = nuruApp.Terminal;

    // Create CompletionProvider once for entire session
    CompletionProvider = new CompletionProvider(TypeConverterRegistry, LoggerFactory);

    History = new ReplHistory(ReplOptions, Terminal);

    Commands = new ReplCommands(this, Terminal, History);
  }

  /// <summary>
  /// Disposes the REPL session and ensures cleanup of resources.
  /// This method is idempotent and can be called multiple times safely.
  /// </summary>
  public void Dispose()
  {
    if (Disposed) return;

    // Critical cleanup that must happen
    Console.CancelKeyPress -= OnCancelKeyPress;

    if (ReplOptions.PersistHistory)
      History.Save();

    Disposed = true;
  }

  /// <summary>
  /// Runs a REPL session asynchronously.
  /// </summary>
  /// <param name="nuruApp">The NuruCoreApp instance to execute commands against.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="loggerFactory">Logger factory for logging.</param>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  public static async Task RunAsync
  (
    NuruCoreApp nuruApp,
    ReplOptions replOptions,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken = default
  )
  {
    CurrentSession = new ReplSession(nuruApp, replOptions, loggerFactory);

    try
    {
      await CurrentSession.RunInstanceAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      // Guaranteed cleanup even on exceptions
      CurrentSession.Dispose();
      CurrentSession = null!;
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

    // Mark session context as REPL mode for help display
    NuruCoreApp.SessionContext.IsReplSession = true;

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
      ReplConsoleReader consoleReader =
        new          (
            History.AsReadOnly,
            CompletionProvider,
            NuruCoreApp.Endpoints,
            ReplOptions,
            LoggerFactory,
            Terminal
          );
      return consoleReader.ReadLine(ReplOptions.Prompt);
    }

    Terminal.Write(PromptFormatter.Format(ReplOptions));
    return Terminal.ReadLine();
  }

  private async Task<int> ExecuteCommandAsync(string[] args)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    try
    {
      int exitCode = await NuruCoreApp.RunAsync(args).ConfigureAwait(false);
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
      string text = $"Exit code: {exitCode}";
      Terminal.WriteLine(ReplOptions.EnableColors ? text.Gray() : text);
    }

    if (ReplOptions.ShowTiming)
    {
      string text = $"({elapsedMs}ms)";
      Terminal.WriteLine(ReplOptions.EnableColors ? text.Gray() : text);
    }

    if (!success)
    {
      string message = errorMessage ?? $"Command failed with exit code {exitCode}";
      Terminal.WriteLine(ReplOptions.EnableColors ? message.Red() : message);
    }
    else if (!ReplOptions.ContinueOnError && exitCode != 0)
    {
      string message = $"Command failed with exit code {exitCode}. Exiting REPL.";
      Terminal.WriteLine(ReplOptions.EnableColors ? message.Red() : message);
    }
  }

  private void CleanupRepl()
  {
    // Reset session context back to CLI mode
    NuruCoreApp.SessionContext.IsReplSession = false;

    // Dispose handles critical cleanup (event handler, history save)
    Dispose();

    // Display goodbye message (non-critical, cosmetic)
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
  /// Stops the REPL session. Called by Exit command.
  /// </summary>
  internal void Stop()
  {
    Running = false;
  }

  // Static wrapper methods for route registration
  // These provide clean method group syntax for REPL command routes

  /// <summary>
  /// Exits the REPL session.
  /// </summary>
  public static Task ExitAsync() => CurrentSession.GetCommands().ExitAsync();

  /// <summary>
  /// Shows the command history.
  /// </summary>
  public static Task ShowHistoryAsync() => CurrentSession.GetCommands().ShowHistoryAsync();

  /// <summary>
  /// Clears the terminal screen.
  /// </summary>
  public static Task ClearScreenAsync() => CurrentSession.GetCommands().ClearScreenAsync();

  /// <summary>
  /// Clears the command history.
  /// </summary>
  public static Task ClearHistoryAsync() => CurrentSession.GetCommands().ClearHistoryAsync();
}
