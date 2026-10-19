#region Purpose
// The CLI side of a served Nuru app: probe the socket, start the server when asked, forward calls.
#endregion

#region Design
// Probe-or-start: a reachable server from the same build takes the call. A server from another build is
// stopped (it would run old code); if it still answers after the stop, the call runs here in Auto mode
// and fails with --daemon or AutoStart. A server this client starts is used only once a ping reports
// this build, and the call itself carries the build id so the server refuses it otherwise. With no server, the call runs here unless --daemon or AutoStart asks
// to start one. A connect failure before the request is sent falls back to local in Auto mode; once the
// request is sent the call is never retried locally, because the handler may already have run.
// --json-args values are inlined so the server never reads this process's stdin or resolves @path
// itself, and the client's directory travels with the call.
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// Forwards a served app's routes to its running server.
/// </summary>
/// <remarks>
/// <c>AddServeRoute()</c> attaches an instance as <see cref="NuruApp.RouteForwarder"/>. The public
/// methods talk to a server directly, for tests and custom hosts.
/// </remarks>
public sealed class NuruDaemonClient : INuruRouteForwarder
{
  private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

  private static readonly TimeSpan PingTimeout = TimeSpan.FromSeconds(5);

  private readonly NuruDaemonOptions Options;

  /// <summary>
  /// Creates a client.
  /// </summary>
  /// <param name="options">Daemon options; null uses the defaults.</param>
  public NuruDaemonClient(NuruDaemonOptions? options = null)
  {
    Options = options?.Clone() ?? new NuruDaemonOptions();
  }

  /// <summary>The socket path this client uses for <paramref name="app"/>.</summary>
  /// <param name="app">The served app.</param>
  /// <returns>The absolute socket path.</returns>
  public string ResolveSocketPath(NuruApp app) => Options.ResolveSocketPath(app);

  /// <inheritdoc />
  public async Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(app);
    ArgumentNullException.ThrowIfNull(request);

    // The server process runs every route itself; never forward to ourselves.
    if (request.Mode == NuruForwardMode.Never || NuruDaemonServer.IsServing(app))
      return null;

    string socketPath = Options.ResolveSocketPath(app);
    bool mayStart = request.Mode == NuruForwardMode.Require || Options.AutoStart;
    if (NuruDaemonPaths.CheckSocketDirectory(socketPath) is { } unsafeDirectory)
      return request.Mode == NuruForwardMode.Require ? new NuruForwardResult(1, "", unsafeDirectory + "\n") : null;

    string buildId = NuruDaemonPaths.GetBuildId(app);
    NuruDaemonStatus? status = await PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
    if (status is not null && status.BuildId != buildId)
    {
      // It would run old code. Stop it, and use the socket only if nothing from that build answers.
      await StopAsync(socketPath, cancellationToken).ConfigureAwait(false);
      status = await PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
      if (status is not null && status.BuildId != buildId)
      {
        return mayStart
          ? new NuruForwardResult(1, "", $"Error: a {app.Capabilities.Name} server from a different build (pid {status.ProcessId.ToString(CultureInfo.InvariantCulture)}) still answers on {socketPath} and did not stop.\n")
          : null;
      }
    }

    if (status is null)
    {
      if (!mayStart)
        return null;

      string? failure = await NuruDaemonLauncher.StartAsync(app, Options, socketPath, buildId, cancellationToken).ConfigureAwait(false);
      if (failure is not null)
        return request.Mode == NuruForwardMode.Require ? new NuruForwardResult(1, "", failure + "\n") : null;
    }

    // The build id rides with the call too: the server refuses it if another build took the socket since.
    byte[] payload = request.Args is { } args
      ? NuruDaemonProtocol.EncodeRunRequest(InlineJsonArguments(args, request.JsonArguments), Environment.CurrentDirectory, buildId)
      : NuruDaemonProtocol.EncodeInvokeRequest(request.Route, request.JsonArguments ?? "{}", Environment.CurrentDirectory, buildId);

    Socket? socket = await TryConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
    if (socket is null)
    {
      return request.Mode == NuruForwardMode.Require
        ? new NuruForwardResult(1, "", $"Error: no {app.Capabilities.Name} server answers on {socketPath}.\n")
        : null;
    }

