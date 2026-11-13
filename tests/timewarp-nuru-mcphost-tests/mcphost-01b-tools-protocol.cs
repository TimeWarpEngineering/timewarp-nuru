#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

#region Purpose
// Pins TimeWarp.Nuru.McpHost (task 508-004): group filter, instructions, the JSON-RPC protocol, bind errors,
// the route forwarder, console capture, and the mcp route help line.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.ToolsProtocol
{
using System.IO.Pipelines;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostToolsProtocolTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostToolsProtocolTests>();

  private static NuruApp CreateApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("mh-app")
      .WithDescription("McpHost test app")
      .Map("mh-greet {name}").WithHandler((string name) => $"Hello, {name}!").WithDescription("Greet someone").AsQuery().Done()
      .Map("mh-run --count {count:int} --verbose").WithHandler((int count, bool verbose) => verbose ? count * 10 : count).AsCommand().Done()
      .Map("mh-tags --tag {t}*").WithHandler((string[] t) => string.Join("+", t)).AsCommand().Done()
      .WithGroupPrefix("mh-admin")
        .Map("reset").WithHandler(() => "reset").AsCommand().Done()
      .Done()
      .Map<MhProblemQuery>()
      .Map<MhSymbolQuery>()
      .Build();
    return app;
  }

  public static async Task Should_suffix_colliding_tool_names()
  {
    EndpointCapability first = Endpoint("deploy {env}", EndpointKind.Command);
    EndpointCapability second = Endpoint("deploy {env} {tag}", EndpointKind.Command);
    CapabilitiesResponse catalog = new() { Name = "x", Version = "1", Endpoints = [first, second] };

    string[] names = [.. NuruMcpToolCatalog.CreateTools(catalog).Select(t => t.Tool.Name)];

    names.ShouldBe(["deploy", "deploy_2"]);
    NuruMcpToolCatalog.GetToolName(Endpoint("", EndpointKind.Command)).ShouldBe(NuruMcpHostOptions.DefaultRouteToolName);
    await Task.CompletedTask;
  }

  public static async Task Should_publish_only_the_filtered_group()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    string[] names = [.. NuruMcpToolCatalog.CreateTools(app.Capabilities, new NuruMcpHostOptions { GroupFilter = "MH-ADMIN" }).Select(t => t.Tool.Name)];

    names.ShouldBe(["mh-admin_reset"]);
    await Task.CompletedTask;
  }

  // 508-014: an [Operation(Group)] is the groupPath the filter reads. The tool keeps the operation name.
  public static async Task Should_filter_by_operation_group_without_renaming_the_tool()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    foreach (string filter in new[] { "mh-symbols", "mh-symbols.read", "MH-SYMBOLS READ" })
    {
      NuruMcpToolDefinition[] tools = [.. NuruMcpToolCatalog.CreateTools(app.Capabilities, new NuruMcpHostOptions { GroupFilter = filter })];

      tools.Select(t => t.Tool.Name).ShouldBe(["mh-get-symbol"]);
      tools[0].Endpoint.Pattern.ShouldBe("mh-get-symbol");
      tools[0].Endpoint.GroupPath.ShouldBe(["mh-symbols", "read"]);
    }

    NuruMcpToolCatalog.CreateTools(app.Capabilities, new NuruMcpHostOptions { GroupFilter = "mh-symbols.write" }).ShouldBeEmpty();
    NuruMcpToolCatalog.CreateTools(app.Capabilities).Select(t => t.Tool.Name).ShouldContain("mh-get-symbol");
    await Task.CompletedTask;
  }

  public static async Task Should_build_instructions_from_description_and_app_text()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    NuruMcpToolCatalog.CreateInstructions(app.Capabilities).ShouldBe("McpHost test app");
    NuruMcpToolCatalog.CreateInstructions(app.Capabilities, new NuruMcpHostOptions { Instructions = "Prefer mh-greet." })
      .ShouldBe("McpHost test app\n\nPrefer mh-greet.");
    await Task.CompletedTask;
  }

  public static async Task Should_list_and_call_tools_over_the_protocol()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    await using RawMcpSession session = new(app, new NuruMcpHostOptions { Instructions = "Prefer mh-greet." });

    JsonElement init = await session.InitializeAsync();
    init.GetProperty("serverInfo").GetProperty("name").GetString().ShouldBe("mh-app");
    init.GetProperty("instructions").GetString().ShouldBe("McpHost test app\n\nPrefer mh-greet.");

    JsonElement list = await session.RequestAsync("tools/list", "{}");
    JsonElement[] tools = [.. list.GetProperty("tools").EnumerateArray()];
    JsonElement greet = tools.Single(t => t.GetProperty("name").GetString() == "mh-greet");
    greet.GetProperty("description").GetString().ShouldBe("Greet someone");
    greet.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean().ShouldBeTrue();
    greet.GetProperty("inputSchema").GetProperty("required")[0].GetString().ShouldBe("name");

    JsonElement hello = await session.RequestAsync("tools/call", """{"name":"mh-greet","arguments":{"name":"Ada"}}""");
    IsError(hello).ShouldBeFalse();
    Text(hello).ShouldBe("Hello, Ada!");

    JsonElement run = await session.RequestAsync("tools/call", """{"name":"mh-run","arguments":{"count":4,"verbose":true}}""");
    Text(run).ShouldBe("40");

    JsonElement grouped = await session.RequestAsync("tools/call", """{"name":"mh-admin_reset","arguments":{}}""");
    Text(grouped).ShouldBe("reset");

    JsonElement repeated = await session.RequestAsync("tools/call", """{"name":"mh-tags","arguments":{"tag":["a","b"]}}""");
    Text(repeated).ShouldBe("a+b");
  }

  public static async Task Should_report_bind_errors_as_tool_errors()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    await using RawMcpSession session = new(app, new NuruMcpHostOptions());
    await session.InitializeAsync();

    // Unknown key: a tool result with isError, not a JSON-RPC error.
    JsonElement unknown = await session.RequestAsync("tools/call", """{"name":"mh-greet","arguments":{"name":"Ada","nope":1}}""");
    IsError(unknown).ShouldBeTrue();
    Text(unknown).ShouldContain("Unknown key 'nope'");

    JsonElement missing = await session.RequestAsync("tools/call", """{"name":"mh-greet","arguments":{}}""");
    IsError(missing).ShouldBeTrue();
    Text(missing).ShouldContain("Missing required value 'name'");

    JsonElement wrongType = await session.RequestAsync("tools/call", """{"name":"mh-run","arguments":{"count":"4"}}""");
    IsError(wrongType).ShouldBeTrue();

    JsonElement problem = await session.RequestAsync("tools/call", """{"name":"mh-problem","arguments":{"fail":true}}""");
    IsError(problem).ShouldBeTrue();
    Text(problem).ShouldContain("Bad input");
  }

  public static async Task Should_match_json_args_messages_for_the_same_arguments()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    EndpointCapability greet = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-greet {name}");
    using JsonDocument arguments = JsonDocument.Parse("""{"name":"Ada","nope":1}""");

    app.TryBind(greet.Pattern, arguments.RootElement, out _, out NuruBindError? error).ShouldBeFalse();
    NuruMcpToolInvoker invoker = new(app);
    CallToolResult result = await invoker.CallAsync(greet, arguments.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()));

    result.IsError.ShouldBe(true);
    ((TextContentBlock)result.Content[0]).Text.ShouldBe(error!.Message);
  }

  public static async Task Should_send_tool_calls_to_the_route_forwarder()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    EndpointCapability greet = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-greet {name}");
    EndpointCapability run = app.Capabilities.Endpoints.Single(e => e.Pattern.StartsWith("mh-run", StringComparison.Ordinal));

    // A served app's forwarder takes the call: the text is what the server rendered.
    McpRecordingForwarder forwarder = new(new NuruForwardResult(0, "", "", "Hello from server\n"));
    app.RouteForwarder = forwarder;
    CallToolResult forwarded = await new NuruMcpToolInvoker(app).CallAsync(greet, Args("""{"name":"Ada"}"""));
    forwarded.IsError.ShouldBe(false);
    ((TextContentBlock)forwarded.Content[0]).Text.ShouldBe("Hello from server");
    NuruForwardRequest request = forwarder.Requests.Single();
    request.Route.ShouldBe(greet.Pattern);
    request.Args.ShouldBeNull();
    request.JsonArguments.ShouldBe("""{"name":"Ada"}""");

    // Structured content parses the rendered number back by the catalog return type.
    app.RouteForwarder = new McpRecordingForwarder(new NuruForwardResult(0, "", "", "42\n"));
    CallToolResult number = await new NuruMcpToolInvoker(app, new NuruMcpHostOptions { ContentFormat = NuruMcpContentFormat.Json })
      .CallAsync(run, Args("""{"count":42}"""));
    number.StructuredContent!.Value.GetProperty("result").GetInt32().ShouldBe(42);

    // A failed forwarded call (a bind error in the server) is a tool error with its message.
    app.RouteForwarder = new McpRecordingForwarder(new NuruForwardResult(1, "", "Error: Unknown key 'nope' for 'mh-greet {name}'.\n"));
    CallToolResult failed = await new NuruMcpToolInvoker(app).CallAsync(greet, Args("""{"nope":1}"""));
    failed.IsError.ShouldBe(true);
    ((TextContentBlock)failed.Content[0]).Text.ShouldBe("Error: Unknown key 'nope' for 'mh-greet {name}'.");

    // A forwarder that declines (no server) leaves the call local.
    app.RouteForwarder = new McpRecordingForwarder(null);
    CallToolResult local = await new NuruMcpToolInvoker(app).CallAsync(greet, Args("""{"name":"Ada"}"""));
    ((TextContentBlock)local.Content[0]).Text.ShouldBe("Hello, Ada!");
  }

  public static async Task Should_capture_default_terminal_output_into_the_result()
  {
    // No UseTerminal: handlers get the default terminal, which writes to the console.
    NuruApp app = NuruApp.CreateBuilder()
      .Map("mh-term").WithHandler((ITerminal t) => { t.WriteLine("via terminal"); }).AsCommand().Done()
      .Build();
    EndpointCapability endpoint = app.Capabilities.Endpoints.Single();

    CallToolResult result = await new NuruMcpToolInvoker(app).CallAsync(endpoint, null);

    result.IsError.ShouldBe(false);
    ((TextContentBlock)result.Content.Single()).Text.ShouldBe("via terminal");
  }

  public static async Task Should_list_the_mcp_route_in_help_when_added()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("mh-help-only").WithHandler(() => "x").AsQuery().Done()
      .AddMcpRoute(instructions: "Use mh-help-only.", groupFilter: "none", contentFormat: NuruMcpContentFormat.Json)
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("Serve the routes as MCP tools over stdio").ShouldBeTrue();
  }

  private static EndpointCapability Endpoint(string pattern, EndpointKind kind) => new()
  {
    Pattern = pattern,
    GroupPath = [],
    Kind = kind,
    Parameters = [],
    Options = []
  };

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }

  private static bool IsError(JsonElement result) =>
    result.TryGetProperty("isError", out JsonElement isError) && isError.GetBoolean();

  private static string Text(JsonElement result) =>
    string.Join("\n", result.GetProperty("content").EnumerateArray().Select(c => c.GetProperty("text").GetString()));
}

/// <summary>Records forwarded requests and answers with a fixed result (null: run locally).</summary>
internal sealed class McpRecordingForwarder(NuruForwardResult? result) : INuruRouteForwarder
{
  public List<NuruForwardRequest> Requests { get; } = [];

  public Task<NuruForwardResult?> ForwardAsync(NuruApp app, NuruForwardRequest request, CancellationToken cancellationToken)
  {
    Requests.Add(request);
    return Task.FromResult(result);
  }
}

internal sealed class RawMcpSession : IAsyncDisposable
{
  private readonly Pipe ToServer = new();
  private readonly Pipe ToClient = new();
  private readonly StreamWriter Writer;
  private readonly StreamReader Reader;
  private readonly Task ServerTask;
  private int NextId;

  public RawMcpSession(NuruApp app, NuruMcpHostOptions options)
  {
    ServerTask = Task.Run(() => NuruMcpServer.RunAsync(app, ToServer.Reader.AsStream(), ToClient.Writer.AsStream(), options));
    Writer = new StreamWriter(ToServer.Writer.AsStream()) { AutoFlush = true, NewLine = "\n" };
    Reader = new StreamReader(ToClient.Reader.AsStream());
  }

  public async Task<JsonElement> InitializeAsync()
  {
    JsonElement result = await RequestAsync(
      "initialize",
      """{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"mh-test","version":"1.0.0"}}""");
    await Writer.WriteLineAsync("""{"jsonrpc":"2.0","method":"notifications/initialized"}""");
    return result;
  }

