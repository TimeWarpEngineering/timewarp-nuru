#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Options
{

/// <summary>
/// Tests for optional flag with optional value (--flag? {value?}).
/// Pattern: --config? {mode?}
/// Both flag and value are optional.
/// </summary>
[TestTag("Options")]
public class OptionalFlagOptionalValueTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionalFlagOptionalValueTests>();

  public static async Task Should_match_without_optional_flag()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
        .WithHandler((string? mode) => $"mode:{mode ?? "null"}")
        .AsCommand()
        .Done()
      .Build();

    // Act - no --config flag provided
    int exitCode = await app.RunAsync(["build"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:null").ShouldBeTrue();
  }

  public static async Task Should_match_with_flag_and_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
        .WithHandler((string? mode) => $"mode:{mode ?? "null"}")
        .AsCommand()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["build", "--config", "debug"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:debug").ShouldBeTrue();
  }

  public static async Task Should_match_with_flag_but_no_value()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("build --config? {mode?}")
        .WithHandler((string? mode) => $"mode:{mode ?? "null"}")
        .AsCommand()
        .Done()
      .Build();

    // Act - flag present but no value
    int exitCode = await app.RunAsync(["build", "--config"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("mode:null").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Options
