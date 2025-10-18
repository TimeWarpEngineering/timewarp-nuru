#!/usr/bin/dotnet --

return await RunTests<ErrorCasesTests>(clearCache: true);

[TestTag("Routing")]
public class ErrorCasesTests
{
  public static async Task Should_no_matching_route_unknown()
  {
    // Arrange
    NuruApp app = new NuruAppBuilder()
      .AddRoute("status", () => 0)
      .AddRoute("version", () => 0)
      .Build();

    // Act
    int exitCode = await app.RunAsync(["unknown"]);

    // Assert
    exitCode.ShouldBe(1); // No route matches

    await Task.CompletedTask;
  }

  public static async Task Should_type_conversion_failure_delay_abc()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("delay {ms:int}", (int ms) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type constraint fails

    await Task.CompletedTask;
  }

  public static async Task Should_missing_required_option_value_build_config()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("build --config {mode}", (string mode) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(1); // Missing option value

    await Task.CompletedTask;
  }

  public static async Task Should_duplicate_non_repeated_option_build_verbose_verbose()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("build --verbose", (bool verbose) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose", "--verbose"]);

    // Assert
    exitCode.ShouldBe(1); // Duplicate option

    await Task.CompletedTask;
  }

  public static async Task Should_unknown_option_build_verbose_unknown()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("build --verbose", (bool verbose) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose", "--unknown"]);

    // Assert
    exitCode.ShouldBe(1); // Unknown option

    await Task.CompletedTask;
  }

  public static async Task Should_mixed_positionals_with_options_deploy_prod_tag_v1_0()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env} --tag {t}", (string env, string t) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(0); // Should match (positionals before options)

    await Task.CompletedTask;
  }

  public static async Task Should_mixed_positionals_options_reversed_implementation_defined()
  {
    // Arrange
#pragma warning disable RCS1163 // Unused parameter
    NuruApp app = new NuruAppBuilder()
      .AddRoute("deploy {env} --tag {t}", (string env, string t) => 0)
      .Build();
#pragma warning restore RCS1163 // Unused parameter

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0", "prod"]);

    // Assert
    exitCode.ShouldBe(1); // Implementation-defined, but expect failure for strict ordering

    await Task.CompletedTask;
  }
}