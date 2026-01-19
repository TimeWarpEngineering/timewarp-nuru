#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.Integration
{

[TestTag("Completion")]
public class IntegrationEnableDynamicTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<IntegrationEnableDynamicTests>();

  public static async Task Should_register_complete_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => { }).AsQuery().Done();

    // Act
    builder.EnableCompletion();

    // Assert - Should have __complete route
    bool hasCompleteRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    hasCompleteRoute.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_register_generate_completion_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder.UseTerminal(terminal);

    // Act
    builder.EnableCompletion();

    // Assert - Should have --generate-completion route
    bool hasGenerateRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--generate-completion"));

    hasGenerateRoute.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_return_builder_for_fluent_chaining()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder.UseTerminal(terminal);

    // Act
    NuruAppBuilder result = builder.EnableCompletion();

    // Assert
    result.ShouldBeSameAs(builder);

    await Task.CompletedTask;
  }

  public static async Task Should_configure_registry_via_callback()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder
      .UseTerminal(terminal)
      .Map("deploy {env}").WithHandler((string env) => 0).AsCommand().Done();

    bool registryConfigured = false;

    // Act
    builder.EnableCompletion(configure: registry =>
    {
      registryConfigured = true;
      TestCompletionSource source = new(["production", "staging"]);
      registry.RegisterForParameter("env", source);
    });

    // Assert
    registryConfigured.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_complete_route_accept_index_and_words()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => { }).AsQuery().Done()
      .EnableCompletion();

    // Act - Find the __complete endpoint and verify its signature
    Endpoint? completeEndpoint = builder.EndpointCollection.FirstOrDefault(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    // Assert
    completeEndpoint.ShouldNotBeNull();
    // Should have: __complete {index:int} {*words}
    completeEndpoint.CompiledRoute.PositionalMatchers.Count.ShouldBeGreaterThanOrEqualTo(2);

    // First matcher is literal "__complete"
    completeEndpoint.CompiledRoute.PositionalMatchers[0].ShouldBeOfType<LiteralMatcher>();

    // Second matcher is parameter for index
    completeEndpoint.CompiledRoute.PositionalMatchers[1].ShouldBeOfType<ParameterMatcher>();

    await Task.CompletedTask;
  }

  public static async Task Should_have_catchall_for_words_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder
      .UseTerminal(terminal)
      .EnableCompletion();

    // Act
    Endpoint? completeEndpoint = builder.EndpointCollection.FirstOrDefault(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    // Assert - Should have a catch-all parameter
    completeEndpoint.ShouldNotBeNull();
    completeEndpoint.CompiledRoute.HasCatchAll.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_throw_on_null_builder()
  {
    // Arrange
    NuruAppBuilder? nullBuilder = null;

    // Act & Assert
    Should.Throw<ArgumentNullException>(() => nullBuilder!.EnableCompletion());

    await Task.CompletedTask;
  }

  public static async Task Should_execute_complete_route_and_return_candidates()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => { }).AsQuery().Done()
      .Map("version").WithHandler(() => { }).AsQuery().Done()
      .EnableCompletion()
      .Build();

    // Act - Execute the __complete route
    int exitCode = await app.RunAsync(["__complete", "1", "app"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("status").ShouldBeTrue();
    terminal.OutputContains("version").ShouldBeTrue();
    terminal.OutputContains(":4").ShouldBeTrue(); // Directive
  }

  public static async Task Should_use_configured_sources_in_complete_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy {env}").WithHandler((string env) => 0).AsCommand().Done()
      .EnableCompletion(configure: registry =>
      {
        TestCompletionSource source = new(["production", "staging", "development"]);
        registry.RegisterForParameter("env", source);
      })
      .Build();

    // Act - Complete the env parameter
    int exitCode = await app.RunAsync(["__complete", "2", "app", "deploy"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("production").ShouldBeTrue();
    terminal.OutputContains("staging").ShouldBeTrue();
    terminal.OutputContains("development").ShouldBeTrue();
  }

  // TODO: #387 - Restore this test after fixing enum option parameter handling
  // public static async Task Should_auto_register_enum_sources()
  // {
  //   // Arrange
  //   using TestTerminal terminal = new();
  //   NuruCoreApp app = NuruApp.CreateBuilder([])
  //     .UseTerminal(terminal)
  //     .Map("deploy {env} --mode {mode}").WithHandler((string env, DeploymentMode mode) => 0).AsCommand().Done()
  //     .EnableCompletion(configure: registry =>
  //     {
  //       // Register enum source for the type
  //       EnumCompletionSource<DeploymentMode> enumSource = new();
  //       registry.RegisterForType(typeof(DeploymentMode), enumSource);
  //     })
  //     .Build();
  //
  //   // Act - Complete the mode parameter (after --mode option)
  //   int exitCode = await app.RunAsync(["__complete", "4", "app", "deploy", "production", "--mode"]);
  //
  //   // Assert
  //   exitCode.ShouldBe(0);
  //   terminal.OutputContains("Fast").ShouldBeTrue();
  //   terminal.OutputContains("Standard").ShouldBeTrue();
  //   terminal.OutputContains("Slow").ShouldBeTrue();
  // }

  public static async Task Should_handle_explicit_app_name()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => { }).AsQuery().Done();

    // Act
    builder.EnableCompletion(appName: "my-custom-app");

    // Assert - The app name will be used when generating scripts
    // For now, just verify the routes are registered correctly
    bool hasCompleteRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    hasCompleteRoute.ShouldBeTrue();

    await Task.CompletedTask;
  }

}

// =============================================================================
// Test Helpers
// =============================================================================

sealed class TestCompletionSource(string[] values) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    foreach (string value in values)
    {
      yield return new CompletionCandidate(
        Value: value,
        Description: null,
        Type: CompletionType.Parameter
      );
    }
  }
}

enum DeploymentMode
{
  Fast,
  Standard,
  Slow
}

} // namespace TimeWarp.Nuru.Tests.Completion.Integration
