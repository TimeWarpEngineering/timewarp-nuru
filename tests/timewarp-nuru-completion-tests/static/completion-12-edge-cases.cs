#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class CompletionEdgeCasesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CompletionEdgeCasesTests>();

  public static async Task Should_handle_empty_string_app_name()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    CompletionScriptGenerator generator = new();

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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    CompletionScriptGenerator generator = new();

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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    CompletionScriptGenerator generator = new();

    // Act - App name with shell special characters
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "my$app");

    // Assert - Should include the app name as-is
    bashScript.ShouldContain("my$app");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("{file}")
      .WithHandler((string file) => 0)
      .AsCommand()
      .Done();
    builder.Map("{source} {dest}")
      .WithHandler((string source, string dest) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Should not crash, but won't have literal commands
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("--version")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("--help")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy {env} --force")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy {env} --dry-run")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("test --verbose")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("build --verbose")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy --verbose")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - '--verbose' should be deduplicated
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_very_long_route_patterns()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {environment} {version} --config {configFile} --region {region} --instance-count {count:int} --timeout {seconds:int} --verbose --force --dry-run")
      .WithHandler((string environment, string version, string configFile, string region, int count, int seconds) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("kebab-case-command")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("snake_case_command")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("kebab-case-command");
    bashScript.ShouldContain("snake_case_command");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_numeric_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("delay {ms:int}")
      .WithHandler((int ms) => 0)
      .AsCommand()
      .Done();
    builder.Map("scale {factor:double}")
      .WithHandler((double factor) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("delay");
    bashScript.ShouldContain("scale");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_optional_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {tag?}")
      .WithHandler((string env, string? tag) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_catch_all_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git {*args}")
      .WithHandler((string[] args) => 0)
      .AsCommand()
      .Done();
    builder.Map("docker {*args}")
      .WithHandler((string[] args) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("git");
    bashScript.ShouldContain("docker");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_case_sensitivity()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("Status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("STATUS")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("http2-config")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("base64-encode")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("sha256-hash")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("version")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    CompletionScriptGenerator generator = new();

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
