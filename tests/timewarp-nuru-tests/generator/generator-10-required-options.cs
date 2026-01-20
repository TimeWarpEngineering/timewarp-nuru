#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test: Required options should only match when present
// Route 1 has REQUIRED --mode option (higher specificity)
// Route 2 has no options (lower specificity)
// Input "round 2.5" should match Route 2, NOT Route 1

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
