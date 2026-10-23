#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

// TimeWarp.Nuru.McpHost (task 508-015): schema fidelity. A JSON member publishes the schema of the type
// its binder deserializes into; a contract carries title, destructive and open-world hints, and a host
// override wins; defaults are in both the default keyword and the description; nullable members keep
// "null" because the binder accepts an explicit null.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.SchemaFidelity
{
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TimeWarp.Amuru;
using TimeWarp.Contracts;
using TimeWarp.Nuru;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostSchemaFidelityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostSchemaFidelityTests>();

  private static NuruApp CreateApp(TestTerminal terminal)
  {
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("mf-app")
      .Map("mf-limit --count {count:int} --mode {mode?}").WithHandler((int count, string? mode) => $"{count}:{mode ?? "none"}").AsQuery().Done()
      .Map("mf-level --level {level:int?}").WithHandler((int? level) => level?.ToString(CultureInfo.InvariantCulture) ?? "none").AsQuery().Done()
      .Map<MfBatchQuery>()
      .Map<MfPurgeCommand>()
      .Map<MfAppendCommand>()
      .Map<MfPlainCommand>()
      .Build();
    return app;
  }

  public static async Task Should_publish_the_schema_of_json_members()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    JsonElement properties = Schema(app, "mf-batch").GetProperty("properties");

    // An object array: items are the element's object schema, with its required members.
    JsonElement operations = properties.GetProperty("Operations");
    operations.GetProperty("type").GetString().ShouldBe("array");
    operations.GetProperty("description").GetString().ShouldBe("Operations to run in order");
    JsonElement item = operations.GetProperty("items");
    item.GetProperty("type").GetString().ShouldBe("object");
    item.GetProperty("properties").GetProperty("Tool").GetProperty("type").GetString().ShouldBe("string");
    item.GetProperty("properties").GetProperty("Tool").GetProperty("description").GetString().ShouldBe("Tool to call");
    item.GetProperty("properties").GetProperty("Weight").GetProperty("type").GetString().ShouldBe("number");
    item.GetProperty("required").EnumerateArray().Select(v => v.GetString()).ShouldBe(["Tool"]);

    // A string-enum converter publishes its names; nested objects nest.
    string[] modes = [.. item.GetProperty("properties").GetProperty("Mode").GetProperty("enum").EnumerateArray().Select(v => v.GetString()!)];
    modes.ShouldBe(["Fast", "Thorough"]);
    JsonElement options = properties.GetProperty("Options");
    options.GetProperty("type").GetString().ShouldBe("object");
    options.GetProperty("properties").GetProperty("Limits").GetProperty("properties").GetProperty("Max").GetProperty("type").GetString().ShouldBe("integer");

    // A nullable JSON member accepts null; a non-nullable one does not.
    TypeNames(options).ShouldBe(["object"]);
    TypeNames(properties.GetProperty("Context")).ShouldBe(["object", "null"]);

    Required(Schema(app, "mf-batch")).ShouldBe(["Operations", "Options"]);
    await Task.CompletedTask;
  }

  public static async Task Should_bind_json_values_and_reject_json_in_a_string()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    EndpointCapability batch = Endpoint(app, "mf-batch");
    NuruMcpToolInvoker invoker = new(app);

    CallToolResult ok = await invoker.CallAsync(batch, Args("""
      {"Operations":[{"Tool":"a","Weight":1.5,"Mode":"Thorough"},{"tool":"b"}],"Options":{"Limits":{"Max":3}},"Context":null}
      """));
    ok.IsError.ShouldBe(false, Text(ok));
    Text(ok).ShouldBe("a:1.5:Thorough,b:0:Fast|max:3|context:none");

    CallToolResult asString = await invoker.CallAsync(batch, Args("""{"Operations":"[{\"Tool\":\"a\"}]","Options":{}}"""));
    asString.IsError.ShouldBe(true);
    Text(asString).ShouldContain("wrong JSON type");
  }

  public static async Task Should_carry_contract_annotations_and_let_the_host_override_them()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    EndpointCapability purge = Endpoint(app, "mf-purge");
    purge.Destructive.ShouldBe(true);
    purge.OpenWorld.ShouldBe(true);
    purge.Title.ShouldBe("Purge the store");

    EndpointCapability append = Endpoint(app, "mf-append");
    append.Destructive.ShouldBe(false);
    append.OpenWorld.ShouldBe(false);
    append.Title.ShouldBeNull();

    EndpointCapability plain = Endpoint(app, "mf-plain");
    plain.Destructive.ShouldBeNull();
    plain.OpenWorld.ShouldBeNull();
    plain.Title.ShouldBeNull();

    IReadOnlyList<NuruMcpToolDefinition> tools = NuruMcpToolCatalog.CreateTools(app.Capabilities);
    Tool purgeTool = tools.Single(t => t.Tool.Name == "mf-purge").Tool;
    purgeTool.Title.ShouldBe("Purge the store");
    purgeTool.Annotations!.Title.ShouldBe("Purge the store");
    purgeTool.Annotations.DestructiveHint.ShouldBe(true);
    purgeTool.Annotations.OpenWorldHint.ShouldBe(true);
    purgeTool.Annotations.ReadOnlyHint.ShouldBe(false);

    ToolAnnotations appendHints = tools.Single(t => t.Tool.Name == "mf-append").Tool.Annotations!;
    appendHints.DestructiveHint.ShouldBe(false);
    appendHints.OpenWorldHint.ShouldBe(false);

    ToolAnnotations plainHints = tools.Single(t => t.Tool.Name == "mf-plain").Tool.Annotations!;
    plainHints.DestructiveHint.ShouldBeNull();
    plainHints.OpenWorldHint.ShouldBeNull();
    plainHints.Title.ShouldBeNull();

    // The host override wins for every hint it sets; the rest stay derived.
    NuruMcpHostOptions options = new();
    options.ToolAnnotations["mf-purge"] = new ToolAnnotations { Title = "Wipe", OpenWorldHint = false };
    options.ToolAnnotations["mf-plain"] = new ToolAnnotations { DestructiveHint = true, IdempotentHint = true };
    IReadOnlyList<NuruMcpToolDefinition> overridden = NuruMcpToolCatalog.CreateTools(app.Capabilities, options);

    Tool wipe = overridden.Single(t => t.Tool.Name == "mf-purge").Tool;
    wipe.Title.ShouldBe("Wipe");
    wipe.Annotations!.Title.ShouldBe("Wipe");
    wipe.Annotations.OpenWorldHint.ShouldBe(false);
    wipe.Annotations.DestructiveHint.ShouldBe(true);

    ToolAnnotations plainOverride = overridden.Single(t => t.Tool.Name == "mf-plain").Tool.Annotations!;
    plainOverride.DestructiveHint.ShouldBe(true);
    plainOverride.IdempotentHint.ShouldBe(true);
    plainOverride.ReadOnlyHint.ShouldBe(false);
    await Task.CompletedTask;
  }

  public static async Task Should_publish_defaults_in_the_keyword_and_the_description()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    JsonElement properties = Schema(app, "mf-plain").GetProperty("properties");

    JsonElement retries = properties.GetProperty("Retries");
    retries.GetProperty("default").GetInt32().ShouldBe(3);
    retries.GetProperty("description").GetString().ShouldBe("How many times to retry. Default: 3.");

    JsonElement label = properties.GetProperty("Label");
    label.GetProperty("default").GetString().ShouldBe("main");
    label.GetProperty("description").GetString().ShouldBe("Default: \"main\".");

    properties.GetProperty("Force").GetProperty("description").GetString().ShouldBe("Skip the prompt. Default: false.");

    NuruMcpToolCatalog.DescribeDefault("Count", "3").ShouldBe("Count. Default: 3.");
    NuruMcpToolCatalog.DescribeDefault("Count?", null).ShouldBe("Count?");
    NuruMcpToolCatalog.DescribeDefault(null, null).ShouldBeNull();
    await Task.CompletedTask;
  }

  public static async Task Should_keep_null_for_nullable_members_the_binder_accepts()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    JsonElement level = Schema(app, "mf-level").GetProperty("properties").GetProperty("level");
    TypeNames(level).ShouldBe(["integer", "null"]);

    NuruMcpToolInvoker invoker = new(app);
    CallToolResult explicitNull = await invoker.CallAsync(Endpoint(app, "mf-level"), Args("""{"level":null}"""));
    explicitNull.IsError.ShouldBe(false, Text(explicitNull));
    Text(explicitNull).ShouldBe("none");

    CallToolResult omitted = await invoker.CallAsync(Endpoint(app, "mf-level"), Args("{}"));
    Text(omitted).ShouldBe("none");

    // A non-nullable member is published without null, and the binder rejects one.
    TypeNames(Schema(app, "mf-limit").GetProperty("properties").GetProperty("count")).ShouldBe(["integer"]);
    CallToolResult rejected = await invoker.CallAsync(Endpoint(app, "mf-limit"), Args("""{"count":null}"""));
    rejected.IsError.ShouldBe(true);
  }

  public static async Task Should_serve_the_contracts_sample_schema_and_call_it_over_stdio()
  {
    // --capabilities builds the sample first, so the MCP launch below does not compile.
    CommandOutput capabilities = await Shell.Builder("dotnet")
      .WithArguments(["run", SamplePath(), "--", "--capabilities"])
      .WithNoValidation()
      .CaptureAsync();
    capabilities.ExitCode.ShouldBe(0, capabilities.Stderr);

    StdioClientTransport transport = new(new StdioClientTransportOptions
    {
      Name = "calculator-contracts",
      Command = "dotnet",
      Arguments = ["run", SamplePath(), "--", "mcp"],
      ShutdownTimeout = TimeSpan.FromSeconds(10)
    });
    using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(3));
    await using McpClient client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);

    IList<McpClientTool> tools = await client.ListToolsAsync();
    Tool total = tools.Single(t => t.Name == "calc_total").ProtocolTool;
    total.Title.ShouldBe("Total numbers");
    total.Annotations!.OpenWorldHint.ShouldBe(false);
    total.Annotations.ReadOnlyHint.ShouldBe(true);

    JsonElement properties = total.InputSchema.GetProperty("properties");
    JsonElement notes = properties.GetProperty("Notes");
    notes.GetProperty("type").GetString().ShouldBe("array");
    notes.GetProperty("items").GetProperty("properties").GetProperty("Label").GetProperty("type").GetString().ShouldBe("string");
    notes.GetProperty("default").GetArrayLength().ShouldBe(0);
    JsonElement format = properties.GetProperty("Format");
    TypeNames(format).ShouldBe(["object", "null"]);
    format.GetProperty("properties").GetProperty("Decimals").GetProperty("type").GetString().ShouldBe("integer");

    CallToolResult result = await client.CallToolAsync(new CallToolRequestParams
    {
      Name = "calc_total",
      Arguments = Args("""{"Values":[1,2],"Notes":[{"Label":"tax","Amount":1.5}],"Format":{"Decimals":1,"Prefix":"$"}}""")
    });
    result.IsError.ShouldBe(false, Text(result));
    Text(result).ShouldBe("total:$4.5|average:1.5|notes:1");
  }

  private static string SamplePath([CallerFilePath] string thisFile = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "samples", "endpoints", "02-calculator-contracts", "calculator-contracts.cs"));

  private static EndpointCapability Endpoint(NuruApp app, string pattern) =>
    app.Capabilities.Endpoints.Single(e => e.Pattern == pattern || e.Pattern.StartsWith(pattern + " ", StringComparison.Ordinal));

  private static JsonElement Schema(NuruApp app, string pattern) =>
    NuruMcpToolCatalog.CreateInputSchema(Endpoint(app, pattern));

  private static string[] TypeNames(JsonElement schema)
  {
    JsonElement type = schema.GetProperty("type");
    return type.ValueKind == JsonValueKind.Array ? [.. type.EnumerateArray().Select(v => v.GetString()!)] : [type.GetString()!];
  }

  private static string[] Required(JsonElement schema) =>
    [.. schema.GetProperty("required").EnumerateArray().Select(v => v.GetString()!)];

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}

[JsonConverter(typeof(JsonStringEnumConverter<MfMode>))]
public enum MfMode
{
  Fast,
  Thorough
}

public sealed class MfOperation
{
  [Description("Tool to call")]
  public required string Tool { get; set; }

  public double Weight { get; set; }

  public MfMode Mode { get; set; }
}

public sealed class MfLimits
{
  public int Max { get; set; }
}

public sealed class MfOptions
{
  public MfLimits Limits { get; set; } = new();
}

public sealed class MfContext
{
  public string Name { get; set; } = "";
}

[Operation("mf-batch", Description = "Run several operations")]
public sealed class MfBatchQuery : IQuery<string>
{
  [Description("Operations to run in order")]
  public required IReadOnlyList<MfOperation> Operations { get; set; }

  public required MfOptions Options { get; set; }

  public MfContext? Context { get; set; }

  public sealed class Handler : IQueryHandler<MfBatchQuery, string>
  {
    public Task<string> Handle(MfBatchQuery query, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(query);
      _ = cancellationToken;
      string operations = string.Join(",", query.Operations.Select(o => $"{o.Tool}:{o.Weight.ToString(CultureInfo.InvariantCulture)}:{o.Mode}"));
      string max = query.Options.Limits.Max.ToString(CultureInfo.InvariantCulture);
      return Task.FromResult($"{operations}|max:{max}|context:{query.Context?.Name ?? "none"}");
    }
  }
}

