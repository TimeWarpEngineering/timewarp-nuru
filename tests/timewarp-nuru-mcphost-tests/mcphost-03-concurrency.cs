#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// TimeWarp.Nuru.McpHost concurrency (task 508-016): tools/call requests run at once over one MCP session,
// in-process and forwarded to a served app's server, each with its own text; notifications/cancelled
// cancels only that call; ExclusiveCalls serializes in-process calls.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.Concurrency
{
using System.IO.Pipelines;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TimeWarp.Nuru.Daemon;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
[TestTag("Concurrency")]
public class McpHostConcurrencyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostConcurrencyTests>();

  private static readonly TimeSpan Limit = TimeSpan.FromSeconds(30);

  private static NuruApp CreateApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("mcc-app")
      .Map("mcc-hold {name}").WithHandler(async (string name, ITerminal terminal, CancellationToken cancellationToken) =>
      {
        await terminal.WriteLineAsync($"start {name}");
        await CcConsole.WriteLineAsync($"console {name}");
        await CcRendezvous.Current.EnterAsync(name, cancellationToken);
        return $"done {name}";
      }).AsQuery().Done()
      .Map("mcc-wait {name}").WithHandler(async (string name, CancellationToken cancellationToken) =>
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
      .Map("mcc-free {n:int}").WithHandler((int n) => CcTracker.Current.RunAsync(n)).AsQuery().Done()
      .Map<MccCodeQuery>()
      .Build();
    return app;
  }

  private static CallToolRequestParams Call(string name, string argumentsJson)
  {
    using JsonDocument document = JsonDocument.Parse(argumentsJson);
    return new CallToolRequestParams
    {
      Name = name,
      Arguments = document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone())
    };
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));

  public static async Task Should_run_tool_calls_concurrently_over_one_session()
  {
    NuruApp app = CreateApp();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    await using McpSession session = await McpSession.StartAsync(app, new NuruMcpHostOptions());

    Task<CallToolResult> a = session.Client.CallToolAsync(Call("mcc-hold", """{"name":"a"}""")).AsTask();
    Task<CallToolResult> b = session.Client.CallToolAsync(Call("mcc-hold", """{"name":"b"}""")).AsTask();
    await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
    CcRendezvous.Current.ReleaseAll();

    CallToolResult resultA = await a.WaitAsync(Limit);
    CallToolResult resultB = await b.WaitAsync(Limit);
    resultA.IsError.ShouldBe(false);
    resultB.IsError.ShouldBe(false);
    Text(resultA).ShouldBe("start a\nconsole a\ndone a");
    Text(resultB).ShouldBe("start b\nconsole b\ndone b");
  }

  public static async Task Should_return_each_tool_call_the_exit_code_its_handler_set()
  {
    NuruApp app = CreateApp();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    await using MccRawSession session = await MccRawSession.StartAsync(app);

    Task<JsonElement> a = session.CallAsync(30, "mcc-code", """{"name":"a","code":0}""");
    Task<JsonElement> b = session.CallAsync(31, "mcc-code", """{"name":"b","code":7}""");
    await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
    CcRendezvous.Current.ReleaseAll();

    // The handler's NuruCallContext.CurrentExitCode survives render: 7 is a tool error, 0 is not.
    // The renderer resolves the relative path against the call's directory.
    JsonElement resultA = await a.WaitAsync(Limit);
    JsonElement resultB = await b.WaitAsync(Limit);
    resultA.GetProperty("isError").GetBoolean().ShouldBeFalse();
    resultB.GetProperty("isError").GetBoolean().ShouldBeTrue();
    MccRawSession.Text(resultA).ShouldBe($"a->{Path.GetFullPath("a")}");
    MccRawSession.Text(resultB).ShouldBe($"b->{Path.GetFullPath("b")}");
  }

  public static async Task Should_return_each_forwarded_tool_call_the_exit_code_its_handler_set()
  {
    NuruApp served = CreateApp();
    NuruDaemonOptions options = new()
    {
      SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-mcc-").FullName, "mcc.sock"),
      IdleTimeout = TimeSpan.Zero
    };
    using CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(served, options, stop.Token));
    NuruDaemonClient daemon = new(options);
    for (int i = 0; i < 100 && await daemon.PingAsync(served) is null; i++)
      await Task.Delay(50);

    try
    {
      NuruApp host = CreateApp();
      host.RouteForwarder = daemon;
      CcRendezvous.Current = new CcRendezvous(expected: 2);
      await using MccRawSession session = await MccRawSession.StartAsync(host);

      Task<JsonElement> a = session.CallAsync(40, "mcc-code", """{"name":"a","code":7}""");
      Task<JsonElement> b = session.CallAsync(41, "mcc-code", """{"name":"b","code":0}""");
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      CcRendezvous.Current.ReleaseAll();

      JsonElement resultA = await a.WaitAsync(Limit);
      JsonElement resultB = await b.WaitAsync(Limit);
      resultA.GetProperty("isError").GetBoolean().ShouldBeTrue();
      resultB.GetProperty("isError").GetBoolean().ShouldBeFalse();
      MccRawSession.Text(resultA).ShouldBe($"a->{Path.GetFullPath("a")}");
      MccRawSession.Text(resultB).ShouldBe($"b->{Path.GetFullPath("b")}");
    }
    finally
    {
      await stop.CancelAsync();
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    }
  }

  public static async Task Should_cancel_only_the_call_the_client_cancelled()
  {
    NuruApp app = CreateApp();
    CcRendezvous.Current = new CcRendezvous(expected: 2);
    await using MccRawSession session = await MccRawSession.StartAsync(app);

    Task<JsonElement> a = session.CallAsync(10, "mcc-wait", """{"name":"a"}""");
    Task<JsonElement> b = session.CallAsync(11, "mcc-hold", """{"name":"b"}""");
    await CcRendezvous.Current.EnteredAtLeast(1).WaitAsync(Limit);
    await Task.Delay(100); // mcc-wait a is waiting in its handler too.

    // notifications/cancelled for request 10 cancels that call's token only.
    await session.CancelAsync(10);
    await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
    CcRendezvous.Current.CancelledNames.ShouldBe(["a"]);
    b.IsCompleted.ShouldBeFalse();

    CcRendezvous.Current.ReleaseAll();
    MccRawSession.Text(await b.WaitAsync(Limit)).ShouldBe("start b\nconsole b\ndone b");
    _ = a; // A cancelled request gets no response.
  }

  public static async Task Should_serialize_in_process_calls_with_exclusive_calls()
  {
    NuruApp app = CreateApp();
    EndpointCapability free = app.Capabilities.Endpoints.Single(e => e.Pattern == "mcc-free {n:int}");

    CcTracker.Current = new CcTracker();
    NuruMcpToolInvoker concurrent = new(app);
    CallToolResult[] results = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => concurrent.CallAsync(free, Args($$"""{"n":{{i}}}""")))).WaitAsync(Limit);
    results.ShouldAllBe(result => result.IsError != true);
    CcTracker.Current.MaxActive.ShouldBeGreaterThan(1);

    CcTracker.Current = new CcTracker();
    NuruMcpToolInvoker serial = new(app, new NuruMcpHostOptions { ExclusiveCalls = true });
    results = await Task.WhenAll(Enumerable.Range(0, 6).Select(i => serial.CallAsync(free, Args($$"""{"n":{{i}}}""")))).WaitAsync(Limit);
    results.ShouldAllBe(result => result.IsError != true);
    CcTracker.Current.MaxActive.ShouldBe(1);
  }

  public static async Task Should_run_forwarded_tool_calls_concurrently_and_cancel_them_alone()
  {
    // The server serves one instance of the app; the MCP host forwards from another (same build).
    NuruApp served = CreateApp();
    NuruDaemonOptions options = new()
    {
      SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-mcc-").FullName, "mcc.sock"),
      IdleTimeout = TimeSpan.Zero
    };
    using CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(served, options, stop.Token));
    NuruDaemonClient daemon = new(options);
    for (int i = 0; i < 100 && await daemon.PingAsync(served) is null; i++)
      await Task.Delay(50);

    try
    {
      NuruApp host = CreateApp();
      host.RouteForwarder = daemon;
      CcRendezvous.Current = new CcRendezvous(expected: 3);
      await using MccRawSession session = await MccRawSession.StartAsync(host);

      Task<JsonElement> a = session.CallAsync(20, "mcc-hold", """{"name":"a"}""");
      Task<JsonElement> b = session.CallAsync(21, "mcc-hold", """{"name":"b"}""");
      Task<JsonElement> c = session.CallAsync(22, "mcc-wait", """{"name":"c"}""");
      await CcRendezvous.Current.EnteredAtLeast(2).WaitAsync(Limit);
      await Task.Delay(100);

      // Cancelling the MCP request closes its forwarded connection; the server cancels that call only.
      await session.CancelAsync(22);
      await CcRendezvous.Current.AllEntered.WaitAsync(Limit);
      CcRendezvous.Current.CancelledNames.ShouldBe(["c"]);

      CcRendezvous.Current.ReleaseAll();
      MccRawSession.Text(await a.WaitAsync(Limit)).ShouldBe("start a\nconsole a\ndone a");
      MccRawSession.Text(await b.WaitAsync(Limit)).ShouldBe("start b\nconsole b\ndone b");
      _ = c;
    }
    finally
    {
      await stop.CancelAsync();
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    }
  }

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }
}

