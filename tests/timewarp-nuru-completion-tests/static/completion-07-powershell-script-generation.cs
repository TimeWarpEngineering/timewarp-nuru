#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class PowerShellScriptGenerationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PowerShellScriptGenerationTests>();

  public static async Task Should_generate_valid_powershell_syntax()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("deploy {env}", (string env) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldNotBeEmpty();
    pwshScript.ShouldContain("# PowerShell completion for");
    pwshScript.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_include_app_name_in_script()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "myapp");

    // Assert
    pwshScript.ShouldContain("myapp");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("create", () => 0);
    builder.Map("createorder", () => 0);
    builder.Map("delete", () => 0);
    builder.Map("list", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldContain("create");
    pwshScript.ShouldContain("createorder");
    pwshScript.ShouldContain("delete");
    pwshScript.ShouldContain("list");

    await Task.CompletedTask;
  }

  public static async Task Should_use_completion_result()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - PowerShell uses full type name [System.Management.Automation.CompletionResult]
    pwshScript.ShouldContain("[System.Management.Automation.CompletionResult]");

    await Task.CompletedTask;
  }

  public static async Task Should_use_register_argumentcompleter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldContain("Register-ArgumentCompleter");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    NuruAppBuilder builder = new();

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldNotBeEmpty();
    pwshScript.ShouldContain("# PowerShell completion for");
    pwshScript.ShouldContain("Register-ArgumentCompleter");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters_in_app_name()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "my-app");

    // Assert
    pwshScript.ShouldContain("my-app");
    pwshScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_generate_consistent_output()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    CompletionScriptGenerator generator = new();

    // Act
    string pwshScript1 = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string pwshScript2 = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript1.ShouldBe(pwshScript2, "Scripts should be identical for same input");

    await Task.CompletedTask;
  }

  public static async Task Should_include_Issue_30_createorder_command()
  {
    // Arrange - Replicate Issue #30 scenario
    NuruAppBuilder builder = new();
    builder.Map("createorder {product} {quantity:int}", (string product, int quantity) => 0);
    builder.Map("create {item}", (string item) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - Verify Issue #30 requirement
    // Issue #30: typing 'cre<TAB>' should show createorder
    pwshScript.ShouldContain("createorder");
    // Both 'create' and 'createorder' should be available
    pwshScript.ShouldContain("create");

    await Task.CompletedTask;
  }

  public static async Task Should_use_completion_result_type()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - PowerShell uses full type name [System.Management.Automation.CompletionResultType]
    pwshScript.ShouldContain("[System.Management.Automation.CompletionResultType]");

    await Task.CompletedTask;
  }
}
