#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test SessionContext class behavior
// Verifies CLI vs REPL context state management

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Session
{

[TestTag("SessionContext")]
public class SessionContextTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SessionContextTests>();

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

  public static async Task Should_default_supports_color_to_true()
  {
    // Arrange
    SessionContext context = new();

    // Assert
    context.SupportsColor.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_allow_setting_supports_color()
  {
    // Arrange
    SessionContext context = new();

    // Act
    context.SupportsColor = false;

    // Assert
    context.SupportsColor.ShouldBeFalse();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Session
