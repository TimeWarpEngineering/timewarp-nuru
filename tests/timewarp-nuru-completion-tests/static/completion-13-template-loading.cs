#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using Shouldly;

return await RunTests<TemplateLoadingTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class TemplateLoadingTests
{
  public static async Task Should_load_bash_template()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Template should be loaded and contain expected structure
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");
    bashScript.ShouldContain("testapp"); // APP_NAME placeholder replaced
    bashScript.ShouldContain("_init_completion");

    await Task.CompletedTask;
  }

  public static async Task Should_load_zsh_template()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldNotBeEmpty();
    zshScript.ShouldContain("# Zsh completion for");
    zshScript.ShouldContain("testapp");
    zshScript.ShouldContain("#compdef");

    await Task.CompletedTask;
  }

  public static async Task Should_load_powershell_template()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldNotBeEmpty();
    pwshScript.ShouldContain("# PowerShell completion for");
    pwshScript.ShouldContain("testapp");
    pwshScript.ShouldContain("Register-ArgumentCompleter");

    await Task.CompletedTask;
  }

  public static async Task Should_load_fish_template()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript.ShouldNotBeEmpty();
    fishScript.ShouldContain("# Fish completion for");
    fishScript.ShouldContain("testapp");
    fishScript.ShouldContain("complete -c");

    await Task.CompletedTask;
  }

  public static async Task Should_replace_app_name_placeholder()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "myapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "yourapp");

    // Assert - {{APP_NAME}} should be replaced
    bashScript.ShouldContain("myapp");
    bashScript.ShouldNotContain("{{APP_NAME}}");

    zshScript.ShouldContain("yourapp");
    zshScript.ShouldNotContain("{{APP_NAME}}");

    await Task.CompletedTask;
  }

  public static async Task Should_replace_commands_placeholder()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("create", () => 0);
    builder.Map("delete", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - {{COMMANDS}} should be replaced with actual commands
    bashScript.ShouldContain("create");
    bashScript.ShouldContain("delete");
    bashScript.ShouldNotContain("{{COMMANDS}}");

    await Task.CompletedTask;
  }

  public static async Task Should_replace_options_placeholder()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose --force", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - {{OPTIONS}} should be replaced with actual options
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--force");
    bashScript.ShouldNotContain("{{OPTIONS}}");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_commands_list()
  {
    // Arrange - No routes
    NuruAppBuilder builder = new();
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Template should still be valid even with empty commands
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldNotContain("{{COMMANDS}}");
    bashScript.ShouldNotContain("{{OPTIONS}}");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_valid_scripts_for_all_shells()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("version --format {fmt}", (string fmt) => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All should be non-empty and contain app name
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("testapp");

    zshScript.ShouldNotBeEmpty();
    zshScript.ShouldContain("testapp");

    pwshScript.ShouldNotBeEmpty();
    pwshScript.ShouldContain("testapp");

    fishScript.ShouldNotBeEmpty();
    fishScript.ShouldContain("testapp");

    await Task.CompletedTask;
  }

  public static async Task Should_use_consistent_template_structure()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test", () => 0);
    CompletionScriptGenerator generator = new();

    // Act
    string bashScript1 = generator.GenerateBash(builder.EndpointCollection, "app1");
    string bashScript2 = generator.GenerateBash(builder.EndpointCollection, "app2");

    // Assert - Same template structure, different app names
    bashScript1.ShouldContain("app1");
    bashScript2.ShouldContain("app2");

    // Both should have same core structure
    bashScript1.ShouldContain("_init_completion");
    bashScript2.ShouldContain("_init_completion");

    await Task.CompletedTask;
  }
}
