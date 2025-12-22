#!/usr/bin/dotnet --
#if !JARIBU_MULTI
return await RunAllTests();
#endif

[TestTag("Completion")]
public class ParameterTypeDetectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ParameterTypeDetectionTests>();

  public static async Task Should_handle_string_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("greet {name}")
      .WithHandler((string name) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Script should be generated
    bashScript.ShouldContain("greet");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_int_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("delay {ms:int}")
      .WithHandler((int ms) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("delay");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_double_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("scale {factor:double}")
      .WithHandler((double factor) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("scale");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_bool_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("set {enabled:bool}")
      .WithHandler((bool enabled) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("set");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_optional_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {tag?}")
      .WithHandler((string env, string? tag) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_catch_all_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("exec {*args}")
      .WithHandler((string[] args) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("exec");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_mixed_parameter_types()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("process {file} {count:int} {verbose:bool}")
      .WithHandler((string file, int count, bool verbose) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("process");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_parameters_with_options()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} --force --tag {version}")
      .WithHandler((string env, string version) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--tag");

    await Task.CompletedTask;
  }

  public static async Task Should_work_across_all_shell_types()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test {value:int}")
      .WithHandler((int value) => 0)
      .AsCommand()
      .Done();

    CompletionScriptGenerator generator = new();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All shells should generate valid scripts
    bashScript.ShouldContain("test");
    zshScript.ShouldContain("test");
    pwshScript.ShouldContain("test");
    fishScript.ShouldContain("test");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_datetime_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("schedule {when:DateTime}")
      .WithHandler((DateTime when) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("schedule");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_guid_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("lookup {id:Guid}")
      .WithHandler((Guid id) => 0)
      .AsQuery()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("lookup");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_routes_with_only_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("{source} {destination}")
      .WithHandler((string source, string destination) => 0)
      .AsCommand()
      .Done();

    // Act
    CompletionScriptGenerator generator = new();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Should generate valid script even without literal commands
    bashScript.ShouldNotBeEmpty();
    bashScript.ShouldContain("# Bash completion for");

    await Task.CompletedTask;
  }
}
