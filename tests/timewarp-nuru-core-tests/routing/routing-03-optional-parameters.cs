#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionalParametersTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionalParametersTests>();

  public static async Task Should_match_required_string_deploy_prod()
  {
    // Arrange
    string? boundEnv = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env}").WithHandler((string env) => { boundEnv = env; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBe("prod");

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_missing_required_string_deploy()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env}").WithHandler((string env) => 0).AsCommand().Done()
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required parameter

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_string_deploy_prod()
  {
    // Arrange
    string? boundEnv = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env?}").WithHandler((string? env) => { boundEnv = env; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBe("prod");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_string_deploy_null()
  {
    // Arrange
    string? boundEnv = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env?}").WithHandler((string? env) => { boundEnv = env; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_integer_list_10()
  {
    // Arrange
    int? boundCount = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("list {count:int?}").WithHandler((int? count) => { boundCount = count; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["list", "10"]);

    // Assert
    exitCode.ShouldBe(0);
    boundCount.ShouldBe(10);

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_integer_list_null()
  {
    // Arrange
    int? boundCount = 5;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("list {count:int?}").WithHandler((int? count) => { boundCount = count; }).AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["list"]);

    // Assert
    exitCode.ShouldBe(0);
    boundCount.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_prod_v1_0()
  {
    // Arrange
    string? boundEnv = null;
    string? boundTag = null;
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => { boundEnv = env; boundTag = tag; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "v1.0"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBe("prod");
    boundTag.ShouldBe("v1.0");

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_prod_null()
  {
    // Arrange
    string? boundEnv = null;
    string? boundTag = "unexpected";
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => { boundEnv = env; boundTag = tag; }).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    boundEnv.ShouldBe("prod");
    boundTag.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_mixed_missing_required_deploy()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy {env} {tag?}").WithHandler((string env, string? tag) => 0).AsCommand().Done()
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required env

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
