#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Options
{

/// <summary>
/// Tests for mixed required and optional options.
/// Pattern: deploy --env {env} --version? {ver?} --dry-run
/// - --env is REQUIRED (no ? on flag)
/// - --version is OPTIONAL (? on flag)
/// - --dry-run is OPTIONAL (boolean flags always optional)
/// </summary>
[TestTag("Options")]
public class MixedRequiredOptionalTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MixedRequiredOptionalTests>();

  [Skip("Awaiting Task 200: Update to new fluent API")]
  public static async Task Should_match_when_all_options_provided()
  {
    // Arrange
    string? capturedEnv = null;
    string? capturedVer = null;
    bool capturedDryRun = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {env} --version? {ver?} --dry-run",
        (string env, string? ver, bool dryRun) =>
        {
          capturedEnv = env;
          capturedVer = ver;
          capturedDryRun = dryRun;
          return 0;
        })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--version", "v1.0", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("prod");
    capturedVer.ShouldBe("v1.0");
    capturedDryRun.ShouldBeTrue();
  }

  [Skip("Awaiting Task 200: Update to new fluent API")]
  public static async Task Should_match_with_only_required_option()
  {
    // Arrange
    string? capturedEnv = null;
    string? capturedVer = null;
    bool capturedDryRun = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {env} --version? {ver?} --dry-run",
        (string env, string? ver, bool dryRun) =>
        {
          capturedEnv = env;
          capturedVer = ver;
          capturedDryRun = dryRun;
          return 0;
        })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("prod");
    capturedVer.ShouldBeNull();
    capturedDryRun.ShouldBeFalse();
  }

  [Skip("Awaiting Task 200: Update to new fluent API")]
  public static async Task Should_not_match_when_missing_required_option()
  {
    // Arrange
    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {env} --version? {ver?} --dry-run",
        (string _, string? _, bool _) => 0)
      .Build();

    // Act - missing required --env
    int exitCode = await app.RunAsync(["deploy", "--version", "v1.0"]);

    // Assert - should not match (exit code 1)
    exitCode.ShouldBe(1);
  }

  [Skip("Awaiting Task 200: Update to new fluent API")]
  public static async Task Should_match_with_required_and_boolean_only()
  {
    // Arrange
    string? capturedEnv = null;
    string? capturedVer = null;
    bool capturedDryRun = false;

    NuruCoreApp app = new NuruAppBuilder()
      .Map("deploy --env {env} --version? {ver?} --dry-run",
        (string env, string? ver, bool dryRun) =>
        {
          capturedEnv = env;
          capturedVer = ver;
          capturedDryRun = dryRun;
          return 0;
        })
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "staging", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    capturedEnv.ShouldBe("staging");
    capturedVer.ShouldBeNull();
    capturedDryRun.ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Options
