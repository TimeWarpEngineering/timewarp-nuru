#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionMatchingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionMatchingTests>();

  public static async Task Should_match_required_option_build_config_debug()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config {mode}")
      .WithHandler((string mode) => $"mode:{mode}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:debug").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_missing_required_option_build()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config {mode}")
      .WithHandler((string mode) => 0)
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required option

    await Task.CompletedTask;
  }

  public static async Task Should_match_required_flag_optional_value_build_config_release()
  {
    // Behavior #2: Required flag + Optional value (--config {mode?})
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:release").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_required_flag_optional_value_build_config_no_value()
  {
    // Behavior #2: Flag present without value should bind null
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_required_flag_optional_value_build_missing_flag()
  {
    // Behavior #2: Missing required flag should not match
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config {mode?}")
      .WithHandler((string? mode) => 0)
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required flag

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_config_release()
  {
    // Behavior #3: Optional flag + Optional value (--config? {mode?})
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:release").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_config_no_value()
  {
    // Behavior #3: Optional flag present without value
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_optional_value_build_missing_flag()
  {
    // Behavior #3: Optional flag omitted entirely
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_required_value_build_config_debug()
  {
    // Behavior #4: Optional flag + Required value (--config? {mode})
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:debug").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_required_value_build_missing_flag()
  {
    // Behavior #4: Optional flag omitted entirely
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_optional_flag_required_value_build_config_no_value()
  {
    // Behavior #4: Flag present without required value should not match
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config? {mode}")
      .WithHandler((string? mode) => 0)
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(1); // Flag present but missing required value

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_verbose_true()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_boolean_flag_build_false()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_tag_v1_0_verbose()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag {t?} --verbose")
      .WithHandler((string e, string? t, bool verbose) => $"e:{e}|t:{t ?? "NULL"}|verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod", "--tag", "v1.0", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:prod").ShouldBeTrue();
    terminal.OutputContains("t:v1.0").ShouldBeTrue();
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_mixed_required_optional_deploy_env_prod_defaults()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag? {t?} --verbose")
      .WithHandler((string e, string? t, bool verbose) => $"e:{e}|t:{t ?? "NULL"}|verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--env", "prod"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("e:prod").ShouldBeTrue();
    terminal.OutputContains("t:NULL").ShouldBeTrue();
    terminal.OutputContains("verbose:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_mixed_missing_required_deploy_tag_v1_0()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("deploy --env {e} --tag? {t?} --verbose").WithHandler((string e, string? t, bool verbose) => 0).AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["deploy", "--tag", "v1.0"]);

    // Assert
    exitCode.ShouldBe(1); // Missing required --env

    await Task.CompletedTask;
  }

  public static async Task Should_match_typed_option_server_port_8080()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("server --port {num:int}")
      .WithHandler((int num) => $"num:{num}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "8080"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("num:8080").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_not_match_typed_option_server_port_abc()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("server --port {num:int}")
      .WithHandler((int num) => 0)
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["server", "--port", "abc"]);

    // Assert
    exitCode.ShouldBe(1); // Type conversion failure

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_verbose()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose,-v")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_option_alias_build_v()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose,-v")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-v"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_long_form()
  {
    // Arrange - Test optional boolean flag with alias using long form
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose,-v?")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--verbose"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_short_form()
  {
    // Arrange - Test optional boolean flag with alias using short form
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose,-v?")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-v"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:True").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_boolean_omitted()
  {
    // Arrange - Test optional boolean flag with alias omitted
    // Pattern: --verbose,-v? (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --verbose,-v?")
      .WithHandler((bool verbose) => $"verbose:{verbose}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("verbose:False").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_long_form()
  {
    // Arrange - Test optional flag with alias and value using long form
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("backup {source} --output,-o? {file}")
      .WithHandler((string source, string? file) => $"source:{source}|file:{file ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data", "--output", "result.tar"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("file:result.tar").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_short_form()
  {
    // Arrange - Test optional flag with alias and value using short form
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("backup {source} --output,-o? {file}")
      .WithHandler((string source, string? file) => $"source:{source}|file:{file ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data", "-o", "result.tar"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("file:result.tar").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_value_omitted()
  {
    // Arrange - Test optional flag with alias omitted entirely
    // Pattern: --output,-o? {file} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("backup {source} --output,-o? {file}")
      .WithHandler((string source, string? file) => $"source:{source}|file:{file ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["backup", "/data"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("file:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_long_form()
  {
    // Arrange - Test optional flag with alias and optional value using long form
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config,-c? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:debug").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_short_form()
  {
    // Arrange - Test optional flag with alias and optional value using short form
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config,-c? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-c", "release"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:release").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_flag_omitted()
  {
    // Arrange - Test optional flag with alias and optional value with flag omitted
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config,-c? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_value_omitted_long()
  {
    // Arrange - Test optional flag present without value (long form)
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config,-c? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_match_optional_flag_with_alias_and_optional_value_value_omitted_short()
  {
    // Arrange - Test optional flag present without value (short form)
    // Pattern: --config,-c? {mode?} (per optional-flag-alias-syntax.md)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("build --config,-c? {mode?}")
      .WithHandler((string? mode) => $"mode:{mode ?? "NULL"}")
      .AsCommand()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "-c"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:NULL").ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing
