#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

// Roundtrip tests: serialize then deserialize CapabilitiesResponse

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesRoundtrip
{

public enum DeployEnvironment { Development, Staging, Production }

[TestTag("Capabilities")]
public class CapabilitiesRoundtripTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesRoundtripTests>();

  public static async Task Should_roundtrip_capabilities_response()
  {
    // Arrange
    CapabilitiesResponse original = new()
    {
      Name = "mytool",
      Version = "1.2.3",
      Description = "A test tool",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "deploy {env}",
          GroupPath = ["ops"],
          Aliases = ["dep"],
          Description = "Deploy to environment",
          Kind = EndpointKind.Command,
          Parameters =
          [
            new ParameterCapability
            {
              Name = "env",
              Type = "string",
              Required = true,
              IsCatchAll = false
            }
          ],
          Options = []
        }
      ]
    };

    // Act: serialize then deserialize
    string json = System.Text.Json.JsonSerializer.Serialize(original, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    CapabilitiesResponse? roundtripped = System.Text.Json.JsonSerializer.Deserialize(json, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    roundtripped.ShouldNotBeNull();
    roundtripped.Name.ShouldBe("mytool");
    roundtripped.Version.ShouldBe("1.2.3");
    roundtripped.Description.ShouldBe("A test tool");
    roundtripped.Endpoints.Count.ShouldBe(1);

    EndpointCapability endpoint = roundtripped.Endpoints[0];
    endpoint.Pattern.ShouldBe("deploy {env}");
    endpoint.GroupPath.ShouldBe(["ops"]);
    endpoint.Aliases.ShouldBe(["dep"]);
    endpoint.Kind.ShouldBe(EndpointKind.Command);
    endpoint.Parameters.Count.ShouldBe(1);
    endpoint.Options.Count.ShouldBe(0);

    ParameterCapability param = endpoint.Parameters[0];
    param.Name.ShouldBe("env");
    param.Type.ShouldBe("string");
    param.Required.ShouldBeTrue();
    param.IsCatchAll.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_roundtrip_all_endpoint_kinds()
  {
    // Arrange
    EndpointKind[] kinds = [EndpointKind.Query, EndpointKind.Command, EndpointKind.IdempotentCommand, EndpointKind.Unspecified];

    foreach (EndpointKind kind in kinds)
    {
      CapabilitiesResponse original = new()
      {
        Name = "tool",
        Version = "1.0.0",
        Endpoints =
        [
          new EndpointCapability
          {
            Pattern = "cmd",
            GroupPath = [],
            Kind = kind,
            Parameters = [],
            Options = []
          }
        ]
      };

      // Act
      string json = System.Text.Json.JsonSerializer.Serialize(original, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
      CapabilitiesResponse? roundtripped = System.Text.Json.JsonSerializer.Deserialize(json, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

      // Assert
      roundtripped.ShouldNotBeNull();
      roundtripped.Endpoints[0].Kind.ShouldBe(kind, $"Kind {kind} should roundtrip correctly");
    }

    await Task.CompletedTask;
  }

  public static async Task Should_parse_capabilities_output_as_valid_json()
  {
    // Integration: run --capabilities and parse output as JSON
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "ok").AsQuery().Done()
      .Build();

    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;

    // Should not throw
    using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(output);
    doc.RootElement.GetProperty("name").GetString().ShouldNotBeNullOrEmpty();
    doc.RootElement.GetProperty("version").GetString().ShouldNotBeNullOrEmpty();
    doc.RootElement.GetProperty("endpoints").ValueKind.ShouldBe(System.Text.Json.JsonValueKind.Array);

    await Task.CompletedTask;
  }

  public static async Task Should_deserialize_capabilities_output_to_dto()
  {
    // Integration: run --capabilities and deserialize to CapabilitiesResponse
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"Hello {name}").WithDescription("Greet someone").AsQuery().Done()
      .Build();

    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    response.ShouldNotBeNull();
    response.Endpoints.Count.ShouldBeGreaterThan(0);

    EndpointCapability greet = response.Endpoints.First(e => e.Pattern.Contains("greet"));
    greet.Kind.ShouldBe(EndpointKind.Query);
    greet.Description.ShouldBe("Greet someone");
    greet.Parameters.Count.ShouldBe(1);
    greet.Parameters[0].Name.ShouldBe("name");

    await Task.CompletedTask;
  }

  public static async Task Should_include_allowed_values_for_enum_parameter()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {env}").WithHandler((DeployEnvironment env) => $"Deploying to {env}").Done()
      .Build();

    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    response.ShouldNotBeNull();
    EndpointCapability deploy = response.Endpoints.First(e => e.Pattern.Contains("deploy"));
    deploy.Options.Count.ShouldBe(1);

    OptionCapability envOption = deploy.Options[0];
    envOption.AllowedValues.ShouldNotBeNull("Enum option should have AllowedValues");
    envOption.AllowedValues!.ShouldContain("Development");
    envOption.AllowedValues!.ShouldContain("Staging");
    envOption.AllowedValues!.ShouldContain("Production");

    await Task.CompletedTask;
  }

  public static async Task Should_not_include_allowed_values_for_string_parameter()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}").WithHandler((string name) => $"Hello {name}").Done()
      .Build();

    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    response.ShouldNotBeNull();
    EndpointCapability greet = response.Endpoints.First(e => e.Pattern.Contains("greet"));
    greet.Parameters[0].AllowedValues.ShouldBeNull("String parameter should not have AllowedValues");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesRoundtrip
