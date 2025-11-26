#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<FishScriptGenerationTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class FishScriptGenerationTests
{
  public static async Task Should_generate_valid_fish_syntax()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("deploy {env}", (string env) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript.ShouldNotBeEmpty();
    fishScript.ShouldContain("# Fish completion for");
    fishScript.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_include_app_name_in_script()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "myapp");

    // Assert
    fishScript.ShouldContain("myapp");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("create", () => 0);
    builder.Map("createorder", () => 0);
    builder.Map("delete", () => 0);
    builder.Map("list", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript.ShouldContain("create");
    fishScript.ShouldContain("createorder");
    fishScript.ShouldContain("delete");
    fishScript.ShouldContain("list");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("test --verbose --force", () => 0);
    builder.Map("build --dry-run,-d", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript.ShouldContain("verbose");
    fishScript.ShouldContain("force");
    fishScript.ShouldContain("dry-run");
    fishScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_use_complete_command()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - Fish uses 'complete -c' commands
    fishScript.ShouldContain("complete -c");

    await Task.CompletedTask;
  }

  public static async Task Should_use_dash_a_for_commands()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - Fish uses '-a' flag for arguments
    fishScript.ShouldContain("-a");

    await Task.CompletedTask;
  }

  public static async Task Should_use_dash_d_for_descriptions()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - Fish uses '-d' flag for descriptions
    fishScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    var builder = new NuruAppBuilder();

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript.ShouldNotBeEmpty();
    fishScript.ShouldContain("# Fish completion for");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters_in_app_name()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "my-app");

    // Assert
    fishScript.ShouldContain("my-app");
    fishScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_generate_consistent_output()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    var generator = new CompletionScriptGenerator();

    // Act
    string fishScript1 = generator.GenerateFish(builder.EndpointCollection, "testapp");
    string fishScript2 = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert
    fishScript1.ShouldBe(fishScript2, "Scripts should be identical for same input");

    await Task.CompletedTask;
  }

  public static async Task Should_include_Issue_30_createorder_command()
  {
    // Arrange - Replicate Issue #30 scenario
    var builder = new NuruAppBuilder();
    builder.Map("createorder {product} {quantity:int}", (string product, int quantity) => 0);
    builder.Map("create {item}", (string item) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - Verify Issue #30 requirement
    // Issue #30: typing 'cre<TAB>' should show createorder
    fishScript.ShouldContain("createorder");
    // Both 'create' and 'createorder' should be available
    fishScript.ShouldContain("create");

    await Task.CompletedTask;
  }
}
