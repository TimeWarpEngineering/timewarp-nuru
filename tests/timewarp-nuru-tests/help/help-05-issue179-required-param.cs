#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj
#pragma warning disable RCS1163 // Unused parameter

#region Purpose
// Reproduction of Issue #179: [NuruRoute("")] endpoint with REQUIRED parameter
// and option intercepts --help instead of showing help.
// The handler executes with --help consumed as the parameter value.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Help
{

[TestTag("Help")]
public class Issue179RequiredParamTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<Issue179RequiredParamTests>();

  /// <summary>
  /// Issue #179: `getleads --help` executes the handler instead of showing help.
  /// The endpoint has [NuruRoute("")] with a required [Parameter] and [Option].
  /// --help gets consumed as the required parameter value.
  /// </summary>
  public static async Task Should_show_help_not_execute_handler()
  {
    // Arrange
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .WithName("getleads")
      .Map<Issue179DefaultEndpoint>()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--help"]);

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("handler-executed").ShouldBeFalse("Handler should NOT execute when --help is requested");
    terminal.OutputContains("getleads").ShouldBeTrue("Help output should contain app name");
  }
}

} // namespace TimeWarp.Nuru.Tests.Help

// ═══════════════════════════════════════════════════════════════════════════════
// Endpoint: exact scenario from Issue #179
// [NuruRoute("")] with required string parameter and option
// ═══════════════════════════════════════════════════════════════════════════════

[NuruRoute("", Description = "Default action")]
internal sealed class Issue179DefaultEndpoint : ICommand<Unit>
{
  [Parameter(Description = "Some input")]
  public string Input { get; set; } = string.Empty;

  [Option("verbose", "v", Description = "Verbose output")]
  public bool Verbose { get; set; }

  internal sealed class Handler(ITerminal terminal) : ICommandHandler<Issue179DefaultEndpoint, Unit>
  {
    public async ValueTask<Unit> Handle(Issue179DefaultEndpoint command, CancellationToken ct)
    {
      await terminal.WriteLineAsync($"handler-executed: Input={command.Input}").ConfigureAwait(false);
      return default;
    }
  }
}
