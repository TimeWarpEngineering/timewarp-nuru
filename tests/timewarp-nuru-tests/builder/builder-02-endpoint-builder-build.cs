#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru/timewarp-nuru.csproj

#region Purpose
// Kanban 482-001 (review finding R-1): EndpointBuilder.Build() on an open route must register
// that route exactly as Done() would, then build the app. Before the fix the generator dropped
// the route and never marked the app built, so RunAsync hit the throwing stub.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Builder.EndpointBuilderBuild
{

[TestTag("Builder")]
public class EndpointBuilderBuildTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<EndpointBuilderBuildTests>();

  public static async Task Should_register_open_route_when_build_called_on_endpoint()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ping").WithHandler(() => 0)
      .Build();

    int exitCode = await app.RunAsync(["ping"]);

    exitCode.ShouldBe(0);
  }

  public static async Task Should_run_open_route_handler_when_build_called_on_endpoint()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ping").WithHandler(() => "pong-open").AsQuery()
      .Build();

    int exitCode = await app.RunAsync(["ping"]);

    exitCode.ShouldBe(0);
    terminal.OutputContains("pong-open").ShouldBeTrue();
  }

  public static async Task Should_register_route_once_when_done_then_build()
  {
    using TestTerminal terminal = new();
    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .Map("ping").WithHandler(() => "pong-done").WithDescription("ping-once-marker").AsQuery()
      .Done()
      .Build();

    int exitCode = await app.RunAsync(["--help"]);

    exitCode.ShouldBe(0);
    terminal.Output.Split("ping-once-marker").Length.ShouldBe(2);
  }
}

}