[Operation("mf-purge", Description = "Delete every stored entry", Title = "Purge the store")]
[Destructive]
[OpenWorld]
public sealed class MfPurgeCommand : ICommand<string>
{
  public sealed class Handler : ICommandHandler<MfPurgeCommand, string>
  {
    public Task<string> Handle(MfPurgeCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("purged");
    }
  }
}

[Operation("mf-append", Description = "Append an entry")]
[DisplayName("Append should not become the title")]
[Destructive(false)]
[OpenWorld(false)]
public sealed class MfAppendCommand : ICommand<string>
{
  public sealed class Handler : ICommandHandler<MfAppendCommand, string>
  {
    public Task<string> Handle(MfAppendCommand command, CancellationToken cancellationToken)
    {
      _ = cancellationToken;
      return Task.FromResult("appended");
    }
  }
}

[Operation("mf-plain", Description = "A command with defaults", Title = "   ")]
[DisplayName("Plain display name")]
public sealed class MfPlainCommand : ICommand<string>
{
  [Description("How many times to retry")]
  public int Retries { get; set; } = 3;

  public string Label { get; set; } = "main";

  [Description("Skip the prompt")]
  public bool Force { get; set; } = false;

  public sealed class Handler : ICommandHandler<MfPlainCommand, string>
  {
    public Task<string> Handle(MfPlainCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      _ = cancellationToken;
      return Task.FromResult($"{command.Label}:{command.Retries.ToString(CultureInfo.InvariantCulture)}");
    }
  }
}
}
