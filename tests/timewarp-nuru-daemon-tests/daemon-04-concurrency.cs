#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// TimeWarp.Nuru.Daemon concurrency (task 508-016): forwarded calls run at once, each with its own output,
// exit code and working directory; exclusive routes and ExclusiveCalls serialize; cancelling one call
// cancels only it; ping answers and the idle timer waits while calls run; a soak of mixed argv and binder
// calls returns every caller its own result.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Daemon.Concurrency
{
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using OneOf;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru.Daemon;

[TestTag("Daemon")]
[TestTag("Concurrency")]
public class DaemonConcurrencyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DaemonConcurrencyTests>();

  private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

  private static NuruApp CreateApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("dcc-app")
      .Map("dcc-hold {name}").WithHandler(async (string name, ITerminal terminal, CancellationToken cancellationToken) =>
      {
        await terminal.WriteLineAsync($"start {name}");
        await CcConsole.WriteLineAsync($"console {name}");
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
        NuruCallContext.CurrentExitCode = name == "a" ? 3 : 4;
        return $"{name}@{NuruCallContext.CurrentDirectory}";
      }).AsCommand().Done()
      .Map("dcc-wait {name}").WithHandler(async (string name, CancellationToken cancellationToken) =>
      {
        try
        {
          await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
          CcRendezvous.Current.Cancelled(name);
          throw;
        }
      }).AsCommand().Done()
      .Map("dcc-excl {n:int}").WithHandler((int n) => CcTracker.Current.RunAsync(n)).AsQuery().Exclusive().Done()
      .Map("dcc-free {n:int}").WithHandler((int n) => CcTracker.Current.RunAsync(n)).AsQuery().Done()
      .Map<DccSoakQuery>()
      .Map<DccCodeQuery>()
      .Build();
    return app;
  }

  private static NuruDaemonOptions CreateOptions(TimeSpan? idleTimeout = null, bool exclusiveCalls = false) => new()
  {
    SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-dcc-").FullName, "dcc.sock"),
    IdleTimeout = idleTimeout ?? TimeSpan.Zero,
    ExclusiveCalls = exclusiveCalls
  };

  private static async Task<(Task<int> Server, CancellationTokenSource Stop)> StartAsync(NuruApp app, NuruDaemonOptions options)
  {
    CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(app, options, stop.Token));
    NuruDaemonClient client = new(options);
    for (int i = 0; i < 100 && await client.PingAsync(app) is null; i++)
      await Task.Delay(50);
    return (server, stop);
  }

  private static async Task StopAsync(Task<int> server, CancellationTokenSource stop)
  {
    await stop.CancelAsync();
    (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    stop.Dispose();
  }

  // A run request with its own "cwd", as a forwarding CLI in another directory sends it.
  private static async Task<(int ExitCode, string Stdout, string Stderr)> RunInAsync(string socketPath, string workingDirectory, params string[] args)
  {
    using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
    await using NetworkStream stream = new(socket);
    using MemoryStream request = new();
    using (Utf8JsonWriter writer = new(request))
    {
      writer.WriteStartObject();
      writer.WriteNumber("v", 1);
      writer.WriteString("op", "run");
      writer.WriteStartArray("args");
      foreach (string arg in args)
        writer.WriteStringValue(arg);
      writer.WriteEndArray();
      writer.WriteString("cwd", workingDirectory);
      writer.WriteEndObject();
    }

    request.WriteByte((byte)'\n');
    await stream.WriteAsync(request.ToArray());
    using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
    using JsonDocument response = JsonDocument.Parse(await reader.ReadLineAsync() ?? "{}");
    JsonElement root = response.RootElement;
    return (root.GetProperty("exitCode").GetInt32(), root.GetProperty("stdout").GetString()!, root.GetProperty("stderr").GetString()!);
  }

  // An invoke request with its own "cwd", as a forwarding MCP host in another directory sends it.
  private static async Task<(int ExitCode, string Rendered, string Stderr)> InvokeInAsync(string socketPath, string workingDirectory, string route, string argumentsJson)
  {
    using Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
    await socket.ConnectAsync(new UnixDomainSocketEndPoint(socketPath));
    await using NetworkStream stream = new(socket);
    using JsonDocument arguments = JsonDocument.Parse(argumentsJson);
    using MemoryStream request = new();
    using (Utf8JsonWriter writer = new(request))
    {
      writer.WriteStartObject();
      writer.WriteNumber("v", 1);
      writer.WriteString("op", "invoke");
      writer.WriteString("route", route);
      writer.WritePropertyName("arguments");
      arguments.RootElement.WriteTo(writer);
      writer.WriteString("cwd", workingDirectory);
      writer.WriteEndObject();
    }

    request.WriteByte((byte)'\n');
    await stream.WriteAsync(request.ToArray());
    using StreamReader reader = new(stream, Encoding.UTF8, leaveOpen: true);
    using JsonDocument response = JsonDocument.Parse(await reader.ReadLineAsync() ?? "{}");
    JsonElement root = response.RootElement;
    return (root.GetProperty("exitCode").GetInt32(), root.GetProperty("rendered").GetString()!, root.GetProperty("stderr").GetString()!);
  }

  public static async Task Should_return_the_exit_code_and_directory_of_each_invoked_call_through_render()
  {
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    string dirA = Directory.CreateTempSubdirectory("dcc-ia-").FullName;
    string dirB = Directory.CreateTempSubdirectory("dcc-ib-").FullName;
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      Task<(int ExitCode, string Rendered, string Stderr)> a = InvokeInAsync(options.SocketPath!, dirA, "dcc-code", """{"name":"a","code":5}""");
      Task<(int ExitCode, string Rendered, string Stderr)> b = InvokeInAsync(options.SocketPath!, dirB, "dcc-code", """{"name":"b","code":6}""");
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      CcRendezvous.Current.ReleaseAll();

      // The handler's NuruCallContext.CurrentExitCode is the call's exit code after render, and the
      // renderer resolves a relative path against the caller's directory.
      (int ExitCode, string Rendered, string Stderr) resultA = await a.WaitAsync(Limit);
      (int ExitCode, string Rendered, string Stderr) resultB = await b.WaitAsync(Limit);
      resultA.ExitCode.ShouldBe(5, resultA.Stderr);
      resultB.ExitCode.ShouldBe(6, resultB.Stderr);
      resultA.Rendered.ShouldBe($"a->{Path.Combine(dirA, "a")}\n");
      resultB.Rendered.ShouldBe($"b->{Path.Combine(dirB, "b")}\n");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_run_forwarded_calls_concurrently_each_with_its_own_output_exit_code_and_directory()
  {
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    string dirA = Directory.CreateTempSubdirectory("dcc-a-").FullName;
    string dirB = Directory.CreateTempSubdirectory("dcc-b-").FullName;
    string processDirectory = Environment.CurrentDirectory;
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      Task<(int ExitCode, string Stdout, string Stderr)> a = RunInAsync(options.SocketPath!, dirA, "dcc-hold", "a");
      Task<(int ExitCode, string Stdout, string Stderr)> b = RunInAsync(options.SocketPath!, dirB, "dcc-hold", "b");

      // Both handlers are inside at once; ping still answers.
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      (await new NuruDaemonClient(options).PingAsync(app)).ShouldNotBeNull();
      CcRendezvous.Current.ReleaseAll();

      (int ExitCode, string Stdout, string Stderr) resultA = await a.WaitAsync(Limit);
      (int ExitCode, string Stdout, string Stderr) resultB = await b.WaitAsync(Limit);
      resultA.ExitCode.ShouldBe(3);
      resultB.ExitCode.ShouldBe(4);
      resultA.Stdout.ShouldBe($"start a\nconsole a\na@{dirA}\n");
      resultB.Stdout.ShouldBe($"start b\nconsole b\nb@{dirB}\n");
      resultA.Stderr.ShouldBeEmpty();
      resultB.Stderr.ShouldBeEmpty();

      // The server process's own state did not move.
      Environment.CurrentDirectory.ShouldBe(processDirectory);
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_serialize_exclusive_routes_and_every_call_with_exclusive_calls()
  {
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      CcTracker.Current = new CcTracker();
      NuruForwardResult[] exclusive = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => client.RunAsync(app, ["dcc-excl", $"{i}"]))).WaitAsync(Limit);
      exclusive.ShouldAllBe(result => result.ExitCode == 0);
      CcTracker.Current.MaxActive.ShouldBe(1);

      CcTracker.Current = new CcTracker();
      NuruForwardResult[] free = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => client.RunAsync(app, ["dcc-free", $"{i}"]))).WaitAsync(Limit);
      free.ShouldAllBe(result => result.ExitCode == 0);
      CcTracker.Current.MaxActive.ShouldBeGreaterThan(1);
    }
    finally
    {
      await StopAsync(server, stop);
    }

    NuruDaemonOptions serialOptions = CreateOptions(exclusiveCalls: true);
    (server, stop) = await StartAsync(app, serialOptions);
    try
    {
      NuruDaemonClient client = new(serialOptions);
      CcTracker.Current = new CcTracker();
      NuruForwardResult[] free = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => client.RunAsync(app, ["dcc-free", $"{i}"]))).WaitAsync(Limit);
      free.ShouldAllBe(result => result.ExitCode == 0);
      CcTracker.Current.MaxActive.ShouldBe(1);

      // An exclusive route under ExclusiveCalls does not wait for its own call.
      CcTracker.Current = new CcTracker();
      (await client.RunAsync(app, ["dcc-excl", "1"]).WaitAsync(Limit)).ExitCode.ShouldBe(0);
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_cancel_only_the_call_whose_client_went_away()
  {
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      using CancellationTokenSource cancelA = new();
      Task<NuruForwardResult> a = client.RunAsync(app, ["dcc-wait", "a"], cancelA.Token);
      Task<NuruForwardResult> b = client.RunAsync(app, ["dcc-hold", "b"]);
      await CcRendezvous.Current.EnteredAtLeast(1).WaitAsync(Limit);
      await Task.Delay(100); // dcc-wait a is waiting in its handler too.

      // The forwarding CLI gives up (Ctrl+C): its connection closes and the server cancels that call only.
      await cancelA.CancelAsync();
      await Should.ThrowAsync<OperationCanceledException>(() => a.WaitAsync(Limit));
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      CcRendezvous.Current.CancelledNames.ShouldBe(["a"]);
      b.IsCompleted.ShouldBeFalse();

      CcRendezvous.Current.ReleaseAll();
      NuruForwardResult resultB = await b.WaitAsync(Limit);
      resultB.ExitCode.ShouldBe(4);
      resultB.Output.ShouldContain("b@");
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }

  public static async Task Should_not_stop_for_idle_while_a_call_runs()
  {
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions(TimeSpan.FromSeconds(1));
    CcRendezvous.Current = new CcRendezvous(expected: 1);
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    using (stop)
    {
      NuruDaemonClient client = new(options);
      Task<NuruForwardResult> call = client.RunAsync(app, ["dcc-hold", "b"]);
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);

      await Task.Delay(TimeSpan.FromSeconds(2.5));
      server.IsCompleted.ShouldBeFalse();
      (await client.PingAsync(app)).ShouldNotBeNull();

      CcRendezvous.Current.ReleaseAll();
      (await call.WaitAsync(Limit)).ExitCode.ShouldBe(4);

      // Idle counts from the last call to finish.
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    }
  }

  public static async Task Should_return_every_caller_its_own_result_under_load()
  {
    const int Calls = 64;
    NuruApp app = CreateApp();
    NuruDaemonOptions options = CreateOptions();
    (Task<int> server, CancellationTokenSource stop) = await StartAsync(app, options);
    try
    {
      NuruDaemonClient client = new(options);
      Task<(int N, bool Invoked, NuruForwardResult Result)>[] calls =
      [
        .. Enumerable.Range(0, Calls).Select(async n =>
        {
          bool invoked = n % 4 >= 2;
          NuruForwardResult result;
          if (invoked)
          {
            using JsonDocument arguments = JsonDocument.Parse($$"""{"n":{{n}}}""");
            result = await client.InvokeAsync(app, "dcc-soak", arguments.RootElement);
          }
          else
          {
            result = await client.RunAsync(app, ["dcc-soak", "--n", $"{n}"]);
          }

          return (n, invoked, result);
        })
      ];

      foreach ((int n, bool invoked, NuruForwardResult result) in await Task.WhenAll(calls).WaitAsync(Limit))
      {
        string text = invoked ? result.RenderedOutput : result.Output;
        if (n % 2 == 0)
        {
          result.ExitCode.ShouldBe(0, $"call {n}: {result.ErrorOutput}");
          text.TrimEnd().ShouldBe($"soak {n}");
          result.ErrorOutput.ShouldBeEmpty();
        }
        else
        {
          // A problem result: its status is the exit code, its title and detail are stderr.
          result.ExitCode.ShouldBe(DccSoakQuery.StatusFor(n), $"call {n}");
          result.ErrorOutput.ShouldContain($"Soak {n} failed");
          result.ErrorOutput.ShouldContain($"odd {n}");
          text.ShouldBeEmpty();
        }
      }
    }
    finally
    {
      await StopAsync(server, stop);
    }
  }
}

