#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Regression test for kanban 465: EmitterStringUtils.EscapeForStringLiteral must escape
// U+0085 (NEL), U+2028 (LINE SEPARATOR), and U+2029 (PARAGRAPH SEPARATOR) as \u0085 /
// \u2028 / \u2029 in generated C# string literals. Unescaped, these characters are line
// terminators in C# and break the generated code. After escaping, runtime help output and
// --capabilities JSON still contain the raw characters (the compiler re-interprets the
// escapes). Covers fluent descriptions/examples and [NuruRoute]/[NuruRouteExample].
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class UnicodeNewlineEscapesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<UnicodeNewlineEscapesTests>();

  /// <summary>
  /// Fluent route description containing U+2028 must compile and render the raw character.
  /// </summary>
  public static async Task Should_render_fluent_route_description_with_line_separator()
  {
    // Arrange - description embeds U+2028 (LINE SEPARATOR) between words
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("h09fluentdesc")
      .Map("h09-fluent-ls")
        .WithHandler(() => "ok")
        .WithDescription("before\u2028after")
        .AsQuery()
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - app compiles (would fail without \u2028 escape) and raw char is present
    exitCode.ShouldBe(0);
    terminal.OutputContains("before\u2028after").ShouldBeTrue("Route description with U+2028 should render raw character");
  }

  /// <summary>
  /// Fluent .WithExample command/description with unicode newlines must compile and render.
  /// </summary>
  public static async Task Should_render_fluent_example_with_unicode_newlines()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("h09-fluent-ex")
        .WithHandler(() => "ok")
        .WithExample("h09-fluent-ex\u2028cmd", "ex\u0085desc\u2029end")
        .Done()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h09-fluent-ex", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("h09-fluent-ex\u2028cmd").ShouldBeTrue("Example command with U+2028 should render raw character");
    terminal.OutputContains("ex\u0085desc\u2029end").ShouldBeTrue("Example description with U+0085/U+2029 should render raw characters");
  }

  /// <summary>
  /// Endpoint DSL description/examples with U+2028/U+0085/U+2029 must compile and appear
  /// in per-route --help output as raw characters.
  /// </summary>
  public static async Task Should_render_endpoint_route_help_with_unicode_newlines()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H09UnicodeEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["h09-endpoint", "--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("route\u2028desc").ShouldBeTrue("Route description with U+2028 should render raw character");
    terminal.OutputContains("Examples:").ShouldBeTrue();
    terminal.OutputContains("h09-endpoint\u2028run").ShouldBeTrue("Example command with U+2028 should render raw character");
    terminal.OutputContains("nel\u0085sep\u2029para").ShouldBeTrue("Example description with U+0085/U+2029 should render raw characters");
  }

  /// <summary>
  /// Same endpoint annotations must surface raw unicode newlines in --capabilities JSON
  /// string properties after deserialization.
  /// </summary>
  public static async Task Should_roundtrip_unicode_newlines_via_capabilities_json()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map<H09UnicodeEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--capabilities"]);

    string output = terminal.AllOutput;
    CapabilitiesResponse? response = System.Text.Json.JsonSerializer.Deserialize(
      output,
      CapabilitiesJsonSerializerContext.Default.CapabilitiesResponse);

    // Assert
    exitCode.ShouldBe(0);
    response.ShouldNotBeNull();
    EndpointCapability endpoint = response.Endpoints.First(e => e.Pattern.Contains("h09-endpoint"));
    endpoint.Description.ShouldBe("route\u2028desc");
    endpoint.Examples.ShouldNotBeNull();
    endpoint.Examples!.Count.ShouldBe(1);
    endpoint.Examples[0].Command.ShouldBe("h09-endpoint\u2028run");
    endpoint.Examples[0].Description.ShouldBe("nel\u0085sep\u2029para");
  }
}

[NuruRoute("h09-endpoint", Description = "route\u2028desc")]
[NuruRouteExample("h09-endpoint\u2028run", Description = "nel\u0085sep\u2029para")]
public sealed class H09UnicodeEndpoint : ICommand<Unit>
{
  public sealed class Handler : ICommandHandler<H09UnicodeEndpoint, Unit>
  {
    public ValueTask<Unit> Handle(H09UnicodeEndpoint command, CancellationToken cancellationToken) => default;
  }
}

} // namespace TimeWarp.Nuru.Tests.Help
