#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#:package Mediator.SourceGenerator

using Microsoft.Extensions.DependencyInjection;

return await RunTests<StaticFactoryMethodTests>(clearCache: true);

[TestTag("Factory")]
public class StaticFactoryMethodTests
{
  public static async Task CreateBuilder_should_return_builder_with_DI_enabled()
  {
    // Arrange & Act - NuruApp.CreateBuilder is available via meta-package (TimeWarp.Nuru)
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);

    // Assert - DI is enabled, so Services property should not throw
    IServiceCollection services = builder.Services; // Should not throw
    services.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task CreateBuilder_should_require_args_parameter()
  {
    // This test verifies the API signature - CreateBuilder requires args
    // The test passing means the signature is correct
    string[] args = ["test"];
    NuruAppBuilder builder = NuruApp.CreateBuilder(args);

    builder.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task CreateBuilder_should_accept_options()
  {
    // Arrange
    NuruCoreApplicationOptions options = new()
    {
      ApplicationName = "TestApp",
      EnvironmentName = "Testing"
    };

    // Act
    NuruAppBuilder builder = NuruApp.CreateBuilder([], options);

    // Assert
    builder.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task CreateSlimBuilder_should_return_builder_without_DI()
  {
    // Arrange & Act - CreateSlimBuilder returns NuruCoreAppBuilder (lightweight)
    NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Assert - DI is NOT enabled, so Services property should throw
    Should.Throw<InvalidOperationException>(() => _ = builder.Services);

    await Task.CompletedTask;
  }

  public static async Task CreateSlimBuilder_should_allow_optional_args()
  {
    // Arrange & Act - both should work (returns NuruCoreAppBuilder)
    NuruCoreAppBuilder builder1 = NuruApp.CreateSlimBuilder();
    NuruCoreAppBuilder builder2 = NuruApp.CreateSlimBuilder(["test"]);

    // Assert
    builder1.ShouldNotBeNull();
    builder2.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task CreateEmptyBuilder_should_return_minimal_builder()
  {
    // Arrange & Act - CreateEmptyBuilder returns NuruCoreAppBuilder (minimal)
    NuruCoreAppBuilder builder = NuruApp.CreateEmptyBuilder();

    // Assert - DI is NOT enabled
    Should.Throw<InvalidOperationException>(() => _ = builder.Services);

    await Task.CompletedTask;
  }

  public static async Task CreateEmptyBuilder_should_allow_optional_args()
  {
    // Arrange & Act - both should work (returns NuruCoreAppBuilder)
    NuruCoreAppBuilder builder1 = NuruApp.CreateEmptyBuilder();
    NuruCoreAppBuilder builder2 = NuruApp.CreateEmptyBuilder(["test"]);

    // Assert
    builder1.ShouldNotBeNull();
    builder2.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Map_should_be_alias_for_Map()
  {
    // Arrange
    bool matched = false;
    NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Act - use Map instead of Map
    builder.Map("test", () => { matched = true; return 0; });
    NuruCoreApp app = builder.Build();
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task MapDefault_should_be_alias_for_MapDefault()
  {
    // Arrange
    bool matched = false;
    NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Act - use MapDefault instead of MapDefault
    builder.MapDefault(() => { matched = true; return 0; });
    NuruCoreApp app = builder.Build();
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task CreateBuilder_full_integration_test()
  {
    // Arrange - Full featured builder with DI (via meta-package)
    bool matched = false;
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder.ConfigureServices(services =>
    {
      services.AddMediator();
      // Additional service registrations can go here if needed
    });
    // DI is available
    builder.Map("status", () => { matched = true; return 0; });

    // Act
    NuruCoreApp app = builder.Build();

    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task CreateSlimBuilder_full_integration_test()
  {
    // Arrange - Lightweight builder (returns NuruCoreAppBuilder)
    string result = "";
    NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder();
    builder.Map("greet {name}", (string name) => { result = $"Hello, {name}!"; return 0; });

    // Act
    NuruCoreApp app = builder.Build();
    int exitCode = await app.RunAsync(["greet", "World"]);

    // Assert
    exitCode.ShouldBe(0);
    result.ShouldBe("Hello, World!");
  }

  public static async Task CreateEmptyBuilder_full_integration_test()
  {
    // Arrange - Minimal builder (returns NuruCoreAppBuilder)
    bool matched = false;
    NuruCoreAppBuilder builder = NuruApp.CreateEmptyBuilder();
    builder.Map("cmd", () => { matched = true; return 0; });

    // Act
    NuruCoreApp app = builder.Build();
    int exitCode = await app.RunAsync(["cmd"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task Backward_compatibility_new_NuruAppBuilder_still_works()
  {
    // Arrange - Original API should still work
    bool matched = false;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("legacy", () => { matched = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["legacy"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task CreateBuilder_should_register_dynamic_completion_routes()
  {
    // Arrange & Act - NuruApp.CreateBuilder() calls UseAllExtensions() which enables dynamic completion
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);

    // Assert - Should have __complete route (dynamic completion callback)
    bool hasCompleteRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    hasCompleteRoute.ShouldBeTrue("__complete route should be registered by CreateBuilder()");

    // Assert - Should have --generate-completion route
    bool hasGenerateRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--generate-completion"));

    hasGenerateRoute.ShouldBeTrue("--generate-completion route should be registered by CreateBuilder()");

    // Assert - Should have --install-completion route
    bool hasInstallRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.OptionMatchers.Any(opt =>
        opt.MatchPattern == "--install-completion"));

    hasInstallRoute.ShouldBeTrue("--install-completion route should be registered by CreateBuilder()");

    await Task.CompletedTask;
  }

  public static async Task CreateBuilder_should_accept_completion_configuration()
  {
    // Arrange
    bool configureWasCalled = false;
    NuruAppOptions options = new()
    {
      ConfigureCompletion = _ =>
      {
        configureWasCalled = true;
        // Could register custom completion sources here
      }
    };

    // Act
    NuruAppBuilder builder = NuruApp.CreateBuilder([], options);

    // Assert
    configureWasCalled.ShouldBeTrue("ConfigureCompletion callback should be invoked");

    await Task.CompletedTask;
  }

  public static async Task CreateSlimBuilder_should_not_register_completion_routes()
  {
    // Arrange & Act - CreateSlimBuilder doesn't call UseAllExtensions
    NuruCoreAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Assert - Should NOT have completion routes (manual opt-in required)
    bool hasCompleteRoute = builder.EndpointCollection.Any(e =>
      e.CompiledRoute.PositionalMatchers.Count > 0 &&
      e.CompiledRoute.PositionalMatchers[0] is LiteralMatcher literal &&
      literal.Value == "__complete");

    hasCompleteRoute.ShouldBeFalse("CreateSlimBuilder should not auto-register completion routes");

    await Task.CompletedTask;
  }
}
