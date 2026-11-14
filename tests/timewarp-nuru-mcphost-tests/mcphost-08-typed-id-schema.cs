#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

#region Purpose
// Pins how a typed ID (a type with its own [JsonConverter] that writes a string, plus IParsable<TSelf>) is
// published in a JSON member schema (task 526): {"type":"string"}, with "format":"uuid" when it wraps a
// Guid, at the member root, nested in an object, as array items, as dictionary values, and nullable. The
// typed IDs here have the shape the timewarp-architecture typed-ID generator emits; the tests do not
// reference that generator. The same typed IDs bind through --json-args and McpHost arguments.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.TypedIdSchema
{
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using ModelContextProtocol.Protocol;
using TimeWarp.Contracts;
using TimeWarp.Nuru;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostTypedIdSchemaTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostTypedIdSchemaTests>();

  private const string OwnerId = "6f1c1c2e-3b7a-4d38-9c1e-0a7b9f6d2e11";
  private const string MemberId = "0d9f8a52-6c4e-4f0b-8a8e-5b2f3c1d7e90";

  private static NuruApp CreateApp(TestTerminal terminal) =>
    NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tid-app")
      .Map<TidAssignCommand>()
      .Build();

  public static async Task Should_publish_a_top_level_typed_id_as_a_uuid_string()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);

    // --capabilities: the option's own schema.
    OptionCapability owner = Option(app, "Owner");
    JsonElement ownerSchema = owner.Schema!.Value;
    ownerSchema.GetProperty("type").GetString().ShouldBe("string");
    ownerSchema.GetProperty("format").GetString().ShouldBe("uuid");

    // A nullable struct member accepts null.
    TypeNames(Option(app, "Reviewer").Schema!.Value).ShouldBe(["string", "null"]);

    // McpHost inputSchema: the same schema, with the member description.
    JsonElement properties = Schema(app).GetProperty("properties");
    TypeNames(properties.GetProperty("Owner")).ShouldBe(["string"]);
    properties.GetProperty("Owner").GetProperty("format").GetString().ShouldBe("uuid");
    properties.GetProperty("Owner").GetProperty("description").GetString().ShouldBe("Who owns the group");
    TypeNames(properties.GetProperty("Reviewer")).ShouldBe(["string", "null"]);
    await Task.CompletedTask;
  }

  public static async Task Should_publish_typed_ids_nested_in_arrays_objects_and_dictionaries()
  {
    using TestTerminal terminal = new();
    NuruApp app = CreateApp(terminal);
    JsonElement properties = Schema(app).GetProperty("properties");

    // An array of typed IDs gets string items.
    JsonElement members = properties.GetProperty("Members");
    members.GetProperty("type").GetString().ShouldBe("array");
    members.GetProperty("description").GetString().ShouldBe("Members to add. Default: [].");
    JsonElement item = members.GetProperty("items");
    TypeNames(item).ShouldBe(["string"]);
    item.GetProperty("format").GetString().ShouldBe("uuid");

    // Nested in an object: description kept, nullable struct and nullable reference add null.
    JsonElement details = properties.GetProperty("Details");
    TypeNames(details).ShouldBe(["object", "null"]);
    JsonElement nested = details.GetProperty("properties");
    JsonElement approver = nested.GetProperty("Approver");
    TypeNames(approver).ShouldBe(["string"]);
    approver.GetProperty("format").GetString().ShouldBe("uuid");
    approver.GetProperty("description").GetString().ShouldBe("Who approved");
    TypeNames(nested.GetProperty("Backup")).ShouldBe(["string", "null"]);

    // A typed ID that does not wrap a Guid is a string with no format.
    JsonElement slug = nested.GetProperty("Slug");
    TypeNames(slug).ShouldBe(["string", "null"]);
    slug.TryGetProperty("format", out _).ShouldBeFalse();

    // A list of typed IDs inside the object, and dictionary values.
    JsonElement watchers = nested.GetProperty("Watchers");
    watchers.GetProperty("type").GetString().ShouldBe("array");
    TypeNames(watchers.GetProperty("items")).ShouldBe(["string"]);
    JsonElement aliases = nested.GetProperty("Aliases");
    TypeNames(aliases.GetProperty("additionalProperties")).ShouldBe(["string"]);
    aliases.GetProperty("additionalProperties").GetProperty("format").GetString().ShouldBe("uuid");

    // A string-enum converter still publishes its names.
    string[] roles = [.. nested.GetProperty("Role").GetProperty("enum").EnumerateArray().Select(v => v.GetString()!)];
    roles.ShouldBe(["Viewer", "Editor"]);
    await Task.CompletedTask;
  }

  public static async Task Should_bind_typed_ids_through_json_args_and_mcp_arguments()
  {
    string json = $$$"""
      {"Owner":"{{{OwnerId}}}","Members":["{{{MemberId}}}"],"Details":{"Approver":"{{{MemberId}}}","Slug":"blue","Watchers":["{{{OwnerId}}}"],"Aliases":{"lead":"{{{OwnerId}}}"},"Role":"Editor"}}
      """;
    string expected = $"owner:{OwnerId}|reviewer:none|members:{MemberId}|approver:{MemberId}|slug:blue|watchers:1|aliases:1|role:Editor";

    // RunAsync is intercepted on an app built in the calling method, so this app is built here.
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("tid-app")
      .Map<TidAssignCommand>()
      .Build();

    int exitCode = await app.RunAsync(["tid-assign", "--json-args", json]);
    exitCode.ShouldBe(0, terminal.ErrorOutput + terminal.Output);
    terminal.Output.ShouldContain(expected);

    NuruMcpToolInvoker invoker = new(app);
    CallToolResult result = await invoker.CallAsync(Endpoint(app), Args(json));
    result.IsError.ShouldBe(false, Text(result));
    Text(result).ShouldBe(expected);

    // A typed ID member takes a JSON string, not an object.
    CallToolResult wrongKind = await invoker.CallAsync(Endpoint(app), Args("{\"Owner\":{\"Value\":\"" + OwnerId + "\"}}"));
    wrongKind.IsError.ShouldBe(true);
    Text(wrongKind).ShouldContain("Expected string");

    // A value the typed ID does not parse is a bind error, not a crash.
    CallToolResult rejected = await invoker.CallAsync(Endpoint(app), Args("""{"Owner":"not-a-guid"}"""));
    rejected.IsError.ShouldBe(true);
  }

  private static EndpointCapability Endpoint(NuruApp app) =>
    app.Capabilities.Endpoints.Single(e => e.Pattern == "tid-assign" || e.Pattern.StartsWith("tid-assign ", StringComparison.Ordinal));

  private static OptionCapability Option(NuruApp app, string name) =>
    Endpoint(app).Options.Single(o => o.Name == name);

  private static JsonElement Schema(NuruApp app) =>
    NuruMcpToolCatalog.CreateInputSchema(Endpoint(app));

  private static string[] TypeNames(JsonElement schema)
  {
    JsonElement type = schema.GetProperty("type");
    return type.ValueKind == JsonValueKind.Array ? [.. type.EnumerateArray().Select(v => v.GetString()!)] : [type.GetString()!];
  }

  private static Dictionary<string, JsonElement> Args(string json)
  {
    using JsonDocument document = JsonDocument.Parse(json);
    return document.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
  }

  private static string Text(CallToolResult result) =>
    string.Join("\n", result.Content.OfType<TextContentBlock>().Select(c => c.Text));
}

