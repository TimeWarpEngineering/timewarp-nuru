#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<PowerShellScriptGenerationTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class PowerShellScriptGenerationTests
{
  public static async Task Should_generate_valid_powershell_syntax()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("deploy {env}", (string env) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "myapp");

    // Assert
    pwshScript.ShouldContain("myapp");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("create", () => 0);
    builder.AddRoute("createorder", () => 0);
    builder.AddRoute("delete", () => 0);
    builder.AddRoute("list", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - PowerShell uses full type name [System.Management.Automation.CompletionResult]
    pwshScript.ShouldContain("[System.Management.Automation.CompletionResult]");

    await Task.CompletedTask;
  }

  public static async Task Should_use_register_argumentcompleter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert
    pwshScript.ShouldContain("Register-ArgumentCompleter");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    var builder = new NuruAppBuilder();

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "my-app");

    // Assert
    pwshScript.ShouldContain("my-app");
    pwshScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_generate_consistent_output()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("version", () => 0);

    var generator = new CompletionScriptGenerator();

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
    var builder = new NuruAppBuilder();
    builder.AddRoute("createorder {product} {quantity:int}", (string product, int quantity) => 0);
    builder.AddRoute("create {item}", (string item) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
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
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");

    // Assert - PowerShell uses full type name [System.Management.Automation.CompletionResultType]
    pwshScript.ShouldContain("[System.Management.Automation.CompletionResultType]");

    await Task.CompletedTask;
  }
}
