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

  public static async Task Should_match_when_all_options_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --version? {ver?} --dry-run")
        .WithHandler((string env, string? ver, bool dryRun) => $"env:{env}|ver:{ver ?? "null"}|dryRun:{dryRun}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--version", "v1.0", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:prod|ver:v1.0|dryRun:True").ShouldBeTrue();
  }

  public static async Task Should_match_with_only_required_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --version? {ver?} --dry-run")
        .WithHandler((string env, string? ver, bool dryRun) => $"env:{env}|ver:{ver ?? "null"}|dryRun:{dryRun}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:prod|ver:null|dryRun:False").ShouldBeTrue();
  }

  public static async Task Should_not_match_when_missing_required_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --version? {ver?} --dry-run")
        .WithHandler((string env, string? ver, bool dryRun) => $"env:{env}|ver:{ver ?? "null"}|dryRun:{dryRun}")
        .AsCommand()
        .Done()
      .Build();

    // Act - missing required --env
    int exitCode = await app.RunAsync(["deploy", "--version", "v1.0"]);

    // Assert - should not match (exit code 1)
    exitCode.ShouldBe(1);
  }

  public static async Task Should_match_with_required_and_boolean_only()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("deploy --env {env} --version? {ver?} --dry-run")
        .WithHandler((string env, string? ver, bool dryRun) => $"env:{env}|ver:{ver ?? "null"}|dryRun:{dryRun}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "staging", "--dry-run"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("env:staging|ver:null|dryRun:True").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Options
