namespace TimeWarp.Nuru;

/// <summary>
/// Delegate for executing a command in the REPL loop.
/// The generated interceptor provides this to route commands through the generated matcher.
/// </summary>
/// <param name="app">The application instance.</param>
/// <param name="args">The parsed command-line arguments.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The exit code from command execution.</returns>
public delegate Task<int> ReplCommandExecutor(NuruApp app, string[] args, CancellationToken cancellationToken);

/// <summary>
/// Provides REPL (Read-Eval-Print Loop) mode for interactive command execution.
/// Implements IDisposable to ensure proper cleanup of resources (event handlers, history persistence).
/// </summary>
public sealed class ReplSession : IDisposable
{
  private readonly ILoggerFactory? LoggerFactory;
  private readonly NuruApp App;
  private readonly ReplOptions ReplOptions;
  private readonly ReplHistory History;
  private readonly IReplRouteProvider RouteProvider;
  private readonly ReplCommandExecutor CommandExecutor;
  private readonly ITerminal Terminal;
  private readonly ReplCommands Commands;
  private bool Running;
  private bool Disposed;

  // CTS for the command currently executing, linked to the external token. Published so
  // OnCancelKeyPress (raised on a console/threadpool thread) can cancel the in-flight
  // command; null whenever no command is executing. Volatile: written on the loop thread,
  // read on the Ctrl+C callback thread.
  // CA2213: not disposed from Dispose() by design — ownership is method-scoped (`using`)
  // in ExecuteCommandAsync; this field only publishes a reference and is nulled before
  // that disposal runs, so it never holds a CTS the session must clean up.
#pragma warning disable CA2213
  private volatile CancellationTokenSource? CurrentCommandCts;
#pragma warning restore CA2213

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
  /// <param name="nuruApp">The NuruApp instance.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="routeProvider">Provider for route information (completion, highlighting).</param>
  /// <param name="commandExecutor">Delegate to execute commands through generated route matcher.</param>
  /// <param name="loggerFactory">Logger factory for logging operations.</param>
  internal ReplSession(
    NuruApp nuruApp,
    ReplOptions replOptions,
    IReplRouteProvider routeProvider,
    ReplCommandExecutor commandExecutor,
    ILoggerFactory? loggerFactory)
  {
    App = nuruApp ?? throw new ArgumentNullException(nameof(nuruApp));
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
    Terminal.CancelKeyPress -= OnCancelKeyPress;

    if (ReplOptions.PersistHistory)
      History.Save();

    Disposed = true;
  }

  /// <summary>
  /// Runs a REPL session asynchronously.
  /// </summary>
  /// <param name="nuruApp">The NuruApp instance.</param>
  /// <param name="replOptions">Configuration for the REPL.</param>
  /// <param name="routeProvider">Provider for route information.</param>
  /// <param name="commandExecutor">Delegate to execute commands.</param>
  /// <param name="loggerFactory">Logger factory for logging operations.</param>
  /// <param name="cancellationToken">Token to cancel the REPL loop.</param>
  public static async Task RunAsync(
    NuruApp nuruApp,
    ReplOptions replOptions,
    IReplRouteProvider routeProvider,
    ReplCommandExecutor commandExecutor,
    ILoggerFactory? loggerFactory,
    CancellationToken cancellationToken = default)
  {
    // Run and dispose a LOCAL instance; CurrentSession is only a mirror for observability.
    // A concurrently-started session would otherwise clobber the static and cause this
    // finally to dispose the wrong instance.
    ReplSession session = new(nuruApp, replOptions, routeProvider, commandExecutor, loggerFactory);
    CurrentSession = session;

    try
    {
      await session.RunInstanceAsync(cancellationToken).ConfigureAwait(false);
    }
    finally
    {
      // Guaranteed cleanup even on exceptions
      session.Dispose();

      // Only clear the static if it still points at our session (don't null a newer one).
      if (ReferenceEquals(CurrentSession, session))
      {
        CurrentSession = null;
      }
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

    // Handle Ctrl+C gracefully
    Terminal.CancelKeyPress += OnCancelKeyPress;
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
    string? input = await ReadCommandInputAsync().ConfigureAwait(false);

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

  private async Task<string?> ReadCommandInputAsync()
  {
    if (ReplOptions.EnableArrowHistory)
    {
      ReplConsoleReader consoleReader = new(
        History.AsReadOnly,
        RouteProvider,
        ReplOptions,
        LoggerFactory,
        Terminal);
      return await consoleReader.ReadLineAsync(ReplOptions.Prompt).ConfigureAwait(false);
    }

    Terminal.Write(PromptFormatter.Format(ReplOptions));
    return Terminal.ReadLine();
  }

  private async Task<int> ExecuteCommandAsync(string[] args, CancellationToken cancellationToken)
  {
    Stopwatch stopwatch = Stopwatch.StartNew();

    // Per-command CTS linked to the external token so Ctrl+C can abort the in-flight
    // command (M15). Published to CurrentCommandCts for OnCancelKeyPress; cleared before
    // disposal so the callback never cancels a disposed CTS.
    using CancellationTokenSource commandCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    CurrentCommandCts = commandCts;

    try
    {
      int exitCode = await CommandExecutor(App, args, commandCts.Token).ConfigureAwait(false);
      stopwatch.Stop();

      DisplayCommandResult(exitCode, stopwatch.ElapsedMilliseconds, success: true);

      if (!ReplOptions.ContinueOnError && exitCode != 0)
      {
        Running = false;
      }

      return exitCode;
    }
    catch (OperationCanceledException)
    {
      // External cancellation (host shutdown) must still unwind RunAsync as before.
      if (cancellationToken.IsCancellationRequested)
      {
        throw;
      }

      // Ctrl+C on THIS command: report and return to the prompt. User-initiated
      // cancellation is not a command failure, so ContinueOnError does not apply.
      // 130 = 128 + SIGINT, the conventional Unix exit code for interrupt.
      stopwatch.Stop();
      string message = "Command cancelled";
      await Terminal.WriteErrorLineAsync(ReplOptions.EnableColors ? message.Yellow() : message).ConfigureAwait(false);
      return 130;
    }
#pragma warning disable CA1031 // Do not catch general exception types — the REPL is a command
                               // boundary; an unexpected exception thrown by a user command must
                               // not tear down the whole interactive session.
    catch (Exception ex)
    {
      return HandleCommandException(stopwatch, ex);
    }
#pragma warning restore CA1031
    finally
    {
      // Unpublish before the using-disposal runs so OnCancelKeyPress can never observe
      // (and Cancel) a disposed CTS.
      CurrentCommandCts = null;
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
      Terminal.WriteErrorLine(ReplOptions.EnableColors ? message.Red() : message);
    }
    else if (!ReplOptions.ContinueOnError && exitCode != 0)
    {
      string message = $"Command failed with exit code {exitCode}. Exiting REPL.";
      Terminal.WriteErrorLine(ReplOptions.EnableColors ? message.Red() : message);
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

    // Command in flight → cancel just that command; the REPL survives and re-prompts.
    // Idle prompt → newline only; Ctrl+C does NOT exit the REPL (use `exit` or Ctrl+D).
    // A second Ctrl+C while cancellation is in flight is a no-op (already cancelled).
    CancellationTokenSource? commandCts = CurrentCommandCts;
    if (commandCts is not null)
    {
      try
      {
        commandCts.Cancel();
      }
      catch (ObjectDisposedException)
      {
        // Benign race: the command completed and its CTS was disposed between our
        // snapshot and Cancel(). Nothing left to cancel.
      }
    }

    Terminal.WriteLine(); // Move to new line after ^C
  }
}
