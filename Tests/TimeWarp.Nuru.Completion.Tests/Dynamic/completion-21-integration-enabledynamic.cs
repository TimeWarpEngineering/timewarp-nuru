#!/usr/bin/dotnet --

return await RunTests<IntegrationEnableDynamicTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class IntegrationEnableDynamicTests
{
  public static async Task Should_register_complete_route()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    builder.EnableDynamicCompletion();

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
    var builder = new NuruAppBuilder();

    // Act
    builder.EnableDynamicCompletion();

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
    var builder = new NuruAppBuilder();

    // Act
    NuruAppBuilder result = builder.EnableDynamicCompletion();

    // Assert
    result.ShouldBeSameAs(builder);

    await Task.CompletedTask;
  }

  public static async Task Should_configure_registry_via_callback()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env}", (string env) => 0);

    bool registryConfigured = false;

    // Act
    builder.EnableDynamicCompletion(configure: registry =>
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.EnableDynamicCompletion();

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
    var builder = new NuruAppBuilder();
    builder.EnableDynamicCompletion();

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
    Should.Throw<ArgumentNullException>(() => nullBuilder!.EnableDynamicCompletion());

    await Task.CompletedTask;
  }

  public static async Task Should_execute_complete_route_and_return_candidates()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("version", () => 0);
    builder.EnableDynamicCompletion();

    NuruApp app = builder.Build();

    // Act - Execute the __complete route
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain("status");
    output.ShouldContain("version");
    output.ShouldContain(":4"); // Directive
  }

  public static async Task Should_use_configured_sources_in_complete_route()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env}", (string env) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSource source = new(["production", "staging", "development"]);
      registry.RegisterForParameter("env", source);
    });

    NuruApp app = builder.Build();

    // Act - Complete the env parameter
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain("production");
    output.ShouldContain("staging");
    output.ShouldContain("development");
  }

  public static async Task Should_auto_register_enum_sources()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env} --mode {mode}", (string env, DeploymentMode mode) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      // Register enum source for the type
      EnumCompletionSource<DeploymentMode> enumSource = new();
      registry.RegisterForType(typeof(DeploymentMode), enumSource);
    });

    NuruApp app = builder.Build();

    // Act - Complete the mode parameter (after --mode option)
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "4", "app", "deploy", "production", "--mode"]));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain("Fast");
    output.ShouldContain("Standard");
    output.ShouldContain("Slow");
  }

  public static async Task Should_handle_explicit_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    builder.EnableDynamicCompletion(appName: "my-custom-app");

    // Assert - The app name will be used when generating scripts
    // For now, just verify the routes are registered correctly
    bool hasCompleteRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    hasCompleteRoute.ShouldBeTrue();

    await Task.CompletedTask;
  }

  private static async Task<(int exitCode, string stdout, string stderr)> CaptureAppOutputAsync(Func<Task<int>> action)
  {
    TextWriter originalOut = Console.Out;
    TextWriter originalError = Console.Error;

    using StringWriter stdoutWriter = new();
    using StringWriter stderrWriter = new();

    try
    {
      Console.SetOut(stdoutWriter);
      Console.SetError(stderrWriter);

      int exitCode = await action();

      return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
    }
    finally
    {
      Console.SetOut(originalOut);
      Console.SetError(originalError);
    }
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
