#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using Shouldly;

#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class CursorContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CursorContextTests>();

  // Note: These tests validate that completion scripts contain the necessary
  // components for context-aware completion. The actual cursor position
  // handling is done by the shell's completion engine.

  public static async Task Should_include_commands_for_first_word_completion()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy", () => 0);
    builder.Map("build", () => 0);
    builder.Map("test", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All commands should be available for first word completion
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("build");
    bashScript.ShouldContain("test");

    await Task.CompletedTask;
  }

  public static async Task Should_include_options_for_option_completion()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy --force --verbose --dry-run", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Options should be available for completion
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--dry-run");

    await Task.CompletedTask;
  }

  public static async Task Should_support_partial_command_matching()
  {
    // Arrange - Multiple commands with common prefix
    NuruAppBuilder builder = new();
    builder.Map("create", () => 0);
    builder.Map("createorder", () => 0);
    builder.Map("createuser", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All variants should be present for shell to filter
    bashScript.ShouldContain("create");
    bashScript.ShouldContain("createorder");
    bashScript.ShouldContain("createuser");

    await Task.CompletedTask;
  }

  public static async Task Should_support_partial_option_matching()
  {
    // Arrange - Options with common prefix
    NuruAppBuilder builder = new();
    builder.Map("test --verbose --version --validate", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All options starting with --v should be present
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--version");
    bashScript.ShouldContain("--validate");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_subcommands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git add", () => 0);
    builder.Map("git commit", () => 0);
    builder.Map("git push", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - First level command should be present
    bashScript.ShouldContain("git");

    await Task.CompletedTask;
  }

  public static async Task Should_provide_context_for_zsh()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("build --config {mode}", (string mode) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert - Zsh script should use _arguments for context-aware completion
    zshScript.ShouldContain("_arguments");
    zshScript.ShouldContain("deploy");
    zshScript.ShouldContain("build");

    await Task.CompletedTask;
  }

  public static async Task Should_provide_context_for_powershell()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test {file}", (string file) => 0);
    builder.Map("run --verbose", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - PowerShell should use Register-ArgumentCompleter for context
    pwshScript.ShouldContain("Register-ArgumentCompleter");
    pwshScript.ShouldContain("test");
    pwshScript.ShouldContain("run");

    await Task.CompletedTask;
  }

  public static async Task Should_provide_context_for_fish()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("config --get {key}", (string key) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - Fish uses declarative complete commands
    fishScript.ShouldContain("complete -c");
    fishScript.ShouldContain("status");
    fishScript.ShouldContain("config");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_first_literal_from_complex_routes()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} to {region}", (string env, string region) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - First literal should be extracted for command completion
    bashScript.ShouldContain("deploy");
    // Note: Current implementation extracts first literal only

    await Task.CompletedTask;
  }

  public static async Task Should_support_option_value_completion()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --config {mode}", (string mode) => 0);
    builder.Map("test --output {file}", (string file) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Options that take values should be present
    bashScript.ShouldContain("--config");
    bashScript.ShouldContain("--output");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_boolean_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy --force --dry-run --verbose", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Boolean options (no values) should be present
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--dry-run");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_work_across_all_shells()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("command {param}", (string param) => 0);
    builder.Map("option --flag {value}", (string value) => 0);

    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All shells should have context-aware completion support
    bashScript.ShouldContain("command");
    zshScript.ShouldContain("command");
    pwshScript.ShouldContain("command");
    fishScript.ShouldContain("command");

    await Task.CompletedTask;
  }
}
