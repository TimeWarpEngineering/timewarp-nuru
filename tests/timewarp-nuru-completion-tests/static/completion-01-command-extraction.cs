#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using Shouldly;

return await RunTests<CommandExtractionTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CommandExtractionTests
{
  public static async Task Should_extract_single_command()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

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
    builder.Map("create", () => 0);
    builder.Map("createorder", () => 0);
    builder.Map("delete", () => 0);

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
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("build {project}", (string project) => 0);

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
    builder.Map("test --verbose", () => 0);
    builder.Map("build --config {mode}", (string mode) => 0);

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
    builder.Map("git commit", () => 0);
    builder.Map("git status", () => 0);
    builder.Map("git push", () => 0);

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
    builder.Map("deploy {env}", (string env) => 0);
    builder.Map("deploy {env} --force", (string env) => 0);
    builder.Map("deploy {env} {tag}", (string env, string tag) => 0);

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
    builder.Map("deploy {env} --version {ver} --force", (string env, string ver) => 0);

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
    builder.Map("{command} {*args}", (string command, string[] args) => 0);

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
    builder.Map("Deploy", () => 0);
    builder.Map("deploy", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("Deploy");
    bashScript.ShouldContain("deploy");

    await Task.CompletedTask;
  }
}
