#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcphost/timewarp-nuru-mcphost.csproj

// TimeWarp.Nuru.McpHost (task 508-004): the calculator sample's `mcp` route, driven by a real MCP
// client over stdio, publishes the same contract its `--capabilities` prints: one tool per endpoint,
// same names, descriptions, members, types, required set, and allowed values.

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.McpHost.CalculatorParity
{
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using TimeWarp.Amuru;
using TimeWarp.Nuru.McpHost;

[TestTag("McpHost")]
public class McpHostCalculatorParityTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<McpHostCalculatorParityTests>();

  public static async Task Should_publish_the_capabilities_contract_as_tools()
  {
    // --capabilities builds the sample first, so the MCP launch below does not compile.
    CapabilitiesResponse catalog = await ReadCapabilitiesAsync();
    catalog.Endpoints.Count.ShouldBeGreaterThan(0);

    await using McpClient client = await StartMcpAsync();
    IList<McpClientTool> tools = await client.ListToolsAsync();

    client.ServerInfo.Name.ShouldBe(catalog.Name);
    tools.Count.ShouldBe(catalog.Endpoints.Count);

    foreach (EndpointCapability endpoint in catalog.Endpoints)
    {
      string name = NuruMcpToolCatalog.GetToolName(endpoint);
      McpClientTool tool = tools.Single(t => t.Name == name);
      Tool protocol = tool.ProtocolTool;

      protocol.Description.ShouldBe(endpoint.Description);

      // The schema the server sends is the one the catalog projects to.
      JsonElement.DeepEquals(protocol.InputSchema, NuruMcpToolCatalog.CreateInputSchema(endpoint)).ShouldBeTrue(name);

      // And that projection is the catalog's members, member for member.
      JsonElement properties = protocol.InputSchema.GetProperty("properties");
      string[] members = [.. endpoint.Parameters.Select(p => p.Name), .. endpoint.Options.Select(o => o.Name)];
      properties.EnumerateObject().Select(p => p.Name).ShouldBe(members, name);

      string[] required =
      [
        .. endpoint.Parameters.Where(p => p.Required).Select(p => p.Name),
        .. endpoint.Options.Where(o => o.Required && !o.IsFlag).Select(o => o.Name)
      ];
      string[] schemaRequired = protocol.InputSchema.TryGetProperty("required", out JsonElement r)
        ? [.. r.EnumerateArray().Select(v => v.GetString()!)]
        : [];
      schemaRequired.ShouldBe(required, name);
      protocol.InputSchema.GetProperty("additionalProperties").GetBoolean().ShouldBeFalse();

      foreach (ParameterCapability parameter in endpoint.Parameters)
        AssertMember(properties.GetProperty(parameter.Name), parameter.Type, parameter.IsCatchAll, parameter.AllowedValues, $"{name}.{parameter.Name}");

      foreach (OptionCapability option in endpoint.Options)
        AssertMember(properties.GetProperty(option.Name), option.IsFlag ? "bool" : option.Type, option.IsRepeated, option.AllowedValues, $"{name}.{option.Name}");

      ToolAnnotations annotations = protocol.Annotations!;
      annotations.ReadOnlyHint.ShouldBe(endpoint.Kind == EndpointKind.Query, name);
      annotations.IdempotentHint.ShouldBe(endpoint.Kind is EndpointKind.Query or EndpointKind.IdempotentCommand, name);
      annotations.DestructiveHint.ShouldBe(endpoint.Destructive, name);
    }
  }

  public static async Task Should_call_calculator_tools_over_stdio()
  {
    await ReadCapabilitiesAsync();
    await using McpClient client = await StartMcpAsync();

    CallToolResult add = await client.CallToolAsync(Call("add", """{"x":2,"y":3.5}"""));
    add.IsError.ShouldBe(false);
    Text(add).ShouldBe("5.5");

    // factorial writes with Console.WriteLine; the host captures it into the result, off the protocol stream.
    CallToolResult factorial = await client.CallToolAsync(Call("factorial", """{"n":5}"""));
    factorial.IsError.ShouldBe(false);
    Text(factorial).ShouldBe("5! = 120");

    CallToolResult unknown = await client.CallToolAsync(Call("add", """{"x":2,"y":3,"z":4}"""));
    unknown.IsError.ShouldBe(true);
    Text(unknown).ShouldContain("Unknown key 'z'");

    CallToolResult stats = await client.CallToolAsync(Call("stats", """{"values":["1","2","3"]}"""));
    stats.IsError.ShouldBe(false);
    Text(stats).ShouldContain("\"sum\"", Case.Insensitive);
  }

  private static void AssertMember(JsonElement schema, string catalogType, bool isArray, IReadOnlyList<string>? allowedValues, string label)
  {
    JsonElement scalar = schema;
    if (isArray || catalogType.EndsWith("[]", StringComparison.Ordinal))
    {
      schema.GetProperty("type").GetString().ShouldBe("array", label);
      scalar = schema.GetProperty("items");
    }

    string expected = NuruMcpToolCatalog.GetJsonSchemaType(catalogType.Replace("[]", "", StringComparison.Ordinal));
    JsonElement type = scalar.GetProperty("type");
    string actual = type.ValueKind == JsonValueKind.Array ? type[0].GetString()! : type.GetString()!;
    actual.ShouldBe(expected, label);

    if (allowedValues is { Count: > 0 })
    {
      string[] values = [.. scalar.GetProperty("enum").EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!)];
      values.ShouldBe([.. allowedValues], label);
    }
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

  private static async Task<CapabilitiesResponse> ReadCapabilitiesAsync()
  {
    CommandOutput output = await Shell.Builder("dotnet")
      .WithArguments(["run", CalculatorPath(), "--", "--capabilities"])
      .WithNoValidation()
      .CaptureAsync();
    output.ExitCode.ShouldBe(0, output.Stderr);
    return JsonSerializer.Deserialize(output.Stdout, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse)!;
  }

  private static async Task<McpClient> StartMcpAsync()
  {
    StdioClientTransport transport = new(new StdioClientTransportOptions
    {
      Name = "calculator",
      Command = "dotnet",
      Arguments = ["run", CalculatorPath(), "--", "mcp"],
      ShutdownTimeout = TimeSpan.FromSeconds(10)
    });
    using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(3));
    return await McpClient.CreateAsync(transport, cancellationToken: timeout.Token);
  }

  private static string CalculatorPath([CallerFilePath] string thisFile = "") =>
    Path.GetFullPath(Path.Combine(Path.GetDirectoryName(thisFile)!, "..", "..", "samples", "endpoints", "02-calculator", "calculator.cs"));
}
}
