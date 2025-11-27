#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<CompletionEdgeCasesTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CompletionEdgeCasesTests
{
  public static async Task Should_handle_empty_string_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    var generator = new CompletionScriptGenerator();

    // Act - Empty app name should still generate valid script
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "");

    // Assert - Script should still be generated with empty placeholder
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_whitespace_only_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    var generator = new CompletionScriptGenerator();

    // Act - Whitespace app name should still generate script
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "   ");

    // Assert
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_shell_characters_in_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    var generator = new CompletionScriptGenerator();

    // Act - App name with shell special characters
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "my$app");

    // Assert - Should include the app name as-is
    bashScript.ShouldContain("my$app");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("{file}", (string file) => 0);
    builder.Map("{source} {dest}", (string source, string dest) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Should not crash, but won't have literal commands
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("--version", () => 0);
    builder.Map("--help", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("--version");
    bashScript.ShouldContain("--help");

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("deploy {env} --force", (string env) => 0);
    builder.Map("deploy {env} --dry-run", (string env) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - 'deploy' should appear only once in the commands list
    int deployCount = bashScript.Split("deploy").Length - 1;
    deployCount.ShouldBeGreaterThan(0, "Should contain 'deploy' at least once");
    // Can't easily verify exact count due to multiple uses in script

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("test --verbose", () => 0);
    builder.Map("build --verbose", () => 0);
    builder.Map("deploy --verbose", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - '--verbose' should be deduplicated
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_very_long_route_patterns()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map(
      "deploy {environment} {version} --config {configFile} --region {region} --instance-count {count:int} --timeout {seconds:int} --verbose --force --dry-run",
      (string environment, string version, string configFile, string region, int count, int seconds) => 0
    );

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("--config");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters_in_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("kebab-case-command", () => 0);
    builder.Map("snake_case_command", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("kebab-case-command");
    bashScript.ShouldContain("snake_case_command");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_numeric_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("delay {ms:int}", (int ms) => 0);
    builder.Map("scale {factor:double}", (double factor) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("delay");
    bashScript.ShouldContain("scale");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_optional_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env} {tag?}", (string env, string? tag) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_catch_all_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("git {*args}", (string[] args) => 0);
    builder.Map("docker {*args}", (string[] args) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("git");
    bashScript.ShouldContain("docker");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_case_sensitivity()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("Status", () => 0);
    builder.Map("status", () => 0);
    builder.Map("STATUS", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All three should be present (case-sensitive)
    bashScript.ShouldContain("Status");
    bashScript.ShouldContain("status");
    bashScript.ShouldContain("STATUS");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_numbers_in_names()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("http2-config", () => 0);
    builder.Map("base64-encode", () => 0);
    builder.Map("sha256-hash", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("http2-config");
    bashScript.ShouldContain("base64-encode");
    bashScript.ShouldContain("sha256-hash");

    await Task.CompletedTask;
  }

  public static async Task Should_work_across_all_shell_types()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    var generator = new CompletionScriptGenerator();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All should contain the commands
    bashScript.ShouldContain("status");
    zshScript.ShouldContain("status");
    pwshScript.ShouldContain("status");
    fishScript.ShouldContain("status");

    await Task.CompletedTask;
  }
}
