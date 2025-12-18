#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class ZshScriptGenerationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ZshScriptGenerationTests>();

  public static async Task Should_generate_valid_zsh_syntax()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("deploy {env}", (string env) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldNotBeEmpty();
    zshScript.ShouldContain("# Zsh completion for");
    zshScript.ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_include_app_name_in_script()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "myapp");

    // Assert
    zshScript.ShouldContain("myapp");

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
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldContain("create");
    zshScript.ShouldContain("createorder");
    zshScript.ShouldContain("delete");
    zshScript.ShouldContain("list");

    await Task.CompletedTask;
  }

  public static async Task Should_include_all_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose --force", () => 0);
    builder.Map("build --dry-run,-d", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldContain("--verbose");
    zshScript.ShouldContain("--force");
    zshScript.ShouldContain("--dry-run");
    zshScript.ShouldContain("-d");

    await Task.CompletedTask;
  }

  public static async Task Should_use_compdef()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldContain("compdef");

    await Task.CompletedTask;
  }

  public static async Task Should_use_describe_and_arguments()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert - Zsh uses _describe and _arguments, not compadd
    zshScript.ShouldContain("_describe");
    zshScript.ShouldContain("_arguments");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    NuruAppBuilder builder = new();

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript.ShouldNotBeEmpty();
    zshScript.ShouldContain("# Zsh completion for");
    zshScript.ShouldContain("compdef");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_special_characters_in_app_name()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "my-app");

    // Assert
    zshScript.ShouldContain("my-app");
    zshScript.ShouldNotBeEmpty();

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
    string zshScript1 = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string zshScript2 = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert
    zshScript1.ShouldBe(zshScript2, "Scripts should be identical for same input");

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
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");

    // Assert - Verify Issue #30 requirement
    // Issue #30: typing 'cre<TAB>' should show createorder
    zshScript.ShouldContain("createorder");
    // Both 'create' and 'createorder' should be available
    zshScript.ShouldContain("create");

    await Task.CompletedTask;
  }
}
