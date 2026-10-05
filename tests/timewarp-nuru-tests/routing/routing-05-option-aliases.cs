#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

#pragma warning disable RCS1163 // Unused parameter - expected in negative test cases

// Long and short option aliases, including optional flags and optional values.
// The other option-matching subsets live beside this file.

namespace TimeWarp.Nuru.Tests.Routing
{

[TestTag("Routing")]
public class OptionAliasMatchingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OptionAliasMatchingTests>();

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
