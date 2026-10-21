#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

// TimeWarp.Nuru.McpHost (task 508-004): catalog to MCP tools (names, schema, annotations, filter,
// instructions) and tools/call through the public binder, over the real JSON-RPC protocol.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.Tools
{
using System.IO.Pipelines;
using System.Text.Json;
using ModelContextProtocol.Protocol;
using OneOf;
using TimeWarp.Contracts;
using TimeWarp.Foundation.Types;
using TimeWarp.Nuru.McpHost;

public enum MhEnv
{
  Dev,
  Staging,
  Prod
}

public static class MhOuter
{
  public enum MhTier
  {
    Basic,
    Premium
  }

  public static class MhInner
  {
    public enum MhSize
    {
      Small,
      Large
    }
  }
}

[TestTag("McpHost")]
public class McpHostToolsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostToolsTests>();

  private static NuruApp CreateApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("mh-app")
      .WithDescription("McpHost test app")
      .Map("mh-greet {name}").WithHandler((string name) => $"Hello, {name}!").WithDescription("Greet someone").AsQuery().Done()
      .Map("mh-config set {key} {value}").WithHandler((string key, string value) => $"{key}={value}").AsIdempotentCommand().Done()
      .Map("mh-run --count {count:int} --verbose").WithHandler((int count, bool verbose) => verbose ? count * 10 : count).AsCommand().Done()
      .Map("mh-tags --tag {t}*").WithHandler((string[] t) => string.Join("+", t)).AsCommand().Done()
      .Map("mh-exec {*args}").WithHandler((string[] args) => string.Join(" ", args)).AsCommand().Done()
      .Map("mh-pick --env {env}").WithHandler((MhEnv env) => $"env:{env}").AsQuery().Done()
      .Map("mh-tier {tier:MhTier?} --size {size}").WithHandler((MhOuter.MhTier? tier, MhOuter.MhInner.MhSize size) => $"{tier}:{size}").AsQuery().Done()
      .Map("mh-note {text} --level {level:int?}").WithHandler((string text, int? level) => $"{text}:{level?.ToString(CultureInfo.InvariantCulture) ?? "none"}").AsCommand().Done()
#pragma warning disable RS0030 // The handler writes to the console directly, like the calculator sample.
      .Map("mh-echo").WithHandler(() => Console.Out.WriteLine("from console")).AsCommand().Done()
      .Map("mh-log-text").WithHandler(() => { Console.Out.WriteLine("log line"); return "value"; }).AsQuery().Done()
#pragma warning restore RS0030
      .WithGroupPrefix("mh-admin")
        .Map("reset").WithHandler(() => "reset").AsCommand().Done()
      .Done()
      .Map<MhWipeCommand>()
      .Map<MhReportQuery>()
      .Map<MhProblemQuery>()
      .Map<MhLoggingReportQuery>()
      .Map<MhSymbolQuery>()
      .Build();
    return app;
  }

  public static async Task Should_name_tools_after_route_literals()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    string[] names = [.. NuruMcpToolCatalog.CreateTools(app.Capabilities).Select(t => t.Tool.Name)];

    names.ShouldContain("mh-greet");
    names.ShouldContain("mh-config_set");
    names.ShouldContain("mh-run");
    names.ShouldContain("mh-admin_reset");
    names.ShouldContain("mh-wipe");
    names.Distinct(StringComparer.Ordinal).Count().ShouldBe(names.Length);

    NuruMcpToolDefinition config = NuruMcpToolCatalog.CreateTools(app.Capabilities, new NuruMcpHostOptions { ToolNameSeparator = "." })
      .Single(t => t.Endpoint.Pattern.StartsWith("mh-config", StringComparison.Ordinal));
    config.Tool.Name.ShouldBe("mh-config.set");
    await Task.CompletedTask;
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

  public static async Task Should_build_input_schema_from_parameters_and_options()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    IReadOnlyList<NuruMcpToolDefinition> tools = NuruMcpToolCatalog.CreateTools(app.Capabilities);

    JsonElement greet = Schema(tools, "mh-greet");
    greet.GetProperty("type").GetString().ShouldBe("object");
    greet.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();
    greet.GetProperty("properties").GetProperty("name").GetProperty("type").GetString().ShouldBe("string");
    Required(greet).ShouldBe(["name"]);

    JsonElement run = Schema(tools, "mh-run");
    run.GetProperty("properties").GetProperty("count").GetProperty("type").GetString().ShouldBe("integer");
    run.GetProperty("properties").GetProperty("verbose").GetProperty("type").GetString().ShouldBe("boolean");
    Required(run).ShouldBe(["count"]);

    JsonElement tags = Schema(tools, "mh-tags").GetProperty("properties").GetProperty("tag");
    tags.GetProperty("type").GetString().ShouldBe("array");
    tags.GetProperty("items").GetProperty("type").GetString().ShouldBe("string");

    JsonElement exec = Schema(tools, "mh-exec").GetProperty("properties").GetProperty("args");
    exec.GetProperty("type").GetString().ShouldBe("array");
    exec.GetProperty("items").GetProperty("type").GetString().ShouldBe("string");

    JsonElement env = Schema(tools, "mh-pick").GetProperty("properties").GetProperty("env");
    env.GetProperty("type").GetString().ShouldBe("string");
    string[] allowed = [.. env.GetProperty("enum").EnumerateArray().Select(v => v.GetString()!)];
    allowed.ShouldBe(["Dev", "Staging", "Prod"]);

    // Task 508-009: enums nested in a class get the same enum list; a nullable one keeps "null".
    JsonElement tierProperties = Schema(tools, "mh-tier").GetProperty("properties");
    JsonElement tier = tierProperties.GetProperty("tier");
    string[] tierTypes = [.. tier.GetProperty("type").EnumerateArray().Select(v => v.GetString()!)];
    tierTypes.ShouldBe(["string", "null"]);
    string[] tierValues = [.. tier.GetProperty("enum").EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!)];
    tierValues.ShouldBe(["Basic", "Premium"]);
    string[] sizes = [.. tierProperties.GetProperty("size").GetProperty("enum").EnumerateArray().Select(v => v.GetString()!)];
    sizes.ShouldBe(["Small", "Large"]);

    JsonElement level = Schema(tools, "mh-note").GetProperty("properties").GetProperty("level");
    string[] levelTypes = [.. level.GetProperty("type").EnumerateArray().Select(v => v.GetString()!)];
    levelTypes.ShouldBe(["integer", "null"]);

    JsonElement wipe = Schema(tools, "mh-wipe").GetProperty("properties").GetProperty("retries");
    wipe.GetProperty("type").GetString().ShouldBe("integer");
    wipe.GetProperty("description").GetString().ShouldBe("Retry count. Default: 3.");
    await Task.CompletedTask;
  }

  public static async Task Should_map_endpoint_kind_and_destructive_to_annotations()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    IReadOnlyList<NuruMcpToolDefinition> tools = NuruMcpToolCatalog.CreateTools(app.Capabilities);

    ToolAnnotations query = Annotations(tools, "mh-greet");
    query.ReadOnlyHint.ShouldBe(true);
    query.IdempotentHint.ShouldBe(true);
    query.DestructiveHint.ShouldBeNull();

    ToolAnnotations idempotent = Annotations(tools, "mh-config_set");
    idempotent.ReadOnlyHint.ShouldBe(false);
    idempotent.IdempotentHint.ShouldBe(true);
    idempotent.DestructiveHint.ShouldBeNull();

    ToolAnnotations command = Annotations(tools, "mh-run");
    command.ReadOnlyHint.ShouldBe(false);
    command.IdempotentHint.ShouldBe(false);
    command.DestructiveHint.ShouldBeNull();

    ToolAnnotations wipe = Annotations(tools, "mh-wipe");
    wipe.DestructiveHint.ShouldBe(true);
    wipe.ReadOnlyHint.ShouldBe(false);

    EndpointCapability wipeEndpoint = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-wipe");
    wipeEndpoint.Destructive.ShouldBe(true);
    app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-greet {name}").Destructive.ShouldBeNull();

    ToolAnnotations unspecified = NuruMcpToolCatalog.CreateAnnotations(Endpoint("x", EndpointKind.Unspecified));
    unspecified.ReadOnlyHint.ShouldBeNull();
    unspecified.IdempotentHint.ShouldBeNull();
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

  public static async Task Should_capture_console_output_into_the_result()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    EndpointCapability echo = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-echo");

    CallToolResult result = await new NuruMcpToolInvoker(app).CallAsync(echo, null);

    result.IsError.ShouldBe(false);
    ((TextContentBlock)result.Content.Single()).Text.ShouldBe("from console");
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

  public static async Task Should_return_structured_content_when_json_is_requested()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    NuruMcpToolInvoker invoker = new(app, new NuruMcpHostOptions { ContentFormat = NuruMcpContentFormat.Json });
    IReadOnlyList<EndpointCapability> endpoints = app.Capabilities.Endpoints;

    CallToolResult report = await invoker.CallAsync(endpoints.Single(e => e.Pattern == "mh-report"), Args("""{"name":"q3"}"""));
    report.StructuredContent.ShouldNotBeNull();
    report.StructuredContent!.Value.GetProperty("name").GetString().ShouldBe("q3");
    report.StructuredContent!.Value.GetProperty("count").GetInt32().ShouldBe(2);

    CallToolResult run = await invoker.CallAsync(endpoints.Single(e => e.Pattern.StartsWith("mh-run", StringComparison.Ordinal)), Args("""{"count":3}"""));
    run.StructuredContent!.Value.GetProperty("result").GetInt32().ShouldBe(3);
    ((TextContentBlock)run.Content[0]).Text.ShouldBe("3");

    CallToolResult text = await invoker.CallAsync(endpoints.Single(e => e.Pattern == "mh-greet {name}"), Args("""{"name":"Ada"}"""));
    text.StructuredContent!.Value.GetProperty("result").GetString().ShouldBe("Hello, Ada!");

    CallToolResult unit = await invoker.CallAsync(endpoints.Single(e => e.Pattern == "mh-wipe"), Args("{}"));
    unit.StructuredContent.ShouldBeNull();

    CallToolResult plain = await new NuruMcpToolInvoker(app).CallAsync(endpoints.Single(e => e.Pattern == "mh-report"), Args("""{"name":"q3"}"""));
    plain.StructuredContent.ShouldBeNull();
  }

  public static async Task Should_keep_console_output_out_of_structured_content()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    NuruMcpToolInvoker invoker = new(app, new NuruMcpHostOptions { ContentFormat = NuruMcpContentFormat.Json });
    IReadOnlyList<EndpointCapability> endpoints = app.Capabilities.Endpoints;

    CallToolResult report = await invoker.CallAsync(endpoints.Single(e => e.Pattern == "mh-log-report"), Args("""{"name":"q4"}"""));
    report.IsError.ShouldBe(false);
    report.StructuredContent.ShouldNotBeNull();
    report.StructuredContent!.Value.GetProperty("name").GetString().ShouldBe("q4");
    ((TextContentBlock)report.Content[0]).Text.ShouldStartWith("building q4");

    CallToolResult scalar = await invoker.CallAsync(endpoints.Single(e => e.Pattern == "mh-log-text"), Args("{}"));
    scalar.IsError.ShouldBe(false);
    scalar.StructuredContent!.Value.GetProperty("result").GetString().ShouldBe("value");
    ((TextContentBlock)scalar.Content[0]).Text.ShouldBe("log line\nvalue");
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

  private static JsonElement Schema(IReadOnlyList<NuruMcpToolDefinition> tools, string name) =>
    tools.Single(t => t.Tool.Name == name).Tool.InputSchema;

  private static ToolAnnotations Annotations(IReadOnlyList<NuruMcpToolDefinition> tools, string name) =>
    tools.Single(t => t.Tool.Name == name).Tool.Annotations!;

  private static string[] Required(JsonElement schema) =>
    [.. schema.GetProperty("required").EnumerateArray().Select(v => v.GetString()!)];

  private static bool IsError(JsonElement result) =>
    result.TryGetProperty("isError", out JsonElement isError) && isError.GetBoolean();

  private static string Text(JsonElement result) =>
    string.Join("\n", result.GetProperty("content").EnumerateArray().Select(c => c.GetProperty("text").GetString()));
}

/// <summary>
/// A minimal MCP client: newline-delimited JSON-RPC over in-memory pipes to <see cref="NuruMcpServer.RunAsync(NuruApp, Stream, Stream, NuruMcpHostOptions?, CancellationToken)"/>.
/// </summary>
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

[NuruRoute("mh-wipe", Description = "Wipe the cache")]
[Destructive]
public sealed class MhWipeCommand : ICommand<Unit>
{
  [Option("retries", Description = "Retry count")]
  public int Retries { get; set; } = 3;

  public sealed class Handler : ICommandHandler<MhWipeCommand, Unit>
  {
    public Task<Unit> Handle(MhWipeCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      _ = cancellationToken;
      return Unit.Task;
    }
  }
}

public sealed record MhReport(string Name, int Count);

[Operation("mh-get-symbol", Description = "Look up a symbol", Group = "mh-symbols.read")]
public sealed class MhSymbolQuery : IQuery<string>
{
  public string Name { get; set; } = "";

  public sealed class Handler : IQueryHandler<MhSymbolQuery, string>
  {
    public Task<string> Handle(MhSymbolQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return Task.FromResult($"symbol:{query.Name}");
    }
  }
}

[NuruRoute("mh-report", Description = "Build a report")]
public sealed class MhReportQuery : IQuery<MhReport>
{
  [Option("name", Description = "Report name")]
  public string Name { get; set; } = "";

  public sealed class Handler : IQueryHandler<MhReportQuery, MhReport>
  {
    public Task<MhReport> Handle(MhReportQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return Task.FromResult(new MhReport(query.Name, query.Name.Length));
    }
  }
}

[NuruRoute("mh-log-report", Description = "Build a report and log progress")]
public sealed class MhLoggingReportQuery : IQuery<MhReport>
{
  [Option("name", Description = "Report name")]
  public string Name { get; set; } = "";

  public sealed class Handler : IQueryHandler<MhLoggingReportQuery, MhReport>
  {
    public async Task<MhReport> Handle(MhLoggingReportQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
#pragma warning disable RS0030 // The handler logs to the console directly, as a real handler might.
      await Console.Out.WriteLineAsync($"building {query.Name}").ConfigureAwait(false);
#pragma warning restore RS0030
      return new MhReport(query.Name, query.Name.Length);
    }
  }
}

[NuruRoute("mh-problem")]
public sealed class MhProblemQuery : IQuery<OneOf<string, SharedProblemDetails>>
{
  [Option("fail", Description = "Return a problem")]
  public bool Fail { get; set; }

  public sealed class Handler : IQueryHandler<MhProblemQuery, OneOf<string, SharedProblemDetails>>
  {
    public Task<OneOf<string, SharedProblemDetails>> Handle(MhProblemQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      OneOf<string, SharedProblemDetails> outcome = query.Fail
        ? new SharedProblemDetails { Type = "https://example.test/mh", Title = "Bad input", Status = 17, Detail = "too small" }
        : "ok";
      return Task.FromResult(outcome);
    }
  }
}
}
