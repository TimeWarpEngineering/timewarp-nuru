#region Purpose
// The serve side of a served Nuru app: listen on a per-user Unix domain socket and run forwarded calls.
#endregion

#region Design
// Calls run one at a time in the process: generated route execution uses process-wide service state,
// Environment.ExitCode and the current directory, and console capture swaps Console.Out/Error (same rule
// as McpHost). ping and stop are answered outside that gate, so status works while a call runs. A call is
// cancelled when its client disconnects.
//
// One server per socket: a server holds an exclusive lock on <socket>.lock from before its stale-socket
// check until after it deletes its socket. Only the lock holder deletes or binds the socket path, so a
// second start (or a transient connect failure) can never unlink a live server's socket; a crashed
// server's lock is released by the OS, and the next server deletes its socket file. The build id is
// fixed when the server starts (a rebuild while it runs must not make it claim the new build), and a
// run or invoke that names another build is refused. The idle timer counts from the last call to finish and never
// fires while a call runs. The served app is marked so its own forwarder declines (no loop): generated
// code in this process runs every route locally.
#endregion

namespace TimeWarp.Nuru.Daemon;

/// <summary>
/// Serves a Nuru app's routes on a local socket for its forwarding CLI and MCP host.
/// </summary>
/// <remarks>
/// <see cref="RunAsync"/> is what the built-in <c>serve</c> route runs (<c>AddServeRoute()</c>).
/// </remarks>
public static class NuruDaemonServer
{
  /// <summary>
  /// Set to <c>1</c> by a client that starts the server in the background. The server then writes
  /// nothing to the inherited standard streams.
  /// </summary>
  public const string DetachedEnvironmentVariable = "NURU_DAEMON_DETACHED";

  /// <summary>
  /// The absolute socket path a client that starts the server resolved. It wins over the options, so the
  /// server listens where the client probes.
  /// </summary>
  internal const string LaunchSocketEnvironmentVariable = "NURU_DAEMON_LAUNCH_SOCKET";

  private static readonly ConditionalWeakTable<NuruApp, object> ServingApps = [];

  // One forwarded call at a time in the process, across apps (see Design).
  private static readonly SemaphoreSlim CallGate = new(1, 1);

  private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

  /// <summary>
  /// True while <paramref name="app"/> is being served in this process. Its forwarder then runs every
  /// route locally.
  /// </summary>
  /// <param name="app">The app.</param>
  /// <returns>True while <see cref="RunAsync"/> serves the app.</returns>
  public static bool IsServing(NuruApp app)
  {
    ArgumentNullException.ThrowIfNull(app);
    return ServingApps.TryGetValue(app, out _);
  }

  /// <summary>
  /// Serves <paramref name="app"/> until <c>serve --stop</c>, the idle timeout, Ctrl+C or SIGTERM, or
  /// <paramref name="cancellationToken"/>.
  /// </summary>
  /// <param name="app">The app to serve.</param>
  /// <param name="options">Daemon options; null uses the defaults.</param>
  /// <param name="cancellationToken">Stops the server.</param>
  /// <returns>0 after a normal stop; 1 when another server already answers on the socket.</returns>
  public static async Task<int> RunAsync(NuruApp app, NuruDaemonOptions? options = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);
    NuruDaemonOptions daemonOptions = options?.Clone() ?? new NuruDaemonOptions();
    string socketPath = TakeLaunchedSocketPath() ?? daemonOptions.ResolveSocketPath(app);
    bool detached = Environment.GetEnvironmentVariable(DetachedEnvironmentVariable) == "1";
    if (detached)
      Environment.SetEnvironmentVariable(DetachedEnvironmentVariable, null); // Not for the handlers' child processes.
    string name = app.Capabilities.Name;

    try
    {
      NuruDaemonPaths.EnsureSocketDirectory(socketPath);
    }
    catch (Exception exception) when (exception is NuruDaemonException or IOException or UnauthorizedAccessException)
    {
      await app.Terminal.WriteErrorLineAsync(exception.Message.StartsWith("Error:", StringComparison.Ordinal) ? exception.Message : "Error: " + exception.Message).ConfigureAwait(false);
      return 1;
    }

