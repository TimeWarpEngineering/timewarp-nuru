#!/usr/bin/dotnet --

return await RunTests<OptionMatchingTests>(clearCache: true);

[TestTag("Routing")]
public class OptionMatchingTests
{
  public static async Task Should_match_required_option_build_config_debug()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundMode = null;
    builder.AddRoute("build --config {mode}", (string mode) => { boundMode = mode; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("debug");

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_missing_required_option_build()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("build --config {mode}", (string _) => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required option

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_option_build_config_release()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundMode = null;
    builder.AddRoute("build --config {mode?}", (string? mode) => { boundMode = mode; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBe("release");

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_option_build_null()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundMode = "unexpected";
    builder.AddRoute("build --config {mode?}", (string? mode) => { boundMode = mode; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundMode.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_verbose_true()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool boundVerbose = false;
    builder.AddRoute("build --verbose", (bool verbose) => { boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_false()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool boundVerbose = true;
    builder.AddRoute("build --verbose", (bool verbose) => { boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_tag_v1_0_verbose()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundE = null;
    string? boundT = null;
    bool boundVerbose = false;
    builder.AddRoute("deploy --env {e} --tag {t?} --verbose", (string e, string? t, bool verbose) => { boundE = e; boundT = t; boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1.0", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldBe("prod");
    boundT.ShouldBe("v1.0");
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_defaults()
  {
    // Arrange
    NuruAppBuilder builder = new();
    string? boundE = null;
    string? boundT = "unexpected";
    bool boundVerbose = true;
    builder.AddRoute("deploy --env {e} --tag {t?} --verbose", (string e, string? t, bool verbose) => { boundE = e; boundT = t; boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    boundE.ShouldBe("prod");
    boundT.ShouldBeNull();
    boundVerbose.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_mixed_missing_required_deploy_tag_v1_0()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("deploy --env {e} --tag {t?} --verbose", (string _, string? _, bool _) => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required --env

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_option_server_port_8080()
  {
    // Arrange
    NuruAppBuilder builder = new();
    int boundNum = 0;
    builder.AddRoute("server --port {num:int}", (int num) => { boundNum = num; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    boundNum.ShouldBe(8080);

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_typed_option_server_port_abc()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("server --port {num:int}", (int _) => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_verbose()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool boundVerbose = false;
    builder.AddRoute("build --verbose,-v", (bool verbose) => { boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_v()
  {
    // Arrange
    NuruAppBuilder builder = new();
    bool boundVerbose = false;
    builder.AddRoute("build --verbose,-v", (bool verbose) => { boundVerbose = verbose; return 0; });

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-v"]);

    // Assert
    exitCode.ShouldBe(0);
    boundVerbose.ShouldBeTrue();

    await Task.CompletedTask;
  }
}