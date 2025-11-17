#!/usr/bin/dotnet --

using TimeWarp.Nuru;
using TimeWarp.Nuru.Completion;
using Shouldly;

return await RunTests<EnumCompletionTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class EnumCompletionTests
{
  public enum LogLevel
  {
    Debug,
    Info,
    Warning,
    Error
  }

  public enum Environment
  {
    Development,
    Staging,
    Production
  }

  public static async Task Should_handle_enum_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("log {level:LogLevel}", (LogLevel level) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert - Command should be present
    bashScript.ShouldContain("log");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_multiple_enum_parameters()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env:Environment} {level:LogLevel}",
      (Environment env, LogLevel level) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_optional_enum_parameter()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("log {message} {level:LogLevel?}",
      (string message, LogLevel? level) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("log");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_options()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {env:Environment} --force --verbose",
      (Environment env) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("--force");
    bashScript.ShouldContain("--verbose");

    await Task.CompletedTask;
  }

  public static async Task Should_work_with_all_shell_types()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("setlevel {level:LogLevel}", (LogLevel level) => 0);

    var generator = new CompletionScriptGenerator();

    // Act
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");
    string zshScript = generator.GenerateZsh(builder.EndpointCollection, "testapp");
    string pwshScript = generator.GeneratePowerShell(builder.EndpointCollection, "testapp");
    string fishScript = generator.GenerateFish(builder.EndpointCollection, "testapp");

    // Assert - All shells should generate valid scripts
    bashScript.ShouldContain("setlevel");
    zshScript.ShouldContain("setlevel");
    pwshScript.ShouldContain("setlevel");
    fishScript.ShouldContain("setlevel");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_in_complex_route()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("deploy {app} {env:Environment} --version {ver} --log {level:LogLevel}",
      (string app, Environment env, string ver, LogLevel level) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("deploy");
    bashScript.ShouldContain("--version");
    bashScript.ShouldContain("--log");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_mixed_parameter_types()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("run {file} {count:int} {env:Environment} {enabled:bool}",
      (string file, int count, Environment env, bool enabled) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("run");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Should_handle_enum_with_catch_all()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.AddRoute("execute {level:LogLevel} {*args}",
      (LogLevel level, string[] args) => 0);

    // Act
    var generator = new CompletionScriptGenerator();
    string bashScript = generator.GenerateBash(builder.EndpointCollection, "testapp");

    // Assert
    bashScript.ShouldContain("execute");
    bashScript.ShouldNotBeEmpty();

    await Task.CompletedTask;
  }
}
