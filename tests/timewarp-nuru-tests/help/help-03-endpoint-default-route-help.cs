#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter - parameters must match route pattern names for binding
#pragma warning disable CA1849 // Call async methods when in async method

#region Purpose
// Tests for Issue #179: [NuruRoute("")] endpoint default routes intercepting --help.
// Validates that --help works correctly when a default route is defined via endpoint DSL.
// Ensures endpoint-based default routes behave identically to fluent API default routes.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class EndpointDefaultRouteHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EndpointDefaultRouteHelpTests>();

  /// <summary>
  /// Issue #179: Endpoint with [NuruRoute("")] and parameter should not intercept --help.
  /// </summary>
  public static async Task Should_show_help_when_endpoint_default_route_with_parameter_exists()
  {
    // Arrange - endpoint with empty route pattern and parameter
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .DiscoverEndpoints()
      .Build();

    // Act - invoke with --help
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - should show help, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when --help is requested");
    terminal.OutputContains("testapp").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Issue #179: Endpoint with [NuruRoute("")] and option should not intercept --help.
  /// </summary>
  public static async Task Should_show_help_when_endpoint_default_route_with_option_exists()
  {
    // Arrange - default endpoint with option (same endpoint, just testing option scenario)
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("testapp")
      .DiscoverEndpoints()
      .Build();

    // Act - invoke with --help
    int exitCode = await app.RunAsync(["--help"]);

    // Assert - should show help, NOT execute default handler
    exitCode.ShouldBe(0);
    terminal.OutputContains("default-handler-executed").ShouldBeFalse("Default handler should NOT execute when --help is requested");
    terminal.OutputContains("testapp").ShouldBeTrue("Help should show app name");
  }

  /// <summary>
  /// Verify endpoint default route still works when no args provided.
  /// </summary>
  public static async Task Should_execute_endpoint_default_route_with_empty_args()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act - invoke with no args
    int exitCode = await app.RunAsync([]);

    // Assert - default handler should run
    exitCode.ShouldBe(0);
    terminal.OutputContains("Input: (none)").ShouldBeTrue("Default handler should execute with empty args");
  }

  /// <summary>
  /// Verify endpoint default route with parameter works correctly.
  /// </summary>
  public static async Task Should_execute_endpoint_default_route_with_positional_arg()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .DiscoverEndpoints()
      .Build();

    // Act - invoke with a positional argument
    int exitCode = await app.RunAsync(["test-value"]);

    // Assert - default handler should run with parameter
    exitCode.ShouldBe(0);
    terminal.OutputContains("Input: test-value").ShouldBeTrue("Default handler should execute with parameter");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help

// ═══════════════════════════════════════════════════════════════════════════════
// Endpoint definition - default route endpoint for Issue #179 testing
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRoute("", Description = "Default action with optional input and verbose option")]
internal sealed class DefaultEndpoint : ICommand<Unit>
{
  [Parameter(Description = "Optional input value")]
  public string? Input { get; set; }

  [Option("verbose", "v", Description = "Enable verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler(ITerminal terminal) : ICommandHandler<DefaultEndpoint, Unit>
  {
    public async ValueTask<Unit> Handle(DefaultEndpoint command, CancellationToken ct)
    {
      string input = command.Input ?? "(none)";
      await terminal.WriteLineAsync($"default-handler-executed: Input: {input}, Verbose: {command.Verbose}").ConfigureAwait(false);
      return default;
    }
  }
}