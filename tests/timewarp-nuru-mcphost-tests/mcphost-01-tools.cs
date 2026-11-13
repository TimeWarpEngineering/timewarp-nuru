#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

#region Purpose
// Pins TimeWarp.Nuru.McpHost (task 508-004): the catalog becomes MCP tools (names, schema, annotations,
// structured content) through the public binder.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.Tools
{
using System.Text.Json;
using ModelContextProtocol.Protocol;
using TimeWarp.Contracts;
using TimeWarp.Nuru;
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
      .Map<MhLoggingReportQuery>()
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
    wipe.Title.ShouldBe("Wipe the cache");

    EndpointCapability wipeEndpoint = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-wipe");
    wipeEndpoint.Destructive.ShouldBe(true);
    wipeEndpoint.Title.ShouldBe("Wipe the cache");
    app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-greet {name}").Destructive.ShouldBeNull();

    ToolAnnotations unspecified = NuruMcpToolCatalog.CreateAnnotations(Endpoint("x", EndpointKind.Unspecified));
    unspecified.ReadOnlyHint.ShouldBeNull();
    unspecified.IdempotentHint.ShouldBeNull();
    await Task.CompletedTask;
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

  public static async Task Should_capture_console_output_into_the_result()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    EndpointCapability echo = app.Capabilities.Endpoints.Single(e => e.Pattern == "mh-echo");

    CallToolResult result = await new NuruMcpToolInvoker(app).CallAsync(echo, null);

    result.IsError.ShouldBe(false);
    ((TextContentBlock)result.Content.Single()).Text.ShouldBe("from console");
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
}

[NuruRoute("mh-wipe", Description = "Wipe the cache")]
[System.ComponentModel.DisplayName("Wipe the cache")]
[Destructive]
public sealed class MhWipeCommand : ICommand<Unit>
{
  [Option("retries", Description = "Retry count")]
  public int Retries { get; set; } = 3;

  public sealed class Handler : IRequestHandler<MhWipeCommand, Unit>
  {
    public ValueTask<Unit> Handle(MhWipeCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      _ = cancellationToken;
      return Unit.ValueTask;
    }
  }
}

public sealed record MhReport(string Name, int Count);

[NuruRoute("mh-report", Description = "Build a report")]
public sealed class MhReportQuery : IQuery<MhReport>
{
  [Option("name", Description = "Report name")]
  public string Name { get; set; } = "";

  public sealed class Handler : IRequestHandler<MhReportQuery, MhReport>
  {
    public ValueTask<MhReport> Handle(MhReportQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      return ValueTask.FromResult(new MhReport(query.Name, query.Name.Length));
    }
  }
}

[NuruRoute("mh-log-report", Description = "Build a report and log progress")]
public sealed class MhLoggingReportQuery : IQuery<MhReport>
{
  [Option("name", Description = "Report name")]
  public string Name { get; set; } = "";

  public sealed class Handler : IRequestHandler<MhLoggingReportQuery, MhReport>
  {
    public async ValueTask<MhReport> Handle(MhLoggingReportQuery query, CancellationToken cancellationToken)
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
}
