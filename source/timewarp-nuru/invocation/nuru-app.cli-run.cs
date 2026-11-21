#region Purpose
// The CLI RunAsync boundary: cancel on Ctrl+C and SIGTERM, and turn a handler exception into exit code 1.
#endregion

#region Design
// Generated RunAsync_Intercepted calls RunCliAsync around ExecuteRouteAsync. ExecuteAsync and
// InvokeAsync do not, so a daemon or MCP host still sees handler exceptions.
// Ctrl+C is Posix SIGINT, and Terminal.CancelKeyPress for any terminal but the console (TestTerminal
// raises it). Ctrl+Break (SIGQUIT) is a Ctrl+C. SIGTERM is the same wait with exit code 143. One
// signal arms one timeout; a second signal does not restart it. The arm flag collapses a signal that
// arrives twice. The console terminal (TimeWarpTerminal) forwards CancelKeyPress to
// Console.CancelKeyPress, whose first subscription initializes the console: about 5 ms on a cold
// Linux start, more than the rest of a run (task 501). Console.CancelKeyPress is SIGINT and SIGQUIT,
// so the Posix registrations already cover it; it is used only when they fail. The Posix registrations
// are process-wide: created by the first CLI run and kept, so a run does not pay to create and dispose
// three of them (about 2 µs per warm run, task 501). A signal goes to every run in progress. With none
// in progress nothing sets Cancel and the default action runs, as it did with no registration. The wait is Task.Delay, not a wait
// inside the signal handler. Null ProcessTerminationTimeout skips registration and passes
// CancellationToken.None. The exception handler stays on.
// While a REPL session owns the signal, this registration does not cancel and does not set
// Cancel, so the session's own handler still runs and the session stays up.
// OptionsValidationException is rethrown: it is thrown while options are created, before the
// handler, and the caller is the place that sees a bad configuration. Handler failures are read
// from the task, not caught as Exception, so the process boundary does not need a CA1031 suppression.
// When the timeout wins, the handler task is observed so a later exception is not unobserved, and
// its cancellation source stays alive because the handler may still read the token.
#endregion

namespace TimeWarp.Nuru;

using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

public partial class NuruApp
{
  private const int SigintExitCode = 130;
  private const int SigtermExitCode = 143;

  // Process-wide Posix registrations and the runs they signal (see Design).
  private static readonly Lock SignalGate = new();
  private static Func<int, bool>[] SignalRuns = [];
  private static bool SignalsRegistered;
  private static PosixSignalRegistration? InterruptRegistration;
  private static PosixSignalRegistration? QuitRegistration;
  private static PosixSignalRegistration? TerminateRegistration;