    // The lock is held for the server's whole life, so the stale check, the delete, and the bind below
    // are never interleaved with another server's start or shutdown (see Design).
    (FileStream? serverLock, string? lockFailure) = await AcquireServerLockAsync(app, socketPath, daemonOptions.StartTimeout, cancellationToken).ConfigureAwait(false);
    if (serverLock is null)
    {
      await app.Terminal.WriteErrorLineAsync(lockFailure).ConfigureAwait(false);
      return 1;
    }

    using (serverLock)
    {
      // A server answers only while it holds the lock, unless it predates the lock.
      if (await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false) is NuruDaemonStatus running)
      {
        await app.Terminal.WriteErrorLineAsync(AlreadyRunning(name, socketPath, running)).ConfigureAwait(false);
        return 1;
      }

      if (File.Exists(socketPath))
        File.Delete(socketPath); // Left by a server that did not stop cleanly; it no longer holds the lock.

      using Socket listener = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
      try
      {
        listener.Bind(new UnixDomainSocketEndPoint(socketPath));
      }
      catch (SocketException exception) when (exception.SocketErrorCode == SocketError.AddressAlreadyInUse)
      {
        NuruDaemonStatus? other = await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
        await app.Terminal.WriteErrorLineAsync(other is not null
          ? AlreadyRunning(name, socketPath, other)
          : $"Error: another process took {socketPath} while this {name} server was starting.").ConfigureAwait(false);
        return 1;
      }

      bool bound = false;
      try
      {
        NuruDaemonPaths.RestrictSocketFile(socketPath);
        listener.Listen(16);
        bound = true;
      }
      finally
      {
        if (!bound)
          TryDelete(socketPath);
      }

