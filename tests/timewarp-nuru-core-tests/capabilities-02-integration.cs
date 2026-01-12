#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Integration tests for --capabilities route

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.CapabilitiesIntegration
{

[TestTag("Capabilities")]
public class CapabilitiesIntegrationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CapabilitiesIntegrationTests>();

  public static async Task Should_output_valid_json_for_capabilities_route()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("mycommand").WithHandler(() => "ok").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--capabilities"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("\"commands\":").ShouldBeTrue("Should contain commands array");
    terminal.OutputContains("\"pattern\":").ShouldBeTrue("Should contain pattern field");
    terminal.OutputContains("\"messageType\":").ShouldBeTrue("Should contain messageType field");
  }

  public static async Task Should_include_user_commands_in_capabilities()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env}").WithHandler((string env) => $"Deployed to {env}").WithDescription("Deploy to environment").Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("deploy {env}").ShouldBeTrue("Should contain user command pattern");
    terminal.OutputContains("Deploy to environment").ShouldBeTrue("Should contain user command description");
  }

  public static async Task Should_exclude_hidden_routes_from_capabilities()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("mycommand").WithHandler(() => "user command").WithDescription("User command").Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert - Built-in hidden routes should not appear
    terminal.OutputContains("\"--capabilities\"").ShouldBeFalse("Should not contain --capabilities");
    terminal.OutputContains("\"--help\"").ShouldBeFalse("Should not contain --help");
    terminal.OutputContains("\"--version,-v\"").ShouldBeFalse("Should not contain --version");
    terminal.OutputContains("\"exit\"").ShouldBeFalse("Should not contain exit");
    terminal.OutputContains("\"quit\"").ShouldBeFalse("Should not contain quit");
    terminal.OutputContains("\"__complete\"").ShouldBeFalse("Should not contain __complete");

    // But user command should appear
    terminal.OutputContains("mycommand").ShouldBeTrue("Should contain user command");
  }

  public static async Task Should_show_correct_message_type_for_query_command()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "status").WithDescription("Show status").AsQuery().Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"messageType\": \"query\"").ShouldBeTrue("Should show query message type");
  }

  public static async Task Should_show_correct_message_type_for_idempotent_command()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("set {key} {value}").WithHandler((string key, string value) => $"{key}={value}").WithDescription("Set a configuration value").AsIdempotentCommand().Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"messageType\": \"idempotent-command\"").ShouldBeTrue("Should show idempotent-command message type");
  }

  public static async Task Should_include_typed_parameter_info()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("listen {port:int}").WithHandler((int port) => $"Listening on {port}").WithDescription("Listen on port").Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"name\": \"port\"").ShouldBeTrue("Should contain parameter name");
    terminal.OutputContains("\"type\": \"int\"").ShouldBeTrue("Should contain parameter type");
  }

  public static async Task Should_include_option_info_with_alias()
  {
    // Arrange
    using TestTerminal terminal = new();

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --output,-o {path}").WithHandler((string path) => $"Output to {path}").WithDescription("Build project").Done()
      .Build();

    // Act
    await app.RunAsync(["--capabilities"]);

    // Assert
    terminal.OutputContains("\"name\": \"output\"").ShouldBeTrue("Should contain option name");
    terminal.OutputContains("\"alias\": \"o\"").ShouldBeTrue("Should contain option alias");
  }

  public static async Task Should_register_capabilities_route_by_default()
  {
    // Arrange & Act - NuruApp.CreateBuilder() calls UseAllExtensions() which registers --capabilities
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);

    // Assert - Should have --capabilities route (same pattern as --version tests)
    bool hasCapabilitiesRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--capabilities"));

    hasCapabilitiesRoute.ShouldBeTrue("--capabilities route should be registered by CreateBuilder()");

    await Task.CompletedTask;
  }

  public static async Task Should_not_register_capabilities_route_when_disabled()
  {
    // Arrange & Act - DisableCapabilitiesRoute = true should prevent auto-registration
    NuruAppBuilder builder = NuruApp.CreateBuilder([], new NuruAppOptions
    {
      DisableCapabilitiesRoute = true
    });

    // Assert - Should NOT have --capabilities route
    bool hasCapabilitiesRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--capabilities"));

    hasCapabilitiesRoute.ShouldBeFalse("DisableCapabilitiesRoute = true should not auto-register --capabilities route");

    await Task.CompletedTask;
  }

  public static async Task Should_allow_manual_capabilities_route_registration()
  {
    // Arrange - Start with capabilities disabled, then manually add
    NuruAppBuilder builder = NuruApp.CreateBuilder([], new NuruAppOptions
    {
      DisableCapabilitiesRoute = true
    });
    builder.AddCapabilitiesRoute();

    // Assert - Should now have --capabilities route after manual registration
    bool hasCapabilitiesRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--capabilities"));

    hasCapabilitiesRoute.ShouldBeTrue("AddCapabilitiesRoute() should register --capabilities route");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.CapabilitiesIntegration
