#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test that interactive route execution works with NuruCoreAppHolder invoker
// Regression test for: "No source-generated invoker found for signature 'NuruCoreAppHolder_Returns_Task'"

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.InteractiveRouteExecution
{

[TestTag("REPL")]
public class InteractiveRouteExecutionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<InteractiveRouteExecutionTests>();

  [Skip("Interactive route execution not yet implemented")]
  public static async Task Should_execute_interactive_route_with_long_form()
  {
    // Arrange - Create app with interactive route (uses NuruCoreAppHolder parameter)
    using TestTerminal terminal = new();
    terminal.QueueLine("exit"); // Exit REPL immediately

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
      .AddReplSupport()
      .AddInteractiveRoute() // This registers StartInteractiveModeAsync(NuruCoreAppHolder) => Task
      .Build();

    // Act - Execute the --interactive route (requires NuruCoreAppHolder_Returns_Task invoker)
    int exitCode = await app.RunAsync(["--interactive"]);

    // Assert - Should enter REPL and exit cleanly
    exitCode.ShouldBe(0, "Interactive route should execute successfully");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("REPL should have started and exited");
  }

  [Skip("Interactive route execution not yet implemented")]
  public static async Task Should_execute_interactive_route_with_short_form()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
      .AddReplSupport()
      .AddInteractiveRoute()
      .Build();

    // Act - Execute the -i route (short form alias)
    int exitCode = await app.RunAsync(["-i"]);

    // Assert
    exitCode.ShouldBe(0, "Short form -i should also work");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("REPL should have started via -i");
  }

  [Skip("Interactive route execution not yet implemented")]
  public static async Task Should_execute_custom_interactive_route_patterns()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "OK")
        .AsQuery()
        .Done()
      .AddReplSupport()
      .AddInteractiveRoute("--repl,-r") // Custom patterns
      .Build();

    // Act - Execute with custom pattern
    int exitCode = await app.RunAsync(["--repl"]);

    // Assert
    exitCode.ShouldBe(0, "Custom interactive route should work");
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("REPL should have started via --repl");
  }

  [Skip("Interactive route execution not yet implemented")]
  public static async Task Should_execute_commands_in_repl_after_interactive_start()
  {
    // Arrange - Verify full REPL functionality works after entering via --interactive
    using TestTerminal terminal = new();
    terminal.QueueLine("status"); // Execute a command in REPL
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status")
        .WithHandler(() => "All systems operational")
        .AsQuery()
        .Done()
      .AddReplSupport()
      .AddInteractiveRoute()
      .Build();

    // Act
    int exitCode = await app.RunAsync(["--interactive"]);

    // Assert - Command should have executed in REPL
    exitCode.ShouldBe(0);
    terminal.OutputContains("All systems operational")
      .ShouldBeTrue("Commands should execute after entering REPL via --interactive");
  }
}

} // namespace TimeWarp.Nuru.Tests.ReplTests.InteractiveRouteExecution
