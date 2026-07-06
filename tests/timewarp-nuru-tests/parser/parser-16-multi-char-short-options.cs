#!/usr/bin/dotnet --

#region Purpose
// Regression tests for kanban 454-005/454-014: multi-character single-dash options
// (-bl, -verbosity) are supported end-to-end (the lexer supported them since Oct 2025
// but the parser rejected them), and OptionMatcher matches declared forms EXACTLY —
// the undocumented POSIX-grouping heuristic (-e matched -help via Contains) is removed.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Parser
{

[TestTag("Parser")]
public class MultiCharShortOptionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<MultiCharShortOptionTests>();

  public static async Task Should_parse_multi_char_short_flag()
  {
    // Arrange & Act - dotnet-style binary logger flag
    CompiledRoute route = PatternParser.Parse("build -bl");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);
    route.OptionMatchers[0].MatchPattern.ShouldBe("-bl");
    route.OptionMatchers[0].ExpectsValue.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_parse_multi_char_short_with_value()
  {
    // Arrange & Act - msbuild-style verbosity
    CompiledRoute route = PatternParser.Parse("build -verbosity {level}");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);
    route.OptionMatchers[0].MatchPattern.ShouldBe("-verbosity");
    route.OptionMatchers[0].ExpectsValue.ShouldBeTrue();
    route.OptionMatchers[0].ParameterName.ShouldBe("level");

    await Task.CompletedTask;
  }

  public static async Task Should_parse_long_option_with_multi_char_short_alias()
  {
    // Arrange & Act
    CompiledRoute route = PatternParser.Parse("build --binary-log,-bl");

    // Assert
    route.ShouldNotBeNull();
    route.OptionMatchers.Count.ShouldBe(1);
    route.OptionMatchers[0].MatchPattern.ShouldBe("--binary-log");
    route.OptionMatchers[0].AlternateForm.ShouldBe("-bl");

    await Task.CompletedTask;
  }

  public static async Task Should_match_short_forms_exactly_not_grouped()
  {
    // Arrange - option with single-char short alias
    CompiledRoute route = PatternParser.Parse("show --edit,-e");
    OptionMatcher option = route.OptionMatchers[0];

    // Assert - exact matches work
    option.TryMatch("--edit", out _).ShouldBeTrue();
    option.TryMatch("-e", out _).ShouldBeTrue();

    // Assert - the removed grouping heuristic must NOT match:
    // '-e' previously matched ANY -xyz containing the letter 'e'
    option.TryMatch("-help", out _).ShouldBeFalse();
    option.TryMatch("-ea", out _).ShouldBeFalse();
    option.TryMatch("-verbose", out _).ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_not_cross_match_multi_char_and_single_char_shorts()
  {
    // Arrange - multi-char short is a distinct token from its prefix chars
    CompiledRoute route = PatternParser.Parse("build -bl");
    OptionMatcher option = route.OptionMatchers[0];

    // Assert
    option.TryMatch("-bl", out _).ShouldBeTrue();
    option.TryMatch("-b", out _).ShouldBeFalse();
    option.TryMatch("-l", out _).ShouldBeFalse();
    option.TryMatch("-blx", out _).ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Should_route_multi_char_short_end_to_end()
  {
    // Arrange - exercises the source-GENERATED matcher path, not just the parser.
    // Boolean flags bind false when absent (the route still matches), so the handler
    // takes the bool. NOTE: a companion plain "p16-build" route would trip NURU_R003
    // (kanban 454-013 — flag-optionality semantics), so this app has one route.
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("p16-build -bl").WithHandler((bool bl) => bl ? "binary-log-on" : "binary-log-off").AsQuery().Done()
      .Build();

    // Act & Assert - multi-char short flag present binds true
    int exitCode = await app.RunAsync(["p16-build", "-bl"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("binary-log-on").ShouldBeTrue();

    // Act & Assert - flag absent binds false
    terminal.ClearOutput();
    exitCode = await app.RunAsync(["p16-build"]);
    exitCode.ShouldBe(0);
    terminal.OutputContains("binary-log-off").ShouldBeTrue();
  }
}

} // namespace TimeWarp.Nuru.Tests.Parser
