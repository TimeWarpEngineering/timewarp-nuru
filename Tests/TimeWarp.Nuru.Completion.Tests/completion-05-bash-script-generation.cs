#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<BashScriptGenerationTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class BashScriptGenerationTests
{
  public static async Task Should_generate_valid_bash_syntax()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);
    builder.AddRoute("deploy {env}", (string env) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");
    bashScript.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_include_app_name_in_script()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "myapp");

    // Assert
    bashScript.ShouldContain("myapp");

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
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("create");
    bashScript.ShouldContain("createorder");
    bashScript.ShouldContain("delete");
    bashScript.ShouldContain("list");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("test --verbose --force", () => 0);
    builder.AddRoute("build --dry-run,-d", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--dry-run");
    bashScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_use_complete_command()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("complete");
    // Should register completion function
    bashScript.ShouldMatch(@"complete\s+-F\s+\w+\s+testapp");

    await Task.CompletedTask;
  }

  public static async Task Should_use_COMPREPLY_variable()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("COMPREPLY");

    await Task.CompletedTask;
  }

  public static async Task Should_use_init_completion()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("_init_completion");

    await Task.CompletedTask;
  }

  public static async Task Should_use_compgen_builtin()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("compgen");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    var builder = new NuruAppBuilder();

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");
    bashScript.ShouldContain("complete");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters_in_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "my-app");

    // Assert
    bashScript.ShouldContain("my-app");
    bashScript.ShouldNotBeEmpty();

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
    string bashScript1 = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string bashScript2 = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript1.ShouldBe(bashScript2, "Scripts should be identical for same input");

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
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Verify Issue #30 requirement
    // Issue #30: typing 'cre<TAB>' should show createorder
    bashScript.ShouldContain("createorder");
    // Both 'create' and 'createorder' should be available
    bashScript.ShouldContain("create");

    await Task.CompletedTask;
  }
}
