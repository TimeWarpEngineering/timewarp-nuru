#!/usr/bin/dotnet --

return await RunTests<OptionalParametersTests>(clearCache: true);

[TestTag("Routing")]
public class OptionalParametersTests
{
  public static async Task Should_match_required_string_deploy_prod()
  {
    // Arrange
    string? boundEnv = null;
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env}", (string env) => { boundEnv = env; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env}", (string env) => 0)
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env?}", (string? env) => { boundEnv = env; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env?}", (string? env) => { boundEnv = env; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("list {count:int?}", (int? count) => { boundCount = count; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("list {count:int?}", (int? count) => { boundCount = count; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env} {tag?}", (string env, string? tag) => { boundEnv = env; boundTag = tag; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env} {tag?}", (string env, string? tag) => { boundEnv = env; boundTag = tag; return 0; })
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
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env} {tag?}", (string env, string? tag) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["deploy"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required env

    await Task.CompletedTask;
  }
}