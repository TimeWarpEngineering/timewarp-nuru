#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

// Required and optional flag/value combinations from the option-matching matrix.
// The other option-matching subsets live beside this file.

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionModifierMatrixTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionModifierMatrixTests>();

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
}

} // namespace TimeWarp.Nuru.Tests.Routing