  public async Task<JsonElement> RequestAsync(string method, string paramsJson)
  {
    int id = ++NextId;
    await Writer.WriteLineAsync($$"""{"jsonrpc":"2.0","id":{{id}},"method":"{{method}}","params":{{paramsJson}}}""");

    using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
    while (true)
    {
      string? line = await Reader.ReadLineAsync(timeout.Token) ?? throw new InvalidOperationException("Server closed the stream.");
      using JsonDocument message = JsonDocument.Parse(line);
      if (!message.RootElement.TryGetProperty("id", out JsonElement responseId) || responseId.ValueKind != JsonValueKind.Number || responseId.GetInt32() != id)
        continue;

      if (message.RootElement.TryGetProperty("error", out JsonElement error))
        throw new InvalidOperationException($"JSON-RPC error: {error}");

      return message.RootElement.GetProperty("result").Clone();
    }
  }

  public async ValueTask DisposeAsync()
  {
    await ToServer.Writer.CompleteAsync();
    await ServerTask.WaitAsync(TimeSpan.FromSeconds(10));
    await ToClient.Reader.CompleteAsync();
    await Writer.DisposeAsync();
    Reader.Dispose();
  }
}

[Operation("mh-get-symbol", Description = "Look up a symbol", Group = "mh-symbols.read")]
public sealed class MhSymbolQuery : IQuery<string>
{
  public string Name { get; set; } = "";

  public sealed class Handler : IRequestHandler<MhSymbolQuery, string>
  {
    public ValueTask<string> Handle(MhSymbolQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return ValueTask.FromResult($"symbol:{query.Name}");
    }
  }
}

[NuruRoute("mh-problem")]
public sealed class MhProblemQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  [Option("fail", Description = "Return a problem")]
  public bool Fail { get; set; }

  public sealed class Handler : IRequestHandler<MhProblemQuery, OneOf<string, SharedProblemDetails>>
  {
    public ValueTask<OneOf<string, SharedProblemDetails>> Handle(MhProblemQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      OneOf<string, SharedProblemDetails> outcome = query.Fail
        ? new SharedProblemDetails { Type = "https://example.test/mh", Title = "Bad input", Status = 17, Detail = "too small" }
        : "ok";
      return ValueTask.FromResult(outcome);
    }
  }
}
}
