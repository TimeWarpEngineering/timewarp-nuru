#region Purpose
// Core contract for forwarding a matched route to another process (a served app, TimeWarp.Nuru.Daemon).
#endregion

#region Design
// Core knows only the contract, so generated code can call it without referencing the daemon package.
// Generated route code calls NuruForwarding.TryForwardAsync after argv matching and type conversion and
// before behaviors and the handler: parse errors stay local, and behaviors run once, where the handler runs.
// A null result means "run here"; the forwarder decides (no server, local mode, or this app is the server).
#endregion

namespace TimeWarp.Nuru;

/// <summary>
/// How a CLI call chooses between a running server and in-process execution.
/// </summary>
public enum NuruForwardMode
{
  /// <summary>Forward when a server is reachable (or the forwarder may start one); otherwise run here.</summary>
  Auto = 0,

  /// <summary><c>--daemon</c>: forward, starting the server if it is not running; fail when that is not possible.</summary>
  Require = 1,

  /// <summary><c>--no-daemon</c>: always run in this process.</summary>
  Never = 2
}

/// <summary>
/// Forwards a matched route to another process and returns what it printed.
/// </summary>
/// <remarks>
/// <see cref="NuruApp.RouteForwarder"/> holds the instance. TimeWarp.Nuru.Daemon implements it with a
/// local socket client; <c>AddServeRoute()</c> attaches it.
/// </remarks>
public interface INuruRouteForwarder
{
  /// <summary>
  /// Runs <paramref name="request"/> in the server, or returns null so the caller runs it in this process.
  /// </summary>
  /// <param name="app">The app whose route matched.</param>
  /// <param name="request">What to run.</param>
  /// <param name="cancellationToken">Cancels the forwarded call.</param>
  /// <returns>The server's output and exit code, or null to run locally.</returns>
  Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken);
}

/// <summary>
/// One route call to forward: either the CLI arguments (<see cref="Args"/>) or a JSON object for the
/// public binder (<see cref="Arguments"/>).
/// </summary>
public sealed class NuruForwardRequest
{
  private NuruForwardRequest(string route, IReadOnlyList<string>? args, string? jsonArguments, NuruForwardMode mode)
  {
    Route = route;
    Args = args;
    JsonArguments = jsonArguments;
    Mode = mode;
  }

  /// <summary>The matched route pattern, as in <see cref="EndpointCapability.Pattern"/>.</summary>
  public string Route { get; }

  /// <summary>
  /// The CLI arguments for an argv call, or null for a binder call. The server matches them with the
  /// same generated code, so parsing and conversion are identical.
  /// </summary>
  public IReadOnlyList<string>? Args { get; }

  /// <summary>
  /// The JSON object text: the binder arguments of a binder call, or the resolved <c>--json-args</c>
  /// value of an argv call (stdin and <c>@path</c> already read), or null.
  /// </summary>
  public string? JsonArguments { get; }

  /// <summary>How the caller chose between the server and this process.</summary>
  public NuruForwardMode Mode { get; }

  /// <summary>
  /// Creates an argv request. Called by generated code.
  /// </summary>
  /// <param name="route">The matched route pattern.</param>
  /// <param name="args">The CLI arguments, with <c>--daemon</c> and <c>--no-daemon</c> removed.</param>
  /// <param name="jsonArguments">The resolved <c>--json-args</c> object text, or null.</param>
  /// <param name="mode">The caller's mode.</param>
  /// <returns>The request.</returns>
  public static NuruForwardRequest ForArgs(string route, IReadOnlyList<string> args, string? jsonArguments, NuruForwardMode mode)
  {
    ArgumentNullException.ThrowIfNull(route);
    ArgumentNullException.ThrowIfNull(args);
    return new NuruForwardRequest(route, [.. args], jsonArguments, mode);
  }

  /// <summary>
  /// Creates a binder request: the server binds <paramref name="arguments"/> with
  /// <see cref="NuruApp.TryBind"/>, invokes and renders, as an MCP <c>tools/call</c> does.
  /// </summary>
  /// <param name="route">The route pattern.</param>
  /// <param name="arguments">A JSON object keyed by parameter and option names.</param>
  /// <param name="mode">The caller's mode.</param>
  /// <returns>The request.</returns>
  public static NuruForwardRequest ForInvoke(string route, JsonElement arguments, NuruForwardMode mode = NuruForwardMode.Auto)
  {
    ArgumentNullException.ThrowIfNull(route);
    string json = arguments.ValueKind == JsonValueKind.Undefined ? "{}" : arguments.GetRawText();
    return new NuruForwardRequest(route, null, json, mode);
  }
}

