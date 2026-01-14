namespace TimeWarp.Nuru;

/// <summary>
/// Delegate for executing a command in the REPL loop.
/// The generated interceptor provides this to route commands through the generated matcher.
/// </summary>
/// <param name="app">The application instance.</param>
/// <param name="args">The parsed command-line arguments.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The exit code from command execution.</returns>
public delegate Task<int> ReplCommandExecutor(NuruCoreApp app, string[] args, CancellationToken cancellationToken);

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// Implements IDisposable to ensure proper cleanup of resources (event handlers, history persistence).
/// </summary>
internal sealed class ReplSession : IDisposable
{
  private readonly ILoggerFactory? LoggerFactory;
  private readonly NuruCoreApp NuruCoreApp;
  private readonly ReplOptions ReplOptions;
  private readonly ReplHistory History;
  private readonly IReplRouteProvider RouteProvider;
  private readonly ReplCommandExecutor CommandExecutor;
  private readonly ITerminal Terminal;
  private readonly ReplCommands Commands;
  private bool Running;
  private bool Disposed;

  /// <summary>
  /// Gets the current active REPL session instance.
  /// This is guaranteed to be non-null when REPL commands execute.
  /// </summary>
  public static ReplSession? CurrentSession { get; private set; }

  /// <summary>
  /// Gets the commands interface for this REPL session.
  /// </summary>
  internal ReplCommands GetCommands() => Commands;

  /// <summary>
  /// Creates a new REPL mode instance.
  /// </summary>
  /// <param name="nuruApp">The NuruCoreApp instance.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="routeProvider">Provider for route information (completion, highlighting).</param>
  /// <param name="commandExecutor">Delegate to execute commands through generated route matcher.</param>
  /// <param name="loggerFactory">Logger factory for logging operations.</param>
  internal ReplSession(
    NuruCoreApp nuruApp,
    ReplOptions replOptions,
    IReplRouteProvider routeProvider,
    ReplCommandExecutor commandExecutor,
    ILoggerFactory? loggerFactory)
  {
    NuruCoreApp = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
    ReplOptions = replOptions ?? new ReplOptions();
    RouteProvider = routeProvider ?? EmptyReplRouteProvider.Instance;
    CommandExecutor = commandExecutor ?? throw new ArgumentNullException(nameof(commandExecutor));
    LoggerFactory = loggerFactory;
    Terminal = nuruApp.Terminal;

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
  /// <param name="nuruApp">The NuruCoreApp instance.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="routeProvider">Provider for route information.</param>
  /// <param name="commandExecutor">Delegate to execute commands.</param>
  /// <param name="loggerFactory">Logger factory for logging.</param>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  public static async Task RunAsync(
    NuruCoreApp nuruApp,
    ReplOptions replOptions,
    IReplRouteProvider routeProvider,
    ReplCommandExecutor commandExecutor,
    ILoggerFactory? loggerFactory,
    CancellationToken cancellationToken = default)
  {
    CurrentSession = new ReplSession(nuruApp, replOptions, routeProvider, commandExecutor, loggerFactory);

    try
    {
      await CurrentSession.RunInstanceAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      // Guaranteed cleanup even on exceptions
      CurrentSession.Dispose();
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
      lastExitCode = await ProcessSingleCommandAsync(cancellationToken).ConfigureAwait(false);
    }

    return lastExitCode;
  }

  private async Task<int> ProcessSingleCommandAsync(CancellationToken cancellationToken)
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

    // Parse command
    string[] args = CommandLineParser.Parse(trimmedInput);
    if (args.Length == 0) return 0;

    // Handle REPL built-in commands directly (not through route matcher)
    if (HandleReplBuiltIn(args)) return 0;

    // Add to history (after built-in check, so "exit" etc. don't get added)
    History.Add(trimmedInput);

    // Execute through generated route matcher
    return await ExecuteCommandAsync(args, cancellationToken).ConfigureAwait(false);
  }

  /// <summary>
  /// Handles REPL built-in commands that are processed directly without going through the route matcher.
  /// </summary>
  /// <returns>True if the command was handled, false if it should be passed to the route matcher.</returns>
  private bool HandleReplBuiltIn(string[] args)
  {
    // Exit commands
    if (args is ["exit"] or ["quit"] or ["q"])
    {
      Running = false;
      return true;
    }

    // Clear screen
    if (args is ["clear"] or ["cls"])
    {
      Terminal.Clear();
      return true;
    }

    // Show history
    if (args is ["history"])
    {
      Commands.ShowHistory();
      return true;
    }

    // Clear history
    if (args is ["clear-history"])
    {
      Commands.ClearHistory();
      return true;
    }

    return false;
  }

  private string? ReadCommandInput()
  {
    if (ReplOptions.EnableArrowHistory)
    {
      ReplConsoleReader consoleReader = new(
        History.AsReadOnly,
        RouteProvider,
        ReplOptions,
        LoggerFactory,
        Terminal);
      return consoleReader.ReadLine(ReplOptions.Prompt);
    }

    Terminal.Write(PromptFormatter.Format(ReplOptions));
    return Terminal.ReadLine();
  }

  private async Task<int> ExecuteCommandAsync(string[] args, CancellationToken cancellationToken)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();
    try
    {
      int exitCode = await CommandExecutor(NuruCoreApp, args, cancellationToken).ConfigureAwait(false);
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
    Terminal.WriteLine(); // Move to new line after ^C
  }
}
