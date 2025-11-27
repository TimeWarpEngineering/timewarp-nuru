#!/usr/bin/dotnet --

using Microsoft.Extensions.DependencyInjection;

return await RunTests<StaticFactoryMethodTests>(clearCache: true);

[TestTag("Factory")]
public class StaticFactoryMethodTests
{
  public static async Task CreateBuilder_should_return_builder_with_DI_enabled()
  {
    // Arrange & Act
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
    var options = new NuruApplicationOptions
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
    // Arrange & Act
    NuruAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Assert - DI is NOT enabled, so Services property should throw
    Should.Throw<InvalidOperationException>(() => _ = builder.Services);

    await Task.CompletedTask;
  }

  public static async Task CreateSlimBuilder_should_allow_optional_args()
  {
    // Arrange & Act - both should work
    NuruAppBuilder builder1 = NuruApp.CreateSlimBuilder();
    NuruAppBuilder builder2 = NuruApp.CreateSlimBuilder(["test"]);

    // Assert
    builder1.ShouldNotBeNull();
    builder2.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task CreateEmptyBuilder_should_return_minimal_builder()
  {
    // Arrange & Act
    NuruAppBuilder builder = NuruApp.CreateEmptyBuilder();

    // Assert - DI is NOT enabled
    Should.Throw<InvalidOperationException>(() => _ = builder.Services);

    await Task.CompletedTask;
  }

  public static async Task CreateEmptyBuilder_should_allow_optional_args()
  {
    // Arrange & Act - both should work
    NuruAppBuilder builder1 = NuruApp.CreateEmptyBuilder();
    NuruAppBuilder builder2 = NuruApp.CreateEmptyBuilder(["test"]);

    // Assert
    builder1.ShouldNotBeNull();
    builder2.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Map_should_be_alias_for_Map()
  {
    // Arrange
    bool matched = false;
    NuruAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Act - use Map instead of Map
    builder.Map("test", () => { matched = true; return 0; });
    NuruApp app = builder.Build();
    int exitCode = await app.RunAsync(["test"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task MapDefault_should_be_alias_for_MapDefault()
  {
    // Arrange
    bool matched = false;
    NuruAppBuilder builder = NuruApp.CreateSlimBuilder();

    // Act - use MapDefault instead of MapDefault
    builder.MapDefault(() => { matched = true; return 0; });
    NuruApp app = builder.Build();
    int exitCode = await app.RunAsync([]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task CreateBuilder_full_integration_test()
  {
    // Arrange - Full featured builder with DI
    bool matched = false;
    NuruAppBuilder builder = NuruApp.CreateBuilder([]);
    builder.Map("status", () => { matched = true; return 0; });

    // Act
    NuruApp app = builder.Build();
    int exitCode = await app.RunAsync(["status"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task CreateSlimBuilder_full_integration_test()
  {
    // Arrange - Lightweight builder
    string result = "";
    NuruAppBuilder builder = NuruApp.CreateSlimBuilder();
    builder.Map("greet {name}", (string name) => { result = $"Hello, {name}!"; return 0; });

    // Act
    NuruApp app = builder.Build();
    int exitCode = await app.RunAsync(["greet", "World"]);

    // Assert
    exitCode.ShouldBe(0);
    result.ShouldBe("Hello, World!");
  }

  public static async Task CreateEmptyBuilder_full_integration_test()
  {
    // Arrange - Minimal builder
    bool matched = false;
    NuruAppBuilder builder = NuruApp.CreateEmptyBuilder();
    builder.Map("cmd", () => { matched = true; return 0; });

    // Act
    NuruApp app = builder.Build();
    int exitCode = await app.RunAsync(["cmd"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }

  public static async Task Backward_compatibility_new_NuruAppBuilder_still_works()
  {
    // Arrange - Original API should still work
    bool matched = false;
    NuruApp app = new NuruAppBuilder()
      .Map("legacy", () => { matched = true; return 0; })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["legacy"]);

    // Assert
    exitCode.ShouldBe(0);
    matched.ShouldBeTrue();
  }
}
