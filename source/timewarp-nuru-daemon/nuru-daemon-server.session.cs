#region Purpose
// The Session nested class of NuruDaemonServer: one run of the accept loop and the idle timer.
#endregion

namespace TimeWarp.Nuru.Daemon;

public static partial class NuruDaemonServer
{
  /// <summary>One run of the accept loop and the idle timer.</summary>
  private sealed class Session(NuruApp app, NuruDaemonOptions options, string socketPath, NuruDaemonBuild build, Socket listener, CancellationTokenSource stopping)
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
            catalog.Name, catalog.Version, build.Id, Environment.ProcessId, socketPath, build.EntryPath, build.WriteTime));

        case NuruDaemonOperations.Stop:
          await stopping.CancelAsync().ConfigureAwait(false);
          return NuruDaemonProtocol.EncodeOk();

        case NuruDaemonOperations.Run or NuruDaemonOperations.Invoke when request.BuildId is not null && request.BuildId != build.Id:
          return NuruDaemonProtocol.EncodeError(
            $"This {app.Capabilities.Name} server (pid {Environment.ProcessId.ToString(CultureInfo.InvariantCulture)}) is a different build of the app ({build.Describe()}); stop it with 'serve --stop'.");

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
      RunCallAsync(workingDirectory, async (context, terminal) =>
      {
        int exitCode = await app.ExecuteAsync(args, context, cancellationToken).ConfigureAwait(false);
        return new NuruForwardResult(exitCode, terminal.Output, terminal.ErrorOutput);
      }, cancellationToken);

    private async Task<NuruForwardResult> InvokeAsync(string route, string? argumentsJson, string? workingDirectory, CancellationToken cancellationToken)
    {
      using JsonDocument arguments = JsonDocument.Parse(argumentsJson ?? "{}");
      return await RunCallAsync(workingDirectory, async (context, terminal) =>
      {
        if (!app.TryBind(route, arguments.RootElement, out NuruBoundRequest? request, out NuruBindError? bindError))
        {
          string bindText = RenderProblemOr(NuruProblems.FromBindError(bindError), bindError.Message);
          return new NuruForwardResult(1, terminal.Output, terminal.ErrorOutput + bindText + "\n");
        }

        object? result = await app.InvokeAsync(request, context, cancellationToken).ConfigureAwait(false);
        using TestTerminal rendered = CreateCallTerminal();
        int exitCode = await app.RenderAsync(request, result, rendered, context).ConfigureAwait(false);
        return new NuruForwardResult(exitCode, terminal.Output, terminal.ErrorOutput + rendered.ErrorOutput, rendered.Output);
      }, cancellationToken, problemErrors: true).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs one call in its own <see cref="NuruCallContext"/>: its own terminal (console writes included),
    /// the client's directory, and the exclusive gate when the options ask for it (see Design).
    /// </summary>
    private async Task<NuruForwardResult> RunCallAsync(
      string? workingDirectory,
      Func<NuruCallContext, TestTerminal, Task<NuruForwardResult>> run,
      CancellationToken cancellationToken,
      bool problemErrors = false)
    {
      using TestTerminal terminal = CreateCallTerminal();
      string? directory = !string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory) ? workingDirectory : null;
      NuruCallContext context = new(terminal, directory) { CaptureConsole = true };
      try
      {
        NuruExclusiveLease exclusive = options.ExclusiveCalls
          ? await NuruExclusiveLease.EnterAsync(app, context, cancellationToken).ConfigureAwait(false)
          : NuruExclusiveLease.None;
        await using (exclusive.ConfigureAwait(false))
        {
          return await run(context, terminal).ConfigureAwait(false);
        }
      }
      catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
      {
        return new NuruForwardResult(130, terminal.Output, terminal.ErrorOutput + "Error: the call was cancelled.\n");
      }
#pragma warning disable CA1031 // A handler failure is returned to the client as stderr and exit code 1.
      catch (Exception exception)
#pragma warning restore CA1031
      {
        string message = $"Error: {exception.Message}";
        if (problemErrors || app.ProblemRendererForErrors)
          message = RenderProblemOr(NuruProblems.FromException(exception), message);
        return new NuruForwardResult(1, terminal.Output, terminal.ErrorOutput + message + "\n");
      }
    }

    /// <summary>A plain-text terminal for one call: no color, not interactive.</summary>
    private static TestTerminal CreateCallTerminal() => new() { SupportsColor = false, IsInteractive = false };

    /// <summary>
    /// <paramref name="problem"/> rendered with the app's problem renderer, without a trailing newline, or
    /// <paramref name="fallback"/> when the app has none or the renderer throws.
    /// </summary>
    private string RenderProblemOr(SharedProblemDetails problem, string fallback)
    {
      try
      {
        return app.TryRenderProblem(problem, out string? text) ? text.TrimEnd('\r', '\n') : fallback;
      }
#pragma warning disable CA1031 // A failing renderer must not hide the original error.
      catch (Exception)
#pragma warning restore CA1031
      {
        return fallback;
      }
    }
  }
}
