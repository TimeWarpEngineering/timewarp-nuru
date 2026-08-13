#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 464: Examples support for route help and capabilities.
// Validates that [NuruRouteExample] (Endpoint DSL) and .WithExample() (Fluent DSL) render
// an "Examples:" section after the Options block in per-route "command --help" output,
// one full-width line per example (command, then optional dimmed description) - not a
// table, since help tables truncate cell text.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class RouteExamplesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RouteExamplesTests>();

  public static async Task Should_show_examples_section_via_attribute()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H08DeployEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-deploy", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Examples:").ShouldBeTrue("Should show Examples header");
    terminal.OutputContains("h08-deploy prod").ShouldBeTrue("Should show first example command");
    terminal.OutputContains("Deploy to production").ShouldBeTrue("Should show first example description");
    terminal.OutputContains("h08-deploy staging --dry-run").ShouldBeTrue("Should show second example command (no description)");
  }

  public static async Task Should_show_examples_after_options_section()
  {
    // Arrange - route with both Options and Examples; Examples must render after Options.
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H08BuildEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-build", "--help"]);
    string output = terminal.AllOutput;

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Options:").ShouldBeTrue();
    terminal.OutputContains("Examples:").ShouldBeTrue();
    int optionsIndex = output.IndexOf("Options:", StringComparison.Ordinal);
    int examplesIndex = output.IndexOf("Examples:", StringComparison.Ordinal);
    (examplesIndex > optionsIndex).ShouldBeTrue("Examples section should render after Options section");
  }

  public static async Task Should_show_example_via_fluent_with_description()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("h08-fluent-cmd")
        .WithHandler(() => "ok")
        .WithExample("h08-fluent-cmd --verbose", "Run verbosely")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-fluent-cmd", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("h08-fluent-cmd --verbose").ShouldBeTrue();
    terminal.OutputContains("Run verbosely").ShouldBeTrue();
  }

  public static async Task Should_show_example_via_fluent_without_description()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("h08-fluent-nodesc")
        .WithHandler(() => "ok")
        .WithExample("h08-fluent-nodesc")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-fluent-nodesc", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("h08-fluent-nodesc").ShouldBeTrue();
  }

  public static async Task Should_show_examples_via_fluent_in_group()
  {
    // Arrange - GroupEndpointBuilder.WithExample() (Fluent DSL routes within a group)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithGroupPrefix("h08-grp")
        .Map("status")
          .WithHandler(() => "ok")
          .WithExample("h08-grp status", "Check group status")
          .Done()
      .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-grp", "status", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("h08-grp status").ShouldBeTrue();
    terminal.OutputContains("Check group status").ShouldBeTrue();
  }

  public static async Task Should_not_show_examples_section_when_none_declared()
  {
    // Arrange - negative assertion: no [NuruRouteExample] and no .WithExample()
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("h08-status").WithHandler(() => "ok").WithDescription("Show status").Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-status", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Show status").ShouldBeTrue();
    terminal.OutputContains("Examples:").ShouldBeFalse("Should not show Examples header when no examples declared");
  }

  public static async Task Should_render_special_characters_in_examples_verbatim()
  {
    // Regression class for kanban 454-002 / help-07: examples are emitted as plain string
    // literals (EmitterStringUtils.EscapeForStringLiteral), so quotes/braces/backslashes
    // must compile and render verbatim, not as interpolation holes or broken string literals.
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H08SpecialCharsEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h08-tool", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("h08-tool \"quoted\" {arg}").ShouldBeTrue("Example command with quotes/braces should render verbatim");
    terminal.OutputContains("Path: C:\\tools\\thing").ShouldBeTrue("Example description with backslashes should render verbatim");
  }
}

[NuruRoute("h08-deploy", Description = "Deploy to an environment")]
[NuruRouteExample("h08-deploy prod", Description = "Deploy to production")]
[NuruRouteExample("h08-deploy staging --dry-run")]
public sealed class H08DeployEndpoint : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<H08DeployEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H08DeployEndpoint command, CancellationToken cancellationToken) => default;
  }
}

[NuruRoute("h08-build", Description = "Build a project")]
[NuruRouteExample("h08-build --verbose", Description = "Build with verbose output")]
public sealed class H08BuildEndpoint : ICommand<Unit>
{
  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  public sealed class Handler : ICommandHandler<H08BuildEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H08BuildEndpoint command, CancellationToken cancellationToken) => default;
  }
}

[NuruRoute("h08-tool", Description = "A tool with special characters in its examples")]
[NuruRouteExample("h08-tool \"quoted\" {arg}", Description = "Path: C:\\tools\\thing")]
public sealed class H08SpecialCharsEndpoint : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<H08SpecialCharsEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H08SpecialCharsEndpoint command, CancellationToken cancellationToken) => default;
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