/// <summary>
/// What a forwarded call printed and returned.
/// </summary>
/// <param name="ExitCode">The exit code the CLI returns for the call.</param>
/// <param name="Output">Standard output: console text, then (for an argv call) everything the route printed.</param>
/// <param name="ErrorOutput">Standard error.</param>
/// <param name="RenderedOutput">
/// For a binder call, the text <see cref="NuruApp.RenderAsync"/> wrote for the result, kept apart from
/// console text; empty for an argv call.
/// </param>
public sealed record NuruForwardResult(int ExitCode, string Output, string ErrorOutput, string RenderedOutput = "");

/// <summary>
/// Forwarding helpers that generated code calls.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class NuruForwarding
{
  /// <summary>Forces forwarding (start the server when it is not running).</summary>
  public const string DaemonFlag = "--daemon";

  /// <summary>Forces in-process execution.</summary>
  public const string NoDaemonFlag = "--no-daemon";

  /// <summary>
  /// Removes <c>--daemon</c> and <c>--no-daemon</c> before any <c>--</c> separator and reports the mode.
  /// </summary>
  /// <param name="args">The CLI arguments.</param>
  /// <param name="mode">Never when <c>--no-daemon</c> is present, Require for <c>--daemon</c>, otherwise Auto.</param>
  /// <returns>The arguments without the flags (the same array when neither is present).</returns>
  public static string[] PeelFlags(string[] args, out NuruForwardMode mode)
  {
    ArgumentNullException.ThrowIfNull(args);
    mode = NuruForwardMode.Auto;
    int separator = Array.IndexOf(args, "--");
    if (separator < 0)
      separator = args.Length;

    List<string>? kept = null;
    for (int i = 0; i < args.Length; i++)
    {
      string arg = args[i];
      bool isFlag = i < separator && (arg == DaemonFlag || arg == NoDaemonFlag);
      if (isFlag)
      {
        kept ??= [.. args[..i]];
        if (arg == NoDaemonFlag)
          mode = NuruForwardMode.Never;
        else if (mode != NuruForwardMode.Never)
          mode = NuruForwardMode.Require;
        continue;
      }

      kept?.Add(arg);
    }

    return kept is null ? args : [.. kept];
  }

  /// <summary>
  /// Forwards an argv call through <see cref="NuruApp.RouteForwarder"/> and writes what the server printed
  /// to <see cref="NuruApp.Terminal"/>.
  /// </summary>
  /// <param name="app">The app whose route matched.</param>
  /// <param name="route">The matched route pattern.</param>
  /// <param name="args">The CLI arguments, with the forwarding flags removed.</param>
  /// <param name="jsonArguments">The resolved <c>--json-args</c> object text, or null.</param>
  /// <param name="mode">The caller's mode.</param>
  /// <param name="cancellationToken">Cancels the call.</param>
  /// <returns>The server's exit code, or null when the route runs in this process.</returns>
  public static async Task<int?> TryForwardAsync(
    NuruApp app,
    string route,
    string[] args,
    string? jsonArguments,
    NuruForwardMode mode,
    CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(app);
    if (app.RouteForwarder is not INuruRouteForwarder forwarder || mode == NuruForwardMode.Never)
      return null;

    NuruForwardResult? result = await forwarder
      .ForwardAsync(app, NuruForwardRequest.ForArgs(route, args, jsonArguments, mode), cancellationToken)
      .ConfigureAwait(false);
    if (result is null)
      return null;

    WriteResult(app.Terminal, result);
    return result.ExitCode;
  }

  /// <summary>
  /// Writes a forwarded result to <paramref name="terminal"/>: standard output as is, then each
  /// standard error line.
  /// </summary>
  /// <param name="terminal">The terminal to write to.</param>
  /// <param name="result">The forwarded result.</param>
  public static void WriteResult(ITerminal terminal, NuruForwardResult result)
  {
    ArgumentNullException.ThrowIfNull(terminal);
    ArgumentNullException.ThrowIfNull(result);

    if (result.Output.Length > 0)
      terminal.Write(result.Output);
    if (result.RenderedOutput.Length > 0)
      terminal.Write(result.RenderedOutput);

    string error = result.ErrorOutput.TrimEnd('\r', '\n');
    if (error.Length == 0)
      return;

    foreach (string line in error.Split('\n'))
      terminal.WriteErrorLine(line.TrimEnd('\r'));
  }
}
