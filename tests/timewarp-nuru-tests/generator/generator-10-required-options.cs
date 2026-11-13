#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Pins that a route with a required option only matches when the option is present,
// and that the lower-specificity no-option route wins otherwise.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Generator.RequiredOptions
{

[TestTag("Generator")]
[TestTag("RequiredOptions")]
public sealed class RequiredOptionsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RequiredOptionsTests>();

  public static async Task Should_match_route_without_option_when_option_not_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("round {value:double} --mode {mode}")
        .WithHandler((double value, string mode) => $"WITH MODE: {value} -> {mode}")
        .Done()
      .Map("round {value:double}")
        .WithHandler((double value) => $"NO MODE: {value}")
        .Done()
      .Build();

    // Act
    await app.RunAsync(["round", "2.5"]);

    // Assert
    terminal.OutputContains("NO MODE: 2.5").ShouldBeTrue();
  }

  public static async Task Should_match_route_with_option_when_option_provided()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("round {value:double} --mode {mode}")
        .WithHandler((double value, string mode) => $"WITH MODE: {value} -> {mode}")
        .Done()
      .Map("round {value:double}")
        .WithHandler((double value) => $"NO MODE: {value}")
        .Done()
      .Build();

    // Act
    await app.RunAsync(["round", "2.5", "--mode", "up"]);

    // Assert
    terminal.OutputContains("WITH MODE: 2.5 -> up").ShouldBeTrue();
  }
}

} // namespace
