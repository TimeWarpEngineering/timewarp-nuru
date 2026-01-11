#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class BashScriptGenerationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<BashScriptGenerationTests>();

  public static async Task Should_generate_valid_bash_syntax()
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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "myapp");

    // Assert
    bashScript.ShouldContain("myapp");

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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("COMPREPLY");

    await Task.CompletedTask;
  }

  public static async Task Should_use_init_completion()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("_init_completion");

    await Task.CompletedTask;
  }

  public static async Task Should_use_compgen_builtin()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("compgen");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    NuruAppBuilder builder = new();

    // Act
    CompletionScriptGenerator generator = new();
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
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "my-app");

    // Assert
    bashScript.ShouldContain("my-app");
    bashScript.ShouldNotBeEmpty();

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
    string bashScript1 = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string bashScript2 = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript1.ShouldBe(bashScript2, "Scripts should be identical for same input");

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
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Verify Issue #30 requirement
    // Issue #30: typing 'cre<TAB>' should show createorder
    bashScript.ShouldContain("createorder");
    // Both 'create' and 'createorder' should be available
    bashScript.ShouldContain("create");

    await Task.CompletedTask;
  }
}
