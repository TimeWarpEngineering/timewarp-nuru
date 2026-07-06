#!/usr/bin/dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Regression test for kanban 454-002: HelpEmitter emitted the app description into a
// GENERATED interpolated string without escaping braces, so a description containing
// route syntax like "greet {name}" produced $"  greet {name}" -> CS0103 and the
// generated code did not compile. Same latent issue for quotes/backslashes in the
// version string. The mere presence of these apps in the compilation exercises the
// emitter; the assertions verify the text renders verbatim.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class DescriptionSpecialCharsTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DescriptionSpecialCharsTests>();

  /// <summary>
  /// App description containing route-syntax braces must compile and render verbatim.
  /// </summary>
  public static async Task Should_render_app_description_containing_braces()
  {
    // Arrange - description deliberately contains {name} route syntax
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("bracesapp")
      .WithDescription("Demo CLI: use greet {name} to say hello")
      .Map("h07-greet {name}").WithHandler((string name) => $"Hello {name}").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - braces render verbatim, not as interpolation holes
    exitCode.ShouldBe(0);
    terminal.OutputContains("use greet {name} to say hello").ShouldBeTrue("App description with braces should render verbatim");
  }

  /// <summary>
  /// App description containing quotes and backslashes must compile and render verbatim.
  /// </summary>
  public static async Task Should_render_app_description_containing_quotes_and_backslashes()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("quotesapp")
      .WithDescription("Say \"hello\" via C:\\tools")
      .Map("h07-quote").WithHandler(() => "ok").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Say \"hello\" via C:\\tools").ShouldBeTrue("App description with quotes/backslashes should render verbatim");
  }

  /// <summary>
  /// Route descriptions containing braces flow through the commands table as plain
  /// literals and must render verbatim.
  /// </summary>
  public static async Task Should_render_route_description_containing_braces()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("routedescapp")
      .Map("h07-deploy {env}").WithHandler((string env) => $"Deployed {env}").WithDescription("Deploy: pass {env} like prod").AsQuery().Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("pass {env} like prod").ShouldBeTrue("Route description with braces should render verbatim");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