[NuruRoute("mcc-code", Description = "Set the exit code, return a path result")]
public sealed class MccCodeQuery : IQuery<CcPathResult>
{
  [Option("name", Description = "The name")]
  public string Name { get; set; } = "";

  [Option("code", Description = "The exit code")]
  public int Code { get; set; }

  public sealed class Handler : IQueryHandler<MccCodeQuery, CcPathResult>
  {
    public async Task<CcPathResult> Handle(MccCodeQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      await CcRendezvous.Current.EnterAsync(query.Name, cancellationToken);
      NuruCallContext.CurrentExitCode = query.Code;
      return new CcPathResult(query.Name);
    }
  }
}

/// <summary>An MCP client and the app's MCP server over in-memory pipes.</summary>
internal sealed class McpSession : IAsyncDisposable
{
  private readonly Pipe ToServer = new();
  private readonly Pipe ToClient = new();
  private readonly CancellationTokenSource Stop = new();
  private Task ServerTask = Task.CompletedTask;

  public McpClient Client { get; private set; } = null!;

  public static async Task<McpSession> StartAsync(NuruApp app, NuruMcpHostOptions options)
  {
    McpSession session = new();
    session.ServerTask = Task.Run(() => NuruMcpServer.RunAsync(app, session.ToServer.Reader.AsStream(), session.ToClient.Writer.AsStream(), options, session.Stop.Token));
    StreamClientTransport transport = new(session.ToServer.Writer.AsStream(), session.ToClient.Reader.AsStream());
    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
    session.Client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
    return session;
  }

  public async ValueTask DisposeAsync()
  {
    await Client.DisposeAsync();
    await ToServer.Writer.CompleteAsync();
    await Stop.CancelAsync();
    await ServerTask.WaitAsync(TimeSpan.FromSeconds(10));
    await ToClient.Reader.CompleteAsync();
    Stop.Dispose();
  }
}

/// <summary>
/// Raw JSON-RPC over pipes, for requests in flight at once and <c>notifications/cancelled</c>. (The SDK
/// client on protocol 2026-07-28 does not send that notification over a stream transport.)
/// </summary>
internal sealed class MccRawSession : IAsyncDisposable
{
  private readonly Pipe ToServer = new();
  private readonly Pipe ToClient = new();
  private readonly CancellationTokenSource Stop = new();
  private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> Pending = new();
  private readonly SemaphoreSlim WriteLock = new(1, 1);
  private StreamWriter Writer = null!;
  private Task ServerTask = Task.CompletedTask;
  private Task ReaderTask = Task.CompletedTask;

  public static async Task<MccRawSession> StartAsync(NuruApp app)
  {
    MccRawSession session = new();
    session.ServerTask = Task.Run(() => NuruMcpServer.RunAsync(app, session.ToServer.Reader.AsStream(), session.ToClient.Writer.AsStream(), new NuruMcpHostOptions(), session.Stop.Token));
    session.Writer = new StreamWriter(session.ToServer.Writer.AsStream()) { AutoFlush = true, NewLine = "\n" };
    session.ReaderTask = Task.Run(session.ReadLoopAsync);
    await session.RequestAsync(1, "initialize", """{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"mcc-test","version":"1.0.0"}}""");
    await session.WriteAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
    return session;
  }

  public static string Text(JsonElement result) =>
    string.Join("\n", result.GetProperty("content").EnumerateArray().Select(block => block.GetProperty("text").GetString()));

  public Task<JsonElement> CallAsync(int id, string tool, string argumentsJson) =>
    RequestAsync(id, "tools/call", $$"""{"name":"{{tool}}","arguments":""" + argumentsJson + "}");

  public Task CancelAsync(int id) =>
    WriteAsync($$$"""{"jsonrpc":"2.0","method":"notifications/cancelled","params":{"requestId":{{{id}}},"reason":"test"}}""");

  public async ValueTask DisposeAsync()
  {
    await Stop.CancelAsync();
    await ToServer.Writer.CompleteAsync();
    await ServerTask.WaitAsync(TimeSpan.FromSeconds(10));
    await ToClient.Writer.CompleteAsync();
    await ReaderTask.WaitAsync(TimeSpan.FromSeconds(10));
    await Writer.DisposeAsync();
    Stop.Dispose();
    WriteLock.Dispose();
  }

  private async Task<JsonElement> RequestAsync(int id, string method, string paramsJson)
  {
    TaskCompletionSource<JsonElement> response = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Pending[id] = response;
    await WriteAsync($$"""{"jsonrpc":"2.0","id":{{id}},"method":"{{method}}","params":{{paramsJson}}}""");
    return await response.Task;
  }

  private async Task WriteAsync(string line)
  {
    await WriteLock.WaitAsync();
    try
    {
      await Writer.WriteLineAsync(line);
    }
    finally
    {
      WriteLock.Release();
    }
  }

  private async Task ReadLoopAsync()
  {
    using StreamReader reader = new(ToClient.Reader.AsStream());
    while (await reader.ReadLineAsync() is string line)
    {
      using JsonDocument message = JsonDocument.Parse(line);
      if (!message.RootElement.TryGetProperty("id", out JsonElement id) || id.ValueKind != JsonValueKind.Number
        || !Pending.TryRemove(id.GetInt32(), out TaskCompletionSource<JsonElement>? response))
      {
        continue;
      }

      if (message.RootElement.TryGetProperty("result", out JsonElement result))
        response.TrySetResult(result.Clone());
      else
        response.TrySetException(new InvalidOperationException(message.RootElement.ToString()));
    }
  }
}
}