/// <summary>Shaped like the timewarp-architecture typed-ID generator's output: a Guid Value, a converter that writes the Guid string, and IParsable.</summary>
[JsonConverter(typeof(TidPrincipalIdJsonConverter))]
public readonly record struct TidPrincipalId(Guid Value) : ISpanParsable<TidPrincipalId>
{
  public override string ToString() => Value.ToString();

  public static TidPrincipalId Parse(string s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

  public static TidPrincipalId Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => new(Guid.Parse(s, provider));

  public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out TidPrincipalId result) =>
    TryParse(s.AsSpan(), provider, out result);

  public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out TidPrincipalId result)
  {
    bool parsed = Guid.TryParse(s, provider, out Guid value);
    result = new TidPrincipalId(value);
    return parsed;
  }
}

public sealed class TidPrincipalIdJsonConverter : JsonConverter<TidPrincipalId>
{
  public override TidPrincipalId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    TidPrincipalId.TryParse(reader.GetString(), CultureInfo.InvariantCulture, out TidPrincipalId id)
      ? id
      : throw new JsonException("Not a principal id");

  public override void Write(Utf8JsonWriter writer, TidPrincipalId value, JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(writer);
    writer.WriteStringValue(value.Value);
  }
}

/// <summary>A reference-type typed ID that does not wrap a Guid.</summary>
[JsonConverter(typeof(TidSlugJsonConverter))]
public sealed record TidSlug(string Value) : IParsable<TidSlug>
{
  public override string ToString() => Value;

  public static TidSlug Parse(string s, IFormatProvider? provider) => new(s);

  public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out TidSlug result)
  {
    result = s is null ? null : new TidSlug(s);
    return result is not null;
  }
}

public sealed class TidSlugJsonConverter : JsonConverter<TidSlug>
{
  public override TidSlug Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
    new(reader.GetString() ?? throw new JsonException("Not a slug"));

  public override void Write(Utf8JsonWriter writer, TidSlug value, JsonSerializerOptions options)
  {
    ArgumentNullException.ThrowIfNull(writer);
    ArgumentNullException.ThrowIfNull(value);
    writer.WriteStringValue(value.Value);
  }
}

[JsonConverter(typeof(JsonStringEnumConverter<TidRole>))]
public enum TidRole
{
  Viewer,
  Editor
}

public sealed class TidDetails
{
  [Description("Who approved")]
  public TidPrincipalId Approver { get; set; }

  public TidPrincipalId? Backup { get; set; }

  public TidSlug? Slug { get; set; }

  public IReadOnlyList<TidPrincipalId> Watchers { get; set; } = [];

  public IReadOnlyDictionary<string, TidPrincipalId> Aliases { get; set; } = new Dictionary<string, TidPrincipalId>();

  public TidRole Role { get; set; }
}

[Operation("tid-assign", Description = "Assign members to a group")]
public sealed class TidAssignCommand : ICommand<string>
{
  [Description("Who owns the group")]
  public required TidPrincipalId Owner { get; set; }

  public TidPrincipalId? Reviewer { get; set; }

  [Description("Members to add")]
  public IReadOnlyList<TidPrincipalId> Members { get; set; } = [];

  public TidDetails? Details { get; set; }

  public sealed class Handler : IRequestHandler<TidAssignCommand, string>
  {
    public ValueTask<string> Handle(TidAssignCommand command, CancellationToken cancellationToken)
    {
      ArgumentNullException.ThrowIfNull(command);
      _ = cancellationToken;
      TidDetails details = command.Details ?? new TidDetails();
      return ValueTask.FromResult(
        $"owner:{command.Owner}|reviewer:{command.Reviewer?.ToString() ?? "none"}|members:{string.Join(",", command.Members)}"
        + $"|approver:{details.Approver}|slug:{details.Slug?.Value ?? "none"}|watchers:{details.Watchers.Count}|aliases:{details.Aliases.Count}|role:{details.Role}");
    }
  }
}
}
