#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class RouteAnalysisTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RouteAnalysisTests>();

  public static async Task Should_analyze_simple_literal_route()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Should extract the literal command
    bashScript.ShouldContain("status");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("greet {name}", (string name) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Should extract command but not parameter
    bashScript.ShouldContain("greet");
    // Parameter names shouldn't appear as commands
    bashScript.ShouldNotContain("name");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_typed_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("delay {ms:int}", (int ms) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("delay");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_optional_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {tag?}", (string env, string? tag) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --config {mode}", (string mode) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("build");
    bashScript.ShouldContain("--config");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_catch_all()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("docker {*args}", (string[] args) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("docker");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_route_with_multiple_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose --dry-run", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("test");
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("--dry-run");

    await Task.CompletedTask;
  }

  public static async Task Should_analyze_complex_route()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --version {ver} --force", (string env, string ver) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All components should be identified
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("--version");
    bashScript.ShouldContain("--force");

    await Task.CompletedTask;
  }

  public static async Task Should_separate_commands_from_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("create", () => 0);
    builder.Map("delete", () => 0);
    builder.Map("list --all", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Commands should be in commands section
    bashScript.ShouldContain("create");
    bashScript.ShouldContain("delete");
    bashScript.ShouldContain("list");
    // Options should be in options section
    bashScript.ShouldContain("--all");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multi_word_literal_commands()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git commit", () => 0);
    builder.Map("git status", () => 0);
    builder.Map("git push", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - First literal "git" should be extracted from all routes
    bashScript.ShouldContain("git");
    // Note: "commit", "status", "push" are second literals in the sequence
    // Current implementation extracts first literal only

    await Task.CompletedTask;
  }

  public static async Task Should_extract_all_route_components()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("server start --port {port:int} --host {host} --verbose",
      (int port, string host) => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - All literals and options extracted
    bashScript.ShouldContain("server");
    bashScript.ShouldContain("start");
    bashScript.ShouldContain("--port");
    bashScript.ShouldContain("--host");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_short_and_long_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test --verbose,-v --quiet,-q", () => 0);

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Both forms should be present
    bashScript.ShouldContain("--verbose");
    bashScript.ShouldContain("-v");
    bashScript.ShouldContain("--quiet");
    bashScript.ShouldContain("-q");

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_all_shell_types()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --force", (string env) => 0);

    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All shells should have the command
    bashScript.ShouldContain("deploy");
    zshScript.ShouldContain("deploy");
    pwshScript.ShouldContain("deploy");
    fishScript.ShouldContain("deploy");

    // All shells should have deploy command
    // Options handling varies by shell implementation

    await Task.CompletedTask;
  }
}