[NuruRoute("dcc-soak", Description = "Return the number, or a problem for odd numbers")]
public sealed class DccSoakQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  [Option("n", Description = "The number")]
  public int N { get; set; }

  public static int StatusFor(int n) => 10 + (n % 7);

  public sealed class Handler : IQueryHandler<DccSoakQuery, OneOf<string, SharedProblemDetails>>
  {
    public async Task<OneOf<string, SharedProblemDetails>> Handle(DccSoakQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      await Task.Delay(query.N % 5, cancellationToken);
      return query.N % 2 == 0
        ? $"soak {query.N}"
        : new SharedProblemDetails { Type = "https://example.test/dcc", Title = $"Soak {query.N} failed", Status = StatusFor(query.N), Detail = $"odd {query.N}" };
    }
  }
}
[NuruRoute("dcc-code", Description = "Set the exit code, return a path result")]
public sealed class DccCodeQuery : IQuery<CcPathResult>
{
  [Option("name", Description = "The name")]
  public string Name { get; set; } = "";

  [Option("code", Description = "The exit code")]
  public int Code { get; set; }

  public sealed class Handler : IQueryHandler<DccCodeQuery, CcPathResult>
  {
    public async Task<CcPathResult> Handle(DccCodeQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      await CcRendezvous.Current.EnterAsync(query.Name, cancellationToken);
      NuruCallContext.CurrentExitCode = query.Code;
      return new CcPathResult(query.Name);
    }
  }
}

}
