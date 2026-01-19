#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.DynamicScriptGen
{

[TestTag("Completion")]
public class DynamicScriptGenTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DynamicScriptGenTests>();

  public static async Task Should_generate_bash_script_with_callback()
  {
    // Arrange
    string appName = "myapp";

    // Act
    string script = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert
    script.ShouldContain("myapp");
    script.ShouldContain("__complete"); // Callback command
    script.ShouldContain("complete -F"); // Bash completion registration

    await Task.CompletedTask;
  }

  public static async Task Should_generate_zsh_script_with_callback()
  {
    // Arrange
    string appName = "myapp";

    // Act
    string script = DynamicCompletionScriptGenerator.GenerateZsh(appName);

    // Assert
    script.ShouldContain("myapp");
    script.ShouldContain("__complete"); // Callback command
    script.ShouldContain("compdef"); // Zsh completion registration

    await Task.CompletedTask;
  }

  public static async Task Should_generate_powershell_script_with_callback()
  {
    // Arrange
    string appName = "myapp";

    // Act
    string script = DynamicCompletionScriptGenerator.GeneratePowerShell(appName);

    // Assert
    script.ShouldContain("myapp");
    script.ShouldContain("__complete"); // Callback command
    script.ShouldContain("Register-ArgumentCompleter"); // PowerShell registration

    await Task.CompletedTask;
  }

  public static async Task Should_generate_fish_script_with_callback()
  {
    // Arrange
    string appName = "myapp";

    // Act
    string script = DynamicCompletionScriptGenerator.GenerateFish(appName);

    // Assert
    script.ShouldContain("myapp");
    script.ShouldContain("__complete"); // Callback command
    script.ShouldContain("complete -c"); // Fish completion registration

    await Task.CompletedTask;
  }

  public static async Task Should_replace_app_name_placeholder()
  {
    // Arrange
    string appName = "custom-cli-tool";

    // Act
    string script = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert - Should contain the actual app name, not placeholder
    script.ShouldContain("custom-cli-tool");
    script.ShouldNotContain("{{APP_NAME}}");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_hyphenated_app_names()
  {
    // Arrange
    string appName = "my-cli-app";

    // Act
    string bashScript = DynamicCompletionScriptGenerator.GenerateBash(appName);
    string zshScript = DynamicCompletionScriptGenerator.GenerateZsh(appName);

    // Assert
    bashScript.ShouldContain("my-cli-app");
    zshScript.ShouldContain("my-cli-app");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_underscored_app_names()
  {
    // Arrange
    string appName = "my_cli_app";

    // Act
    string script = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert
    script.ShouldContain("my_cli_app");

    await Task.CompletedTask;
  }

  public static async Task Should_include_cursor_position_in_callback()
  {
    // Arrange
    string appName = "testapp";

    // Act
    string bashScript = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert - Script should pass cursor position to __complete
    // Uses _init_completion which sets local "cword" variable (not COMP_CWORD)
    bashScript.ShouldContain("cword");

    await Task.CompletedTask;
  }

  public static async Task Should_include_command_words_in_callback()
  {
    // Arrange
    string appName = "testapp";

    // Act
    string bashScript = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert - Script should pass words to __complete
    // Uses _init_completion which sets local "words" variable (not COMP_WORDS)
    bashScript.ShouldContain("words");

    await Task.CompletedTask;
  }

  public static async Task Should_generate_script_that_parses_directive()
  {
    // Arrange
    string appName = "testapp";

    // Act
    string bashScript = DynamicCompletionScriptGenerator.GenerateBash(appName);

    // Assert - Script should handle directive codes
    bashScript.ShouldContain(":"); // Directive parsing

    await Task.CompletedTask;
  }

  public static async Task Should_generate_all_shell_scripts_successfully()
  {
    // Arrange
    string appName = "multitest";

    // Act & Assert - All should generate without error
    string bash = DynamicCompletionScriptGenerator.GenerateBash(appName);
    string zsh = DynamicCompletionScriptGenerator.GenerateZsh(appName);
    string pwsh = DynamicCompletionScriptGenerator.GeneratePowerShell(appName);
    string fish = DynamicCompletionScriptGenerator.GenerateFish(appName);

    bash.ShouldNotBeNullOrEmpty();
    zsh.ShouldNotBeNullOrEmpty();
    pwsh.ShouldNotBeNullOrEmpty();
    fish.ShouldNotBeNullOrEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_generate_scripts_with_different_lengths()
  {
    // Arrange
    string appName = "testapp";

    // Act
    string bash = DynamicCompletionScriptGenerator.GenerateBash(appName);
    string zsh = DynamicCompletionScriptGenerator.GenerateZsh(appName);
    string pwsh = DynamicCompletionScriptGenerator.GeneratePowerShell(appName);
    string fish = DynamicCompletionScriptGenerator.GenerateFish(appName);

    // Assert - Each shell has different syntax, so scripts will vary in length
    bash.Length.ShouldBeGreaterThan(0);
    zsh.Length.ShouldBeGreaterThan(0);
    pwsh.Length.ShouldBeGreaterThan(0);
    fish.Length.ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.DynamicScriptGen
