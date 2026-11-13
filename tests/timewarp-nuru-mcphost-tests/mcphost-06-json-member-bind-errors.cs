#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj
#:project $(SourceDirectory)timewarp-nuru-daemon/timewarp-nuru-daemon.csproj

#region Purpose
// Pins that a JSON member whose value fails inside the member (an unknown enum name in an array, a type
// mismatch in a nested object, a nested value that does not parse) is a bind error (task 530): exit 1 and
// error=Invalid in process, forwarded through the daemon and over McpHost, never a crash or Faulted. A
// handler exception stays Faulted.
// The CI multi-mode compilation must not contain a visible IResponseRenderer<SharedProblemDetails>, so the
// tests attach the renderer the generator would attach.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.JsonMemberBindErrors
{
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru.Daemon;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostJsonMemberBindErrorsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostJsonMemberBindErrorsTests>();

  private const string UnknownEnum = """{"Steps":[{"Tool":"Delete"}]}""";
  private const string NestedMismatch = """{"Steps":[{"Tool":"Find"}],"Options":{"Limits":{"Max":"lots"}}}""";
  private const string UnparsableNested = """{"Steps":[{"Tool":"Find","Target":"not-a-guid"}]}""";
  private const string HandlerThrows = """{"Steps":[]}""";

  // Each case: the JSON, and the "member: path" the message must name.
  private static readonly (string Json, string Location)[] Cases =
  [
    (UnknownEnum, "Steps: $[0].Tool"),
    (NestedMismatch, "Options: $.Limits.Max"),
    (UnparsableNested, "Steps: $[0].Target")
  ];

  // Each Build() is its own app; RunAsync is intercepted only beside its builder, so the tests use ExecuteAsync.
  private static NuruApp CreateApp(TestTerminal terminal) =>
    NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("jb-app")
      .Map<JbBatchQuery>()
      .Build();

  private static NuruApp CreateRenderingApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("jb-app")
      .UseProblemRendererForErrors()
      .Map<JbBatchQuery>()
      .AddServeRoute()
      .Build();
    app.ProblemRenderer = (_, problem) => Outline(problem);
    return app;
  }

  private static string Outline(SharedProblemDetails problem) =>
    $"error={problem.Type}\nerrorMessage={problem.Detail}";

  private static string Pattern(NuruApp app) =>
    app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("jb-batch", StringComparison.Ordinal)).Pattern;

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }

  private static Task<CallToolResult> CallAsync(NuruApp app, string json)
  {
    EndpointCapability endpoint = app.Capabilities.Endpoints.Single(e => e.Pattern == Pattern(app));
    return new NuruMcpToolInvoker(app).CallAsync(endpoint, Args(json));
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(block => block.Text));

  public static async Task Should_return_an_invalid_value_bind_error_from_try_bind()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    foreach ((string json, string location) in Cases)
    {
      using JsonDocument document = JsonDocument.Parse(json);
      app.TryBind(Pattern(app), document.RootElement, out _, out NuruBindError? error).ShouldBeFalse(json);
      error.ShouldNotBeNull();
      error.Kind.ShouldBe(NuruBindErrorKind.InvalidValue);
      error.Key.ShouldBe(location.Split(':')[0]);
      error.Message.ShouldStartWith($"Error: Invalid value for '{error.Key}' on '{Pattern(app)}'. {location}: ");
      error.Message.ShouldNotContain(" at ");
      error.Message.ShouldNotContain("LineNumber");
    }
  }

  public static async Task Should_exit_1_with_plain_usage_errors_in_process()
  {
    foreach ((string json, string location) in Cases)
    {
      using TestTerminal terminal = new();
      NuruApp app = CreateApp(terminal);

      int exitCode = await app.ExecuteAsync(["jb-batch", "--json-args", json]);

      exitCode.ShouldBe(1, json);
      terminal.ErrorOutput.ShouldContain(location);
      terminal.ErrorOutput.ShouldContain("Error: Invalid value for");
      terminal.ErrorOutput.ShouldNotContain("Unhandled");
      terminal.ErrorOutput.ShouldNotContain(" at TimeWarp");
    }
  }

  public static async Task Should_render_invalid_problems_in_process_and_with_no_daemon()
  {
    foreach ((string json, string location) in Cases)
    {
      using TestTerminal terminal = new();
      NuruApp app = CreateRenderingApp(terminal);
      app.RouteForwarder = null;

      (await app.ExecuteAsync(["jb-batch", "--json-args", json])).ShouldBe(1, json);
      terminal.ErrorOutput.ShouldContain("error=Invalid");
      terminal.ErrorOutput.ShouldContain(location);

      using TestTerminal noDaemon = new();
      NuruApp local = CreateRenderingApp(noDaemon);
      (await local.ExecuteAsync(["--no-daemon", "jb-batch", "--json-args", json])).ShouldBe(1, json);
      noDaemon.ErrorOutput.ShouldContain("error=Invalid");
      noDaemon.ErrorOutput.ShouldContain(location);
    }

    // A handler exception is not a bind error: in process it still escapes the CLI as itself.
    using TestTerminal faulted = new();
    NuruApp boom = CreateRenderingApp(faulted);
    boom.RouteForwarder = null;
    InvalidOperationException thrown = await Should.ThrowAsync<InvalidOperationException>(
      () => boom.ExecuteAsync(["jb-batch", "--json-args", HandlerThrows]));
    thrown.Message.ShouldBe("no steps");
    faulted.ErrorOutput.ShouldNotContain("error=Invalid");
  }

  public static async Task Should_report_is_error_with_invalid_over_mcp()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateRenderingApp(terminal);
    app.RouteForwarder = null;

    foreach ((string json, string location) in Cases)
    {
      CallToolResult result = await CallAsync(app, json);
      result.IsError.ShouldBe(true, json);
      Text(result).ShouldStartWith("error=Invalid\n");
      Text(result).ShouldContain(location);
    }

    CallToolResult boom = await CallAsync(app, HandlerThrows);
    boom.IsError.ShouldBe(true);
    Text(boom).ShouldBe("error=Faulted\nerrorMessage=no steps");
  }

  public static async Task Should_render_forwarded_failures_like_in_process_ones()
  {
    using TestTerminal servedTerminal = new();
    NuruApp served = CreateRenderingApp(servedTerminal);
    served.RouteForwarder = null;
    NuruDaemonOptions options = new()
    {
      SocketPath = Path.Combine(Directory.CreateTempSubdirectory("nuru-jb-").FullName, "jb.sock"),
      IdleTimeout = TimeSpan.Zero
    };
    using CancellationTokenSource stop = new();
    Task<int> server = Task.Run(() => NuruDaemonServer.RunAsync(served, options, stop.Token));
    NuruDaemonClient daemon = new(options);
    for (int i = 0; i < 100 && await daemon.PingAsync(served) is null; i++)
      await Task.Delay(50);

    try
    {
      using TestTerminal localTerminal = new();
      NuruApp local = CreateRenderingApp(localTerminal);
      local.RouteForwarder = null;

      foreach ((string json, string location) in Cases)
      {
        using TestTerminal hostTerminal = new();
        NuruApp host = CreateRenderingApp(hostTerminal);
        host.RouteForwarder = daemon;

        CallToolResult forwarded = await CallAsync(host, json);
        CallToolResult inProcess = await CallAsync(local, json);
        forwarded.IsError.ShouldBe(true, json);
        Text(forwarded).ShouldBe(Text(inProcess));
        Text(forwarded).ShouldStartWith("error=Invalid\n");
        Text(forwarded).ShouldContain(location);

        (await host.ExecuteAsync(["jb-batch", "--json-args", json])).ShouldBe(1, json);
        hostTerminal.ErrorOutput.ShouldContain("error=Invalid");
        hostTerminal.ErrorOutput.ShouldContain(location);
      }

      using TestTerminal boomTerminal = new();
      NuruApp boomHost = CreateRenderingApp(boomTerminal);
      boomHost.RouteForwarder = daemon;
      CallToolResult forwardedBoom = await CallAsync(boomHost, HandlerThrows);
      forwardedBoom.IsError.ShouldBe(true);
      Text(forwardedBoom).ShouldBe("error=Faulted\nerrorMessage=no steps");
    }
    finally
    {
      await stop.CancelAsync();
      (await server.WaitAsync(TimeSpan.FromSeconds(10))).ShouldBe(0);
    }
  }
}

[JsonConverter(typeof(JsonStringEnumConverter<JbTool>))]
public enum JbTool
{
  Find,
  Rename
}

public sealed class JbStep
{
  public JbTool Tool { get; set; }

  public Guid Target { get; set; }
}

public sealed class JbLimits
{
  public int Max { get; set; }
}

public sealed class JbOptions
{
  public JbLimits Limits { get; set; } = new();
}

[Operation("jb-batch", Description = "Run several steps")]
public sealed class JbBatchQuery : IQuery<string>
{
  public required IReadOnlyList<JbStep> Steps { get; set; }

  public JbOptions? Options { get; set; }

  public sealed class Handler : IRequestHandler<JbBatchQuery, string>
  {
    public ValueTask<string> Handle(JbBatchQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      if (query.Steps.Count == 0)
        throw new InvalidOperationException("no steps");

      return ValueTask.FromResult(string.Join(",", query.Steps.Select(s => s.Tool)));
    }
  }
}
}