  /// <summary>
  /// Runs one CLI <see cref="RunAsync"/> call: signal handling, then <paramref name="execute"/>.
  /// Called by generated code.
  /// </summary>
  /// <param name="app">The app being run.</param>
  /// <param name="args">Command-line arguments.</param>
  /// <param name="execute">The generated route executor. It receives the CLI cancellation token.</param>
  /// <returns>The exit code.</returns>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static Task<int> RunCliAsync(
    NuruApp app,
    string[] args,
    Func<NuruApp, string[], CancellationToken, Task<int>> execute)
  {
    ArgumentNullException.ThrowIfNull(app);
    ArgumentNullException.ThrowIfNull(args);
    ArgumentNullException.ThrowIfNull(execute);

    TimeSpan? timeout = app.ProcessTerminationTimeout;
    if (timeout is null)
    {
      return ObserveResultAsync(
        app,
        execute(app, args, CancellationToken.None),
        static () => false,
        static () => 0);
    }

    if (timeout.Value < TimeSpan.Zero)
    {
      throw new ArgumentOutOfRangeException(
        nameof(app),
        timeout.Value,
        "ProcessTerminationTimeout cannot be negative.");
    }

    return RunWithSignalsAsync(app, args, execute, timeout.Value);
  }

  private static async Task<int> RunWithSignalsAsync(
    NuruApp app,
    string[] args,
    Func<NuruApp, string[], CancellationToken, Task<int>> execute,
    TimeSpan timeout)
  {
    CancellationTokenSource? runCancellation = new();
    CancellationTokenSource delayCancellation = new();
    CancellationTokenSource signalSource = runCancellation;
    TaskCompletionSource<int> forced = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int armed = 0;
    int signalCode = SigintExitCode;

    bool TryArm(int code)
    {
      if (app.ReplOwnsCancellation)
        return false;

      if (Interlocked.Exchange(ref armed, 1) != 0)
        return true;

      Volatile.Write(ref signalCode, code);
      try
      {
        signalSource.Cancel();
      }
      catch (ObjectDisposedException)
      {
        // The run already finished.
      }
      catch (AggregateException)
      {
        // A cancellation callback threw. The token is cancelled.
      }

      _ = CompleteAfterTimeoutAsync(timeout, code, forced, delayCancellation.Token);
      return true;
    }

    void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs eventArgs)
    {
      if (TryArm(SigintExitCode))
        eventArgs.Cancel = true;
    }

    Func<int, bool> arm = TryArm;
    Task<int>? run = null;
    ITerminal terminal = app.Terminal;
    bool keyPressSubscribed = false;
    try
    {
      bool ctrlCSignalled = EnterSignalScope(arm);
      if (terminal is not TimeWarpTerminal || !ctrlCSignalled)
      {
        terminal.CancelKeyPress += OnCancelKeyPress;
        keyPressSubscribed = true;
      }

      run = execute(app, args, signalSource.Token);
      Task<int> winner = await Task.WhenAny(run, forced.Task).ConfigureAwait(false);
      if (!ReferenceEquals(winner, run))
      {
        // The handler is still reading the token, so the source lives until the run finishes.
        ReleaseWhenComplete(run, signalSource);
        runCancellation = null;
        return await forced.Task.ConfigureAwait(false);
      }

      return await ObserveResultAsync(
        app,
        run,
        () => signalSource.IsCancellationRequested,
        () => Volatile.Read(ref signalCode)).ConfigureAwait(false);
    }
    finally
    {
      if (keyPressSubscribed)
        terminal.CancelKeyPress -= OnCancelKeyPress;

      ExitSignalScope(arm);
      await delayCancellation.CancelAsync().ConfigureAwait(false);
      delayCancellation.Dispose();
      runCancellation?.Dispose();
    }
  }

  private static async Task CompleteAfterTimeoutAsync(
    TimeSpan timeout,
    int code,
    TaskCompletionSource<int> forced,
    CancellationToken delayToken)
  {
    try
    {
      await Task.Delay(timeout, delayToken).ConfigureAwait(false);
      forced.TrySetResult(code);
    }
    catch (OperationCanceledException)
    {
      // The run finished and cancelled the delay.
    }
    catch (ObjectDisposedException)
    {
      // The delay source was disposed as the run returned.
    }
  }

  /// <summary>
  /// Adds a run to the signalled runs, registering the process-wide Posix handlers on first use.
  /// </summary>
  /// <returns>True when SIGINT and SIGQUIT are registered, so Ctrl+C and Ctrl+Break reach the run.</returns>
  private static bool EnterSignalScope(Func<int, bool> arm)
  {
    lock (SignalGate)
    {
      if (!SignalsRegistered)
      {
        SignalsRegistered = true;
        InterruptRegistration = TryRegister(PosixSignal.SIGINT, SigintExitCode);
        QuitRegistration = TryRegister(PosixSignal.SIGQUIT, SigintExitCode);
        TerminateRegistration = TryRegister(PosixSignal.SIGTERM, SigtermExitCode);
      }

      Func<int, bool>[] runs = new Func<int, bool>[SignalRuns.Length + 1];
      SignalRuns.CopyTo(runs, 0);
      runs[^1] = arm;
      Volatile.Write(ref SignalRuns, runs);
      return InterruptRegistration is not null && QuitRegistration is not null;
    }
  }

  /// <summary>Removes a run added by <see cref="EnterSignalScope"/>.</summary>
  private static void ExitSignalScope(Func<int, bool> arm)
  {
    lock (SignalGate)
    {
      int index = Array.IndexOf(SignalRuns, arm);
      if (index < 0)
        return;

      Func<int, bool>[] runs = new Func<int, bool>[SignalRuns.Length - 1];
      Array.Copy(SignalRuns, 0, runs, 0, index);
      Array.Copy(SignalRuns, index + 1, runs, index, runs.Length - index);
      Volatile.Write(ref SignalRuns, runs);
    }
  }

  private static PosixSignalRegistration? TryRegister(PosixSignal signal, int exitCode)
  {
    try
    {
      return PosixSignalRegistration.Create(signal, context =>
      {
        // Every run in progress arms; any that takes the signal cancels the default action.
        bool cancel = false;
        foreach (Func<int, bool> arm in Volatile.Read(ref SignalRuns))
          cancel |= arm(exitCode);

        if (cancel)
          context.Cancel = true;
      });
    }
    catch (PlatformNotSupportedException)
    {
      return null;
    }
  }

  private static async Task<int> ObserveResultAsync(
    NuruApp app,
    Task<int> run,
    Func<bool> signalled,
    Func<int> signalCode)
  {
    // WhenAny completes without throwing, so a handler exception stays on the task.
    await Task.WhenAny(run).ConfigureAwait(false);

    if (run.IsCompletedSuccessfully)
      return run.GetAwaiter().GetResult();

    if (run.IsCanceled)
    {
      if (signalled())
        return signalCode();

      return await ReportCancellationAsync(app, run).ConfigureAwait(false);
    }

    Exception failure = UnwrapHandlerFailure(run.Exception);
    if (failure is OptionsValidationException)
      ExceptionDispatchInfo.Capture(failure).Throw();

    await ReportHandlerFailureAsync(app, failure).ConfigureAwait(false);
    return 1;
  }

  private static async Task<int> ReportCancellationAsync(NuruApp app, Task<int> run)
  {
    try
    {
      return run.GetAwaiter().GetResult();
    }
    catch (OperationCanceledException exception)
    {
      await ReportHandlerFailureAsync(app, exception).ConfigureAwait(false);
      return 1;
    }
  }

  private static Exception UnwrapHandlerFailure(AggregateException? aggregate)
  {
    if (aggregate is null)
      return new InvalidOperationException("The handler failed without an exception.");

    foreach (Exception inner in aggregate.Flatten().InnerExceptions)
    {
      if (inner is OptionsValidationException)
        return inner;
    }

    return aggregate.GetBaseException();
  }

  private static Task ReportHandlerFailureAsync(NuruApp app, Exception exception)
  {
    string message = exception.Message;
    if (string.IsNullOrEmpty(message))
      message = exception.GetType().Name;

    return app.Terminal.WriteErrorLineAsync(message);
  }

  private static void ReleaseWhenComplete(Task run, CancellationTokenSource runCancellation)
  {
    _ = run.ContinueWith(
      static (completed, state) =>
      {
        _ = completed.Exception;
        ((CancellationTokenSource)state!).Dispose();
      },
      runCancellation,
      CancellationToken.None,
      TaskContinuationOptions.ExecuteSynchronously,
      TaskScheduler.Default);
  }
}
