#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 464: Examples support for route help and capabilities.
// Validates that [NuruRouteExample] (Endpoint DSL) and .WithExample() (Fluent DSL) surface
// as an "examples" array on the endpoint's capabilities JSON entry, and that the property
// is omitted entirely (not emitted as null/empty) for routes with no examples - keeping the
// JSON shape byte-identical to before this feature existed for unannotated routes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Capabilities
{

[TestTag("Capabilities")]
public class CapabilitiesExamplesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesExamplesTests>();

  public static async Task Should_roundtrip_example_capability_dto()
  {
    // Arrange
    CapabilitiesResponse original = new()
    {
      Name = "mytool",
      Version = "1.0.0",
      Endpoints =
      [
        new EndpointCapability
        {
          Pattern = "cap06-deploy {env}",
          GroupPath = [],
          Kind = EndpointKind.Command,
          Parameters = [],
          Options = [],
          Examples =
          [
            new ExampleCapability { Command = "cap06-deploy prod", Description = "Deploy to production" },
            new ExampleCapability { Command = "cap06-deploy staging --dry-run" }
          ]
        }
      ]
    };

    // Act
    string json = System.Text.Json.JsonSerializer.Serialize(original, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    CapabilitiesResponse? roundtripped = System.Text.Json.JsonSerializer.Deserialize(json, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    roundtripped.ShouldNotBeNull();
    EndpointCapability endpoint = roundtripped.Endpoints[0];
    endpoint.Examples.ShouldNotBeNull();
    endpoint.Examples!.Count.ShouldBe(2);
    endpoint.Examples[0].Command.ShouldBe("cap06-deploy prod");
    endpoint.Examples[0].Description.ShouldBe("Deploy to production");
    endpoint.Examples[1].Command.ShouldBe("cap06-deploy staging --dry-run");
    endpoint.Examples[1].Description.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_include_examples_array_for_endpoint_declared_via_attribute()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<Cap06DeployEndpoint>()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    response.ShouldNotBeNull();
    EndpointCapability deploy = response.Endpoints.First(e => e.Pattern.Contains("cap06-deploy"));
    deploy.Examples.ShouldNotBeNull("Endpoint with [NuruRouteExample] should have Examples");
    deploy.Examples!.Count.ShouldBe(2);
    deploy.Examples.ShouldContain(e => e.Command == "cap06-deploy prod" && e.Description == "Deploy to production");
    deploy.Examples.ShouldContain(e => e.Command == "cap06-deploy staging --dry-run" && e.Description == null);
  }

  public static async Task Should_include_examples_array_for_fluent_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("cap06-fluent-cmd")
        .WithHandler(() => "ok")
        .WithExample("cap06-fluent-cmd --verbose", "Run verbosely")
        .Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    response.ShouldNotBeNull();
    EndpointCapability endpoint = response.Endpoints.First(e => e.Pattern.Contains("cap06-fluent-cmd"));
    endpoint.Examples.ShouldNotBeNull();
    endpoint.Examples!.Count.ShouldBe(1);
    endpoint.Examples[0].Command.ShouldBe("cap06-fluent-cmd --verbose");
    endpoint.Examples[0].Description.ShouldBe("Run verbosely");
  }

  public static async Task Should_omit_examples_property_for_route_with_no_examples()
  {
    // Arrange - no [NuruRouteExample] / .WithExample() at all
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("cap06-status").WithHandler(() => "ok").WithDescription("Show status").Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;

    // Assert - the "examples" key must not appear at all in the JSON for this endpoint
    // (DefaultIgnoreCondition.WhenWritingNull on the serializer context omits null properties).
    using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(output);
    System.Text.Json.JsonElement endpoints = doc.RootElement.GetProperty("endpoints");
    System.Text.Json.JsonElement status = endpoints.EnumerateArray()
      .First(e => e.GetProperty("pattern").GetString()!.Contains("cap06-status"));

    status.TryGetProperty("examples", out _).ShouldBeFalse("examples property should be omitted when route has no examples");

    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(output, CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);
    response.ShouldNotBeNull();
    EndpointCapability endpoint = response.Endpoints.First(e => e.Pattern.Contains("cap06-status"));
    endpoint.Examples.ShouldBeNull();
  }
}

[NuruRoute("cap06-deploy", Description = "Deploy to an environment")]
[NuruRouteExample("cap06-deploy prod", Description = "Deploy to production")]
[NuruRouteExample("cap06-deploy staging --dry-run")]
public sealed class Cap06DeployEndpoint : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<Cap06DeployEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(Cap06DeployEndpoint command, CancellationToken cancellationToken) => default;
  }
}

} // namespace TimeWarp.Nuru.Tests.Capabilities
