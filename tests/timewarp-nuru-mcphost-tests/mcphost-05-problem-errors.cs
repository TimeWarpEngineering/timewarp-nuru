#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

// tools/call bind failures and escaped exceptions go through the app's problem renderer (task 508-018), in
// process and forwarded to a served app's server. The CI multi-mode compilation must not contain a visible
// IResponseRenderer<SharedProblemDetails>, so these tests attach the renderer the generator would attach.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.ProblemErrors
{
using System.Text.Json;
using ModelContextProtocol.Protocol;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru.Daemon;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostProblemErrorsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostProblemErrorsTests>();

  private const string Greet = "mpe-greet {name}";
  private const string Boom = "mpe-boom {name}";

  private static NuruApp CreateApp()
  {
    NuruApp app = NuruApp.CreateBuilder()
      .WithName("mpe-app")
      .Map("mpe-greet {name}").WithHandler((string name) => $"Hello, {name}!").AsQuery().Done()
      .Map("mpe-boom {name}").WithHandler((string name) => name == "ok" ? "fine" : throw new InvalidOperationException($"boom {name}")).AsCommand().Done()
      .Build();
    return app;
  }

  private static NuruApp CreateRenderingApp()
  {
    NuruApp app = CreateApp();
    app.ProblemRenderer = (_, problem) => Outline(problem);
    return app;
  }

  private static string Outline(SharedProblemDetails problem) =>
    $"error={problem.Type}\nerrorMessage={problem.Detail}";

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }

  private static Task<CallToolResult> CallAsync(NuruApp app, string pattern, string json)
  {
    EndpointCapability endpoint = app.Capabilities.Endpoints.Single(e => e.Pattern == pattern);
    return new NuruMcpToolInvoker(app).CallAsync(endpoint, Args(json));
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

  public static async Task Should_render_a_bind_failure_through_the_problem_renderer()
  {
    NuruApp app = CreateRenderingApp();

    CallToolResult result = await CallAsync(app, Greet, """{"name":"Ada","nope":1}""");

    result.IsError.ShouldBe(true);
    Text(result).ShouldBe($"error=Invalid\nerrorMessage=Unknown key 'nope' for '{Greet}'. Known names: name.");
  }

  public static async Task Should_render_an_escaped_exception_as_faulted()
  {
    NuruApp app = CreateRenderingApp();

    CallToolResult result = await CallAsync(app, Boom, """{"name":"x"}""");

    result.IsError.ShouldBe(true);
    Text(result).ShouldBe("error=Faulted\nerrorMessage=boom x");
  }

  public static async Task Should_keep_plain_errors_without_a_problem_renderer()
  {
    NuruApp app = CreateApp();
    using JsonDocument bad = JsonDocument.Parse("""{"name":"Ada","nope":1}""");
    app.TryBind(Greet, bad.RootElement, out _, out NuruBindError? expected).ShouldBeFalse();

    CallToolResult bind = await CallAsync(app, Greet, """{"name":"Ada","nope":1}""");
    Text(bind).ShouldBe(expected!.Message);

    CallToolResult boom = await CallAsync(app, Boom, """{"name":"x"}""");
    Text(boom).ShouldBe("Error: boom x");
  }

  public static async Task Should_fall_back_to_plain_text_when_the_renderer_throws()
  {
    NuruApp app = CreateApp();
    app.ProblemRenderer = (_, _) => throw new InvalidOperationException("renderer broke");

    CallToolResult result = await CallAsync(app, Boom, """{"name":"x"}""");

    result.IsError.ShouldBe(true);
    Text(result).ShouldBe("Error: boom x");
  }

  public static async Task Should_render_forwarded_failures_like_in_process_ones()
  {
    NuruApp served = CreateRenderingApp();
    NuruDaemonOptions options = new()
    {
      SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-mpe-").FullName, "mpe.sock"),
      IdleTimeout = TimeSpan.Zero
    };
    using CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(served, options, stop.Token));
    NuruDaemonClient daemon = new(options);
    for (int i = 0; i < 100 && await daemon.PingAsync(served) is null; i++)
      await Task.Delay(50);

    try
    {
      NuruApp local = CreateRenderingApp();
      NuruApp host = CreateRenderingApp();
      host.RouteForwarder = daemon;

      CallToolResult forwardedBind = await CallAsync(host, Greet, """{"name":"Ada","nope":1}""");
      CallToolResult localBind = await CallAsync(local, Greet, """{"name":"Ada","nope":1}""");
      forwardedBind.IsError.ShouldBe(true);
      Text(forwardedBind).ShouldBe(Text(localBind));
      Text(forwardedBind).ShouldStartWith("error=Invalid\n");

      CallToolResult forwardedBoom = await CallAsync(host, Boom, """{"name":"x"}""");
      CallToolResult localBoom = await CallAsync(local, Boom, """{"name":"x"}""");
      forwardedBoom.IsError.ShouldBe(true);
      Text(forwardedBoom).ShouldBe(Text(localBoom));
      Text(forwardedBoom).ShouldBe("error=Faulted\nerrorMessage=boom x");

      CallToolResult forwardedOk = await CallAsync(host, Boom, """{"name":"ok"}""");
      forwardedOk.IsError.ShouldBe(false);
      Text(forwardedOk).ShouldBe("fine");
    }
    finally
    {
      await stop.CancelAsync();
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    }
  }
}
}
