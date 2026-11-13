#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins unknown-option handling for required, discriminator, and literal-less routes and flag-typo edge cases (task 515); split from routing-37-unknown-options.cs.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Routing.UnknownOptionReview
{

[TestTag("Routing")]
public class UnknownOptionReviewTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UnknownOptionReviewTests>();

  // Review M1: a missing required value option skips the route, but only after the unknown
  // option is recorded, so the typo still gets a suggestion
  public static async Task Should_suggest_required_value_option_for_close_typo()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--foldr", "x"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--foldr'. Did you mean '--folder'?").ShouldBeTrue();
    terminal.OutputContains("created:").ShouldBeFalse();
  }

  // Review M1: an unbound discriminator flag (no bool parameter) must be present to match
  public static async Task Should_suggest_discriminator_flag_for_close_typo()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("commit --amend").WithHandler(() => "commit:amend")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["commit", "--ammend"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--ammend'. Did you mean '--amend'?").ShouldBeTrue();
    terminal.OutputContains("commit:").ShouldBeFalse();
  }

  public static async Task Should_reject_unknown_option_beside_present_required_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--folder", "f", "--repo", "x"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--repo'").ShouldBeTrue();
    terminal.OutputContains("created:").ShouldBeFalse();
  }

  public static async Task Should_still_run_route_with_required_option_present()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("kanban create {title} --folder {folder}")
        .WithHandler((string title, string folder) => $"created:{title}|{folder}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["kanban", "create", "Title", "--folder", "f"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("created:Title|f").ShouldBeTrue();
  }

  // Review M2: a route with no leading literal records the unknown option once the rest of the
  // route matches, before a missing required flag skips it
  public static async Task Should_suggest_discriminator_flag_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--check-updates").WithHandler(() => "check-updates")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-update"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--check-update'. Did you mean '--check-updates'?").ShouldBeTrue();
    terminal.OutputContains("check-updates").ShouldBeFalse();
  }

  // Review M2: the token after the typo is its value, so it does not count as an extra positional
  public static async Task Should_suggest_required_value_option_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output}")
        .WithHandler((string file, string output) => $"readme:{file}|{output}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  public static async Task Should_suggest_optional_value_option_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output? {output?}")
        .WithHandler((string file, string? output) => $"readme:{file}|{output}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // A missing value still skips the route; with no unknown option it stays Unknown command
  public static async Task Should_keep_unknown_command_for_missing_value_on_route_without_literal()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --count {count:int}")
        .WithHandler((string file, int count) => $"readme:{file}|{count}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--count"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // Review M3: a flag typo takes no value, so a stray word after it is an extra positional and the
  // route does not fit
  public static async Task Should_keep_unknown_command_for_flag_typo_with_extra_word()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("--check-updates").WithHandler(() => "check-updates")
      .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--check-update", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("check-updates").ShouldBeFalse();
  }

  public static async Task Should_keep_unknown_command_for_flag_typo_with_extra_word_after_parameter()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --check-updates")
        .WithHandler((string file) => $"readme:{file}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--check-update", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // The route has a value option too, but the typo is closest to the flag
  public static async Task Should_keep_unknown_command_for_flag_typo_beside_value_option()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output} --force")
        .WithHandler((string file, string output, bool force) => $"readme:{file}|{output}|{force}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--forc", "extra"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown command").ShouldBeTrue();
    terminal.ErrorContains("Did you mean").ShouldBeFalse();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }

  // The value-option typo on the same route still owns its value and suggests
  public static async Task Should_suggest_value_option_typo_beside_flag()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("{file} --output {output} --force")
        .WithHandler((string file, string output, bool force) => $"readme:{file}|{output}|{force}")
        .AsCommand().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["readme", "--outpt", "out"]);

    // Assert
    exitCode.ShouldBe(1);
    terminal.ErrorContains("Unknown option '--outpt'. Did you mean '--output'?").ShouldBeTrue();
    terminal.OutputContains("readme:").ShouldBeFalse();
  }
}

} // namespace TimeWarp.Nuru.Tests.Routing.UnknownOptionReview
