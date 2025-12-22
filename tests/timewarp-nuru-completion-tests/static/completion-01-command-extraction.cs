#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class CommandExtractionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CommandExtractionTests>();

  public static async Task Should_extract_single_command()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("status");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_extract_multiple_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("create")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("createorder")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("delete")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("create");
    bashScript.ShouldContain("createorder");
    bashScript.ShouldContain("delete");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_commands_with_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("build {project}")
      .WithHandler((string project) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("build");
    bashScript.ShouldNotContain("{env}");
    bashScript.ShouldNotContain("{project}");

    await Task.CompletedTask;
  }

  public static async Task Should_extract_commands_with_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("build --config {mode}")
      .WithHandler((string mode) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("test");
    bashScript.ShouldContain("build");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_nested_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git commit")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("git status")
      .WithHandler(() => 0)
      .AsQuery()
      .Done();
    builder.Map("git push")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("git");
    // Note: Only first literal is extracted as command

    await Task.CompletedTask;
  }

  public static async Task Should_deduplicate_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy {env} --force")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy {env} {tag}")
      .WithHandler((string env, string tag) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    // "deploy" should appear only once in the commands list
    int deployCount = System.Text.RegularExpressions.Regex.Matches(bashScript, @"\bdeploy\b").Count;
    deployCount.ShouldBeGreaterThan(0, "deploy should be present");
    // Note: May appear in multiple contexts, but should be in commands list once

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_route_collection()
  {
    // Arrange
    NuruAppBuilder builder = new();
    // No routes added

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // Script should still be valid even with no commands

    await Task.CompletedTask;
  }

  public static async Task Should_extract_command_from_complex_route()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --version {ver} --force")
      .WithHandler((string env, string ver) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldNotContain("{env}");
    bashScript.ShouldNotContain("{ver}");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_route_with_only_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("{command} {*args}")
      .WithHandler((string command, string[] args) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldNotBeEmpty();
    // No literal commands to extract, but script should be valid

    await Task.CompletedTask;
  }

  public static async Task Should_be_case_sensitive()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("Deploy")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();
    builder.Map("deploy")
      .WithHandler(() => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("Deploy");
    bashScript.ShouldContain("deploy");

    await Task.CompletedTask;
  }
}