      (TextWriter Out, TextWriter Error)? previous = detached ? ConsoleRedirect.Redirect(TextWriter.Null, TextWriter.Null) : null;
      using CancellationTokenSource stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      using PosixSignalRegistration? interrupt = TryRegister(PosixSignal.SIGINT, stopping);
      using PosixSignalRegistration? terminate = TryRegister(PosixSignal.SIGTERM, stopping);
      ServingApps.AddOrUpdate(app, new object());
      try
      {
        if (!detached)
        {
          string idle = daemonOptions.IdleTimeout > TimeSpan.Zero
            ? $"stops after {daemonOptions.IdleTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture)} s idle"
            : "no idle timeout";
          await app.Terminal.WriteErrorLineAsync($"Serving {name} on {socketPath} ({idle}). Ctrl+C or '{string.Join(' ', daemonOptions.ServeArguments)} --stop' stops it.").ConfigureAwait(false);
        }

        Session session = new(app, daemonOptions, socketPath, NuruDaemonPaths.GetBuildId(app), listener, stopping);
        await session.RunAsync().ConfigureAwait(false);
      }
      finally
      {
        ServingApps.Remove(app);
        TryDelete(socketPath); // Still under the lock, so it is this server's socket.
        if (previous is { } writers)
          ConsoleRedirect.Restore(writers);
      }
    }

    return 0;
  }

  /// <summary>
  /// Prints whether a server for <paramref name="app"/> is running (<c>serve --status</c>).
  /// </summary>
  /// <param name="app">The app.</param>
  /// <param name="options">Daemon options; null uses the defaults.</param>
  /// <param name="cancellationToken">Cancels the probe.</param>
  /// <returns>0 when a server answers; 1 when none does.</returns>
  public static async Task<int> StatusAsync(NuruApp app, NuruDaemonOptions? options = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);
    NuruDaemonClient client = new(options);
    string socketPath = client.ResolveSocketPath(app);
    string name = app.Capabilities.Name;
    if (NuruDaemonPaths.CheckSocketDirectory(socketPath) is { } unsafeDirectory)
    {
      await app.Terminal.WriteErrorLineAsync(unsafeDirectory).ConfigureAwait(false);
      return 1;
    }

    NuruDaemonStatus? status = await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false);
    if (status is null)
    {
      await app.Terminal.WriteLineAsync($"{name} server is not running ({socketPath}).").ConfigureAwait(false);
      return 1;
    }

    string build = status.BuildId == NuruDaemonPaths.GetBuildId(app) ? "" : ", from a different build of the app";
    await app.Terminal.WriteLineAsync(
      $"{name} server is running: pid {status.ProcessId.ToString(CultureInfo.InvariantCulture)}, version {status.Version}, socket {status.SocketPath}{build}.").ConfigureAwait(false);
    return 0;
  }

  /// <summary>
  /// Stops a running server for <paramref name="app"/> (<c>serve --stop</c>).
  /// </summary>
  /// <param name="app">The app.</param>
  /// <param name="options">Daemon options; null uses the defaults.</param>
  /// <param name="cancellationToken">Cancels the request.</param>
  /// <returns>0 whether or not a server was running.</returns>
  public static async Task<int> StopAsync(NuruApp app, NuruDaemonOptions? options = null, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(app);
    NuruDaemonClient client = new(options);
    string name = app.Capabilities.Name;
    bool stopped = await client.StopAsync(app, cancellationToken).ConfigureAwait(false);
    await app.Terminal.WriteLineAsync(stopped ? $"{name} server stopped." : $"{name} server is not running.").ConfigureAwait(false);
    return 0;
  }

  /// <summary>
  /// Reads (and clears, so a handler's child processes do not inherit it) the absolute socket path a
  /// starting client resolved for this server.
  /// </summary>
  private static string? TakeLaunchedSocketPath()
  {
    string? launched = Environment.GetEnvironmentVariable(LaunchSocketEnvironmentVariable);
    if (launched is null)
      return null;

    Environment.SetEnvironmentVariable(LaunchSocketEnvironmentVariable, null);
    return Path.IsPathFullyQualified(launched) ? launched : null;
  }

  /// <summary>
  /// Takes the exclusive lock beside the socket. While another server holds it, gives up as soon as that
  /// server answers, or after <paramref name="timeout"/> (it may be finishing a call before it exits).
  /// </summary>
  private static async Task<(FileStream? Lock, string? Failure)> AcquireServerLockAsync(
    NuruApp app, string socketPath, TimeSpan timeout, CancellationToken cancellationToken)
  {
    string lockPath = socketPath + ".lock";
    string name = app.Capabilities.Name;
    long deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
    while (true)
    {
      try
      {
        // FileShare.None is an exclusive advisory lock (flock) on Unix; the OS drops it when the process dies.
        return (new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None), null);
      }
      catch (UnauthorizedAccessException exception)
      {
        return (null, $"Error: cannot open the {name} server lock {lockPath}: {exception.Message}");
      }
      catch (IOException)
      {
        // Another server holds it.
      }

      if (await NuruDaemonClient.PingAsync(socketPath, cancellationToken).ConfigureAwait(false) is NuruDaemonStatus running)
        return (null, AlreadyRunning(name, socketPath, running));

      if (Environment.TickCount64 >= deadline)
        return (null, $"Error: another {name} server holds {lockPath} and does not answer on {socketPath}.");

      await Task.Delay(50, cancellationToken).ConfigureAwait(false);
    }
  }

  private static string AlreadyRunning(string name, string socketPath, NuruDaemonStatus running) =>
    $"Error: a {name} server is already running on {socketPath} (pid {running.ProcessId.ToString(CultureInfo.InvariantCulture)}).";

  private static PosixSignalRegistration? TryRegister(PosixSignal signal, CancellationTokenSource stopping)
  {
    try
    {
      return PosixSignalRegistration.Create(signal, context =>
      {
        context.Cancel = true;
        stopping.Cancel();
      });
    }
    catch (PlatformNotSupportedException)
    {
      return null;
    }
  }

  private static void TryDelete(string path)
  {
    try
    {
      File.Delete(path);
    }
    catch (IOException)
    {
      // Best effort: the next server deletes a stale socket file.
    }
    catch (UnauthorizedAccessException)
    {
      // Same.
    }
  }

  /// <summary>One run of the accept loop and the idle timer.</summary>
  private sealed class Session(NuruApp app, NuruDaemonOptions options, string socketPath, string buildId, Socket listener, CancellationTokenSource stopping)
  {
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Task, byte> Handlers = new();
    private long LastActivity = Environment.TickCount64;
    private int ActiveCalls;

    public async Task RunAsync()
    {
      Task idle = WatchIdleAsync();
      try
      {
        while (!stopping.IsCancellationRequested)
        {
          Socket client;
          try
          {
            client = await listener.AcceptAsync(stopping.Token).ConfigureAwait(false);
          }
          catch (OperationCanceledException)
          {
            break;
          }

          Task handler = HandleAsync(client);
          Handlers.TryAdd(handler, 0);
          _ = handler.ContinueWith(t => Handlers.TryRemove(t, out _), TaskScheduler.Default);
        }
      }
      finally
      {
        await stopping.CancelAsync().ConfigureAwait(false);
        listener.Close(); // New connections are refused at once, so a stop is visible while calls finish.
        await Task.WhenAll([idle, .. Handlers.Keys]).ConfigureAwait(false);
      }
    }

    private async Task WatchIdleAsync()
    {
      TimeSpan timeout = options.IdleTimeout;
      if (timeout <= TimeSpan.Zero)
        return;

      TimeSpan interval = timeout < TimeSpan.FromSeconds(4) ? timeout / 4 : TimeSpan.FromSeconds(1);
      using PeriodicTimer timer = new(interval);
      try
      {
        while (await timer.WaitForNextTickAsync(stopping.Token).ConfigureAwait(false))
        {
          long idleMilliseconds = Environment.TickCount64 - Interlocked.Read(ref LastActivity);
          if (Volatile.Read(ref ActiveCalls) == 0 && idleMilliseconds >= timeout.TotalMilliseconds)
          {
            await stopping.CancelAsync().ConfigureAwait(false);
            return;
          }
        }
      }
      catch (OperationCanceledException)
      {
        // The server is stopping.
      }
    }

    private async Task HandleAsync(Socket client)
    {
      NetworkStream stream = new(client, ownsSocket: true);
      await using (stream.ConfigureAwait(false))
      {
        await HandleAsync(stream).ConfigureAwait(false);
      }
    }

    private async Task HandleAsync(NetworkStream stream)
    {
      try
      {
        using StreamReader reader = new(stream, Utf8NoBom, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);

        string? line;
        using (CancellationTokenSource readTimeout = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token))
        {
          readTimeout.CancelAfter(TimeSpan.FromSeconds(30));
          line = await reader.ReadLineAsync(readTimeout.Token).ConfigureAwait(false);
        }

        if (line is null)
          return;

        byte[] response = await RespondAsync(line, reader).ConfigureAwait(false);
        await stream.WriteAsync(response, CancellationToken.None).ConfigureAwait(false);
        await stream.FlushAsync(CancellationToken.None).ConfigureAwait(false);
      }
      catch (OperationCanceledException)
      {
        // The server is stopping, or the client sent nothing.
      }
      catch (IOException)
      {
        // The client went away; nothing to answer.
      }
      catch (SocketException)
      {
        // Same.
      }
    }

    private async Task<byte[]> RespondAsync(string line, StreamReader reader)
    {
      NuruDaemonRequest? request = NuruDaemonProtocol.DecodeRequest(line, out string? error);
      if (request is null)
        return NuruDaemonProtocol.EncodeError(error ?? "Invalid request.");

      switch (request.Operation)
      {
        case NuruDaemonOperations.Ping:
          CapabilitiesResponse catalog = app.Capabilities;
          return NuruDaemonProtocol.EncodeStatus(new NuruDaemonStatus(
            catalog.Name, catalog.Version, buildId, Environment.ProcessId, socketPath));

        case NuruDaemonOperations.Stop:
          await stopping.CancelAsync().ConfigureAwait(false);
          return NuruDaemonProtocol.EncodeOk();

        case NuruDaemonOperations.Run or NuruDaemonOperations.Invoke when request.BuildId is not null && request.BuildId != buildId:
          return NuruDaemonProtocol.EncodeError(
            $"This {app.Capabilities.Name} server (pid {Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}) is a different build of the app; stop it with 'serve --stop'.");

        case NuruDaemonOperations.Run when request.Args is not null:
        case NuruDaemonOperations.Invoke when request.Route is not null:
          Interlocked.Increment(ref ActiveCalls);
          try
          {
            using CancellationTokenSource call = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
            using CancellationTokenSource watch = CancellationTokenSource.CreateLinkedTokenSource(stopping.Token);
#pragma warning disable CA2025 // The finally block below awaits the watcher before 'call' and 'watch' are disposed.
            Task disconnect = CancelOnDisconnectAsync(reader, call, watch.Token);
#pragma warning restore CA2025
            NuruForwardResult result;
            try
            {
              result = request.Operation == NuruDaemonOperations.Run
                ? await RunArgsAsync(request.Args!, request.WorkingDirectory, call.Token).ConfigureAwait(false)
                : await InvokeAsync(request.Route!, request.Arguments, request.WorkingDirectory, call.Token).ConfigureAwait(false);
            }
            finally
            {
              await watch.CancelAsync().ConfigureAwait(false);
              await disconnect.ConfigureAwait(false);
            }

            return NuruDaemonProtocol.EncodeResult(result);
          }
          finally
          {
            Interlocked.Exchange(ref LastActivity, Environment.TickCount64);
            Interlocked.Decrement(ref ActiveCalls);
          }

        default:
          return NuruDaemonProtocol.EncodeError($"Unknown or incomplete request '{request.Operation}'.");
      }
    }

    /// <summary>
    /// Cancels <paramref name="call"/> when the client closes its side before the response. Ends when
    /// <paramref name="watchToken"/> is cancelled (the call finished).
    /// </summary>
    private static async Task CancelOnDisconnectAsync(StreamReader reader, CancellationTokenSource call, CancellationToken watchToken)
    {
      char[] buffer = new char[1];
      try
      {
        int read = await reader.ReadAsync(buffer, watchToken).ConfigureAwait(false);
        if (read == 0)
          await call.CancelAsync().ConfigureAwait(false);
      }
      catch (Exception exception) when (exception is OperationCanceledException or ObjectDisposedException or IOException or SocketException)
      {
        // The call finished first or the server is stopping.
        _ = exception;
      }
    }

    private Task<NuruForwardResult> RunArgsAsync(string[] args, string? workingDirectory, CancellationToken cancellationToken) =>
      RunGatedAsync(workingDirectory, async (output, error) =>
      {
        int exitCode = await app.ExecuteAsync(args, cancellationToken).ConfigureAwait(false);
        return new NuruForwardResult(exitCode, output.ToString(), error.ToString());
      }, cancellationToken);

    private async Task<NuruForwardResult> InvokeAsync(string route, string? argumentsJson, string? workingDirectory, CancellationToken cancellationToken)
    {
      using JsonDocument arguments = JsonDocument.Parse(argumentsJson ?? "{}");
      return await RunGatedAsync(workingDirectory, async (output, error) =>
      {
        if (!app.TryBind(route, arguments.RootElement, out NuruBoundRequest? request, out NuruBindError? bindError))
          return new NuruForwardResult(1, output.ToString(), bindError.Message + "\n");

        object? result = await app.InvokeAsync(request, cancellationToken).ConfigureAwait(false);
        using TestTerminal terminal = new();
        int exitCode = await app.RenderAsync(request, result, terminal).ConfigureAwait(false);
        return new NuruForwardResult(exitCode, output.ToString(), error + terminal.ErrorOutput, terminal.Output);
      }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs one call under the process gate, in the client's directory, with the console captured.
    /// </summary>
    private static async Task<NuruForwardResult> RunGatedAsync(
      string? workingDirectory,
      Func<StringWriter, StringWriter, Task<NuruForwardResult>> run,
      CancellationToken cancellationToken)
    {
      await CallGate.WaitAsync(cancellationToken).ConfigureAwait(false);
      string? previousDirectory = null;
      using StringWriter output = new(CultureInfo.InvariantCulture);
      using StringWriter error = new(CultureInfo.InvariantCulture);
      (TextWriter Out, TextWriter Error) previous = ConsoleRedirect.Redirect(output, error);
      try
      {
        if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
        {
          previousDirectory = Environment.CurrentDirectory;
          Environment.CurrentDirectory = workingDirectory;
        }

        return await run(output, error).ConfigureAwait(false);
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return new NuruForwardResult(130, output.ToString(), error + "Error: the call was cancelled.\n");
      }
#pragma warning disable CA1031 // A handler failure is returned to the client as stderr and exit code 1.
      catch (Exception exception)
#pragma warning restore CA1031
      {
        return new NuruForwardResult(1, output.ToString(), error + $"Error: {exception.Message}\n");
      }
      finally
      {
        ConsoleRedirect.Restore(previous);
        if (previousDirectory is not null)
          Environment.CurrentDirectory = previousDirectory;
        CallGate.Release();
      }
    }
  }
}
