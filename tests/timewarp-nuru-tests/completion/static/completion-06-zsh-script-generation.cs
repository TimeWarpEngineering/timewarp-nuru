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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("create")
      .WithHandler(() => { })
      .AsCommand()
      .Done();
    builder.Map("createorder")
      .WithHandler(() => { })
      .AsCommand()
      .Done();
    builder.Map("delete")
      .WithHandler(() => { })
      .AsCommand()
      .Done();
    builder.Map("list")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("test --verbose --force")
      .WithHandler(() => { })
      .AsCommand()
      .Done();
    builder.Map("build --dry-run,-d")
      .WithHandler(() => { })
      .AsCommand()
      .Done();

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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.Map("version")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

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
    builder.Map("createorder {product} {quantity:int}")
      .WithHandler((string product, int quantity) => 0)
      .AsCommand()
      .Done();
    builder.Map("create {item}")
      .WithHandler((string item) => 0)
      .AsCommand()
      .Done();

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
