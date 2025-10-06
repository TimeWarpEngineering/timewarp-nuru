#!/usr/bin/dotnet --

return await RunTests<ErrorCasesTests>(clearCache: true);

[TestTag("Routing")]
public class ErrorCasesTests
{
  public static async Task Should_no_matching_route_unknown()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("version", () => 0);

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["unknown"]);

    // Assert
    exitCode.ShouldBe(1); // No route matches

    await Task.CompletedTask;
  }

  public static async Task Should_type_conversion_failure_delay_abc()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("delay {ms:int}", (int ms) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["delay", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type constraint fails

    await Task.CompletedTask;
  }

  public static async Task Should_missing_required_option_value_build_config()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("build --config {mode}", (string mode) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(1); // Missing option value

    await Task.CompletedTask;
  }

  public static async Task Should_duplicate_non_repeated_option_build_verbose_verbose()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("build --verbose", (bool verbose) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose", "--verbose"]);

    // Assert
    exitCode.ShouldBe(1); // Duplicate option

    await Task.CompletedTask;
  }

  public static async Task Should_unknown_option_build_verbose_unknown()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("build --verbose", (bool verbose) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose", "--unknown"]);

    // Assert
    exitCode.ShouldBe(1); // Unknown option

    await Task.CompletedTask;
  }

  public static async Task Should_mixed_positionals_with_options_deploy_prod_tag_v1_0()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("deploy {env} --tag {t}", (string env, string t) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "prod", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(0); // Should match (positionals before options)

    await Task.CompletedTask;
  }

  public static async Task Should_mixed_positionals_options_reversed_implementation_defined()
  {
    // Arrange
    NuruAppBuilder builder = new();
#pragma warning disable RCS1163 // Unused parameter
    builder.AddRoute("deploy {env} --tag {t}", (string env, string t) => 0);
#pragma warning restore RCS1163 // Unused parameter

    NuruApp app = builder.Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0", "prod"]);

    // Assert
    exitCode.ShouldBe(1); // Implementation-defined, but expect failure for strict ordering

    await Task.CompletedTask;
  }
}