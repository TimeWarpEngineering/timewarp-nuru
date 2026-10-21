#region Purpose
// Per-call state for route execution that runs concurrently in one process (a daemon or MCP host):
// the call's terminal, working directory and exit code.
#endregion

#region Design
// The context flows with the call through an AsyncLocal, so generated code, handlers and services read the
// current call without a parameter. NuruApp.Terminal, the DI ITerminal (NuruCallTerminal) and the generated
// exit-code reads and writes (CurrentExitCode) follow it. With no context (a plain CLI run, the REPL),
// everything falls back to process state exactly as before: the app's terminal, Environment.ExitCode and
// Environment.CurrentDirectory. Generated DI also keys its per-call service scope on the context being set.
// Console writes are not process-safe to swap per call; with CaptureConsole the context asks the console
// router (installed once, see NuruConsoleRouter) to send Console.Out/Error writes made in this call to the
// call's terminal. A call context is entered only by EnterAsync-style helpers on NuruApp, never by user code
// setting Current, so the previous context is always restored.
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// The terminal, working directory and exit code of one route call. Daemon and MCP hosts run each call
/// in its own context, so concurrent calls never share them.
/// </summary>
/// <remarks>
/// <para>
/// Read <see cref="Current"/> from a handler or service: it is the call that is running, or null in a
/// plain CLI run. Prefer the static helpers, which work in both cases: <see cref="CurrentDirectory"/>
/// and <see cref="GetFullPath"/> resolve paths against the caller's directory (a served call does not
/// change the process directory), and <see cref="CurrentExitCode"/> sets the exit code of this call
/// (in a plain CLI run, <see cref="Environment.ExitCode"/>).
/// </para>
/// <para>
/// Use <see cref="NuruApp.ExecuteAsync(string[], NuruCallContext, CancellationToken)"/> or
/// <see cref="NuruApp.InvokeAsync(NuruBoundRequest, NuruCallContext, CancellationToken)"/> to run a call in a
/// context you create.
/// </para>
/// </remarks>
public sealed class NuruCallContext
{
  private static readonly AsyncLocal<NuruCallContext?> CurrentContext = new();

  private readonly ConcurrentDictionary<object, object> States = new(ReferenceEqualityComparer.Instance);

  /// <summary>
  /// Creates a call context.
  /// </summary>
  /// <param name="terminal">Where the call writes: <see cref="NuruApp.Terminal"/> and an injected
  /// <see cref="ITerminal"/> return it while the call runs.</param>
  /// <param name="workingDirectory">
  /// The caller's directory. Null uses the current call's directory, else the process directory. A relative
  /// path is resolved against that.
  /// </param>
  public NuruCallContext(ITerminal terminal, string? workingDirectory = null)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    Terminal = terminal;
    WorkingDirectory = string.IsNullOrEmpty(workingDirectory)
      ? CurrentDirectory
      : Path.GetFullPath(workingDirectory, CurrentDirectory);
  }

  /// <summary>The call that is running, or null outside a call context (a plain CLI run).</summary>
  public static NuruCallContext? Current => CurrentContext.Value;

  /// <summary>Where the call writes.</summary>
  public ITerminal Terminal { get; }

  /// <summary>The caller's working directory (absolute). Relative paths in a call resolve against it.</summary>
  public string WorkingDirectory { get; }

  /// <summary>The call's exit code. Starts at 0; problem details and handlers set it.</summary>
  public int ExitCode { get; set; }

  /// <summary>
  /// When true, text a handler writes to <c>Console.Out</c> or <c>Console.Error</c> during the call goes
  /// to <see cref="Terminal"/>'s <see cref="IConsole.Out"/> and <see cref="IConsole.Error"/> instead of the
  /// process streams. Default false.
  /// </summary>
  public bool CaptureConsole { get; init; }

  /// <summary>
  /// The directory relative paths resolve against: the current call's <see cref="WorkingDirectory"/>, or
  /// <see cref="Environment.CurrentDirectory"/> outside a call.
  /// </summary>
  public static string CurrentDirectory => Current?.WorkingDirectory ?? Environment.CurrentDirectory;

  /// <summary>
  /// The exit code of the running call: the current context's <see cref="ExitCode"/>, or
  /// <see cref="Environment.ExitCode"/> outside a call. Set it from a handler to fail the call with a
  /// non-zero exit code; generated route code reads and writes it.
  /// </summary>
  public static int CurrentExitCode
  {
    get => Current is { } context ? context.ExitCode : Environment.ExitCode;
    set
    {
      if (Current is { } context)
        context.ExitCode = value;
      else
        Environment.ExitCode = value;
    }
  }

  /// <summary>Set while this call holds the app's exclusive gate (see <see cref="NuruExclusiveLease"/>).</summary>
  internal bool HoldsExclusive { get; set; }

  /// <summary>
  /// Resolves <paramref name="path"/> against <see cref="CurrentDirectory"/>: the caller's directory in a
  /// served call, the process directory otherwise.
  /// </summary>
  /// <param name="path">An absolute or relative path.</param>
  /// <returns>The absolute path.</returns>
  public static string GetFullPath(string path)
  {
    ArgumentNullException.ThrowIfNull(path);
    return Path.GetFullPath(path, CurrentDirectory);
  }

  /// <summary>
  /// A context for a nested call of the current one (same terminal, directory and console capture),
  /// or a context on <paramref name="fallbackTerminal"/> outside a call.
  /// </summary>
  internal static NuruCallContext ForNestedCall(ITerminal fallbackTerminal) =>
    Current is { } current
      ? new NuruCallContext(current.Terminal, current.WorkingDirectory) { CaptureConsole = current.CaptureConsole, HoldsExclusive = current.HoldsExclusive }
      : new NuruCallContext(fallbackTerminal);

  /// <summary>
  /// Per-call state another component keeps for this call (generated DI, the command and session caches).
  /// </summary>
  internal T GetState<T>(object owner, Func<T> create)
    where T : class =>
    (T)States.GetOrAdd(owner, static (_, factory) => factory(), create);

  /// <summary>
  /// Runs <paramref name="run"/> with this context as <see cref="Current"/>, then restores the previous one.
  /// </summary>
  internal async Task<T> RunAsync<T>(Func<Task<T>> run)
  {
    if (CaptureConsole)
      NuruConsoleRouter.EnsureInstalled();

    // An async method restores the caller's ExecutionContext on return, so Current reverts by itself.
    // Returning run() without await would leak this context to the caller.
    CurrentContext.Value = this;
#pragma warning disable RCS1174 // The await is what scopes the AsyncLocal to this call; see above.
    return await run().ConfigureAwait(false);
#pragma warning restore RCS1174
  }
}