    try
    {
      string line = await ExchangeAsync(socket, payload, cancellationToken).ConfigureAwait(false);
      return NuruDaemonProtocol.DecodeResult(line);
    }
    catch (Exception exception) when (exception is NuruDaemonException or IOException or SocketException or JsonException)
    {
      return new NuruForwardResult(1, "", $"Error: the {app.Capabilities.Name} server failed the call: {exception.Message}\n");
    }
  }

  /// <summary>
  /// Asks the server for <paramref name="app"/> to report itself.
  /// </summary>
  /// <param name="app">The served app.</param>
  /// <param name="cancellationToken">Cancels the probe.</param>
  /// <returns>The server's status, or null when no server answers.</returns>
  public Task<NuruDaemonStatus?> PingAsync(NuruApp app, CancellationToken cancellationToken = default) =>
    PingAsync(Options.ResolveSocketPath(app), cancellationToken);

  /// <summary>
  /// Stops the server for <paramref name="app"/> and waits until its socket no longer answers.
  /// </summary>
  /// <param name="app">The served app.</param>
  /// <param name="cancellationToken">Cancels the request.</param>
  /// <returns>True when a server was running.</returns>
  public Task<bool> StopAsync(NuruApp app, CancellationToken cancellationToken = default) =>
    StopAsync(Options.ResolveSocketPath(app), cancellationToken);

  /// <summary>
  /// Runs CLI arguments in the server for <paramref name="app"/>, as a forwarded CLI call does.
  /// </summary>
  /// <param name="app">The served app.</param>
  /// <param name="args">The CLI arguments.</param>
  /// <param name="cancellationToken">Cancels the call; the server cancels the handler.</param>
  /// <returns>What the call printed and its exit code.</returns>
  /// <exception cref="NuruDaemonException">No server answers, or it failed the request.</exception>
  public async Task<NuruForwardResult> RunAsync(NuruApp app, IReadOnlyList<string> args, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(args);
    byte[] payload = NuruDaemonProtocol.EncodeRunRequest(args, Environment.CurrentDirectory);
    return NuruDaemonProtocol.DecodeResult(await SendAsync(app, payload, cancellationToken).ConfigureAwait(false));
  }

  /// <summary>
  /// Binds a JSON object to <paramref name="route"/> in the server with the public binder, invokes and
  /// renders it, as an MCP <c>tools/call</c> does.
  /// </summary>
  /// <param name="app">The served app.</param>
  /// <param name="route">The route pattern, as in <see cref="EndpointCapability.Pattern"/>.</param>
  /// <param name="arguments">A JSON object keyed by parameter and option names.</param>
  /// <param name="cancellationToken">Cancels the call.</param>
  /// <returns>
  /// The result. A bind error is exit code 1 with the <c>--json-args</c> message on standard error.
  /// </returns>
  /// <exception cref="NuruDaemonException">No server answers, or it failed the request.</exception>
  public async Task<NuruForwardResult> InvokeAsync(NuruApp app, string route, JsonElement arguments, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(route);
    string json = arguments.ValueKind == JsonValueKind.Undefined ? "{}" : arguments.GetRawText();
    byte[] payload = NuruDaemonProtocol.EncodeInvokeRequest(route, json, Environment.CurrentDirectory);
    return NuruDaemonProtocol.DecodeResult(await SendAsync(app, payload, cancellationToken).ConfigureAwait(false));
  }

  /// <summary>
  /// Replaces the value after <c>--json-args</c> with the resolved JSON object text.
  /// </summary>
  internal static string[] InlineJsonArguments(IReadOnlyList<string> args, string? json)
  {
    string[] result = [.. args];
    if (json is null)
      return result;

    for (int i = 0; i < result.Length - 1; i++)
    {
      if (result[i] == "--")
        break;

      if (result[i] == "--json-args")
      {
        result[i + 1] = json;
        break;
      }
    }

    return result;
  }

  internal static async Task<NuruDaemonStatus?> PingAsync(string socketPath, CancellationToken cancellationToken)
  {
    Socket? socket = await TryConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
    if (socket is null)
      return null;

    // A server answers ping outside its call gate; one that does not answer soon is not usable.
    using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    timeout.CancelAfter(PingTimeout);
    try
    {
      string line = await ExchangeAsync(socket, NuruDaemonProtocol.EncodeSimpleRequest(NuruDaemonOperations.Ping), timeout.Token).ConfigureAwait(false);
      return NuruDaemonProtocol.DecodeStatus(line);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
      return null;
    }
    catch (Exception exception) when (exception is NuruDaemonException or IOException or SocketException or JsonException)
    {
      return null;
    }
  }

  internal static async Task<bool> StopAsync(string socketPath, CancellationToken cancellationToken)
  {
    Socket? socket = await TryConnectAsync(socketPath, cancellationToken).ConfigureAwait(false);
    if (socket is null)
      return false;

    try
    {
      string line = await ExchangeAsync(socket, NuruDaemonProtocol.EncodeSimpleRequest(NuruDaemonOperations.Stop), cancellationToken).ConfigureAwait(false);
      NuruDaemonProtocol.DecodeOk(line);
    }
    catch (Exception exception) when (exception is NuruDaemonException or IOException or SocketException or JsonException)
    {
      // It stopped while answering, or it is not a server we understand; wait below either way.
    }

    // The server finishes its current call, then closes the socket.
    long deadline = Environment.TickCount64 + 10_000;
    while (Environment.TickCount64 < deadline)
    {
      if (await PingAsync(socketPath, cancellationToken).ConfigureAwait(false) is null)
        break;
      await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }

    return true;
  }

  private async Task<string> SendAsync(NuruApp app, byte[] payload, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(app);
    string socketPath = Options.ResolveSocketPath(app);
    Socket socket = await TryConnectAsync(socketPath, cancellationToken).ConfigureAwait(false)
      ?? throw new NuruDaemonException($"No {app.Capabilities.Name} server answers on {socketPath}.");
    return await ExchangeAsync(socket, payload, cancellationToken).ConfigureAwait(false);
  }

  private static async Task<Socket?> TryConnectAsync(string socketPath, CancellationToken cancellationToken)
  {
    // A socket in a directory other users can write may be theirs, not this user's server.
    if (!File.Exists(socketPath) || NuruDaemonPaths.CheckSocketDirectory(socketPath) is not null)
      return null;

    Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    try
    {
      await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath), cancellationToken).ConfigureAwait(false);
      return socket;
    }
    catch (SocketException)
    {
      socket.Dispose();
      return null;
    }
  }

  /// <summary>Sends one request line on <paramref name="socket"/> and reads the response line.</summary>
  private static async Task<string> ExchangeAsync(Socket socket, byte[] payload, CancellationToken cancellationToken)
  {
    NetworkStream stream = new(socket, ownsSocket: true);
    await using (stream.ConfigureAwait(false))
    {
      await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
      await stream.FlushAsync(cancellationToken).ConfigureAwait(false);

      using StreamReader reader = new(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
      return await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false)
        ?? throw new NuruDaemonException("The server closed the connection before it answered.");
    }
  }
}
