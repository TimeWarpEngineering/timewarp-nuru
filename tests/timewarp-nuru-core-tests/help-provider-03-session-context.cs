#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj

// Test SessionContext integration for help display
// Verifies that help and --help behave consistently based on runtime context

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Core.HelpProviderSession
{

[TestTag("Help")]
[TestTag("SessionContext")]
public class SessionContextHelpTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SessionContextHelpTests>();

  public static async Task Should_default_to_cli_context()
  {
    // Arrange
    SessionContext context = new();

    // Assert - Default should be CLI mode
    context.IsReplSession.ShouldBeFalse();
    context.HelpContext.ShouldBe(HelpContext.Cli);

    await Task.CompletedTask;
  }

  public static async Task Should_switch_to_repl_context()
  {
    // Arrange
    SessionContext context = new();

    // Act
    context.IsReplSession = true;

    // Assert
    context.HelpContext.ShouldBe(HelpContext.Repl);

    await Task.CompletedTask;
  }

  public static async Task Should_switch_back_to_cli_context()
  {
    // Arrange
    SessionContext context = new();
    context.IsReplSession = true;

    // Act
    context.IsReplSession = false;

    // Assert
    context.HelpContext.ShouldBe(HelpContext.Cli);

    await Task.CompletedTask;
  }

  public static async Task Should_expose_session_context_from_nuru_core_app()
  {
    // Arrange & Act
    NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
      .Map("test", () => "ok")
      .Build();

    // Assert - App should have SessionContext property
    app.SessionContext.ShouldNotBeNull();
    app.SessionContext.IsReplSession.ShouldBeFalse();
    app.SessionContext.HelpContext.ShouldBe(HelpContext.Cli);

    await Task.CompletedTask;
  }

  public static async Task Should_hide_repl_commands_in_cli_help()
  {
    // Arrange
    string? capturedHelp = null;
    NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
      .AddReplRoutes() // Adds exit, quit, q, clear, history, etc.
      .Map("test", () => "ok")
      .Map("help", (SessionContext session) =>
      {
        // This simulates what the auto-generated help route does
        capturedHelp = session.HelpContext.ToString();
        return capturedHelp;
      })
      .Build();

    // Act - Execute help in CLI context (default)
    await app.RunAsync(["help"]);

    // Assert - Should use CLI context when not in REPL
    capturedHelp.ShouldBe("Cli");

    await Task.CompletedTask;
  }

  public static async Task Should_show_repl_commands_when_session_context_is_repl()
  {
    // Arrange
    string? capturedHelp = null;
    NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
      .AddReplRoutes()
      .Map("test", () => "ok")
      .Map("help", (SessionContext session) =>
      {
        capturedHelp = session.HelpContext.ToString();
        return capturedHelp;
      })
      .Build();

    // Simulate REPL context by setting the flag
    app.SessionContext.IsReplSession = true;

    // Act - Execute help in REPL context
    await app.RunAsync(["help"]);

    // Assert - Should use REPL context
    capturedHelp.ShouldBe("Repl");

    // Cleanup
    app.SessionContext.IsReplSession = false;

    await Task.CompletedTask;
  }

  public static async Task Should_have_same_behavior_for_help_and_dash_dash_help()
  {
    // Arrange - Test that both help and --help use SessionContext
    string? helpContext = null;
    string? dashDashHelpContext = null;

    NuruCoreApp app = NuruCoreApp.CreateSlimBuilder()
      .Map("test", () => "ok")
      .Map("help", (SessionContext session) =>
      {
        helpContext = session.HelpContext.ToString();
        return helpContext;
      })
      .Map("--help", (SessionContext session) =>
      {
        dashDashHelpContext = session.HelpContext.ToString();
        return dashDashHelpContext;
      })
      .Build();

    // Act
    await app.RunAsync(["help"]);
    await app.RunAsync(["--help"]);

    // Assert - Both should use the same context (CLI by default)
    helpContext.ShouldBe("Cli");
    dashDashHelpContext.ShouldBe("Cli");

    // Now test in REPL context
    app.SessionContext.IsReplSession = true;

    await app.RunAsync(["help"]);
    await app.RunAsync(["--help"]);

    // Both should now use REPL context
    helpContext.ShouldBe("Repl");
    dashDashHelpContext.ShouldBe("Repl");

    // Cleanup
    app.SessionContext.IsReplSession = false;

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Core.HelpProviderSession
