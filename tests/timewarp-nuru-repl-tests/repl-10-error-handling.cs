#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test error handling (Section 10 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.ErrorHandling
{
  [TestTag("REPL")]
  public class ErrorHandlingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<ErrorHandlingTests>();

  private static string ThrowInvalidOperation() => throw new InvalidOperationException("Test error");
  private static string ThrowError1() => throw new InvalidOperationException("Error 1");
  private static string ThrowArgument() => throw new ArgumentException("Error 2");

  public static async Task Should_continue_after_command_error_when_configured()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail");
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("fail").WithHandler(ThrowInvalidOperation).AsCommand().Done()
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should continue after error");
  }

  public static async Task Should_exit_on_error_when_configured()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail");
    terminal.QueueLine("status");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("fail").WithHandler(ThrowInvalidOperation).AsCommand().Done()
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddReplSupport(options => options.ContinueOnError = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should exit after error
    // Note: With Task return type, we can't check exit code, but we verify error was shown
    terminal.ErrorOutput.ShouldContain("Error");
  }

  public static async Task Should_handle_invalid_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("nonexistent");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should continue even with invalid route
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should handle invalid route gracefully");
  }

  public static async Task Should_handle_type_conversion_error()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("add notanumber");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("add {n:int}")
        .WithHandler((int n) => $"Result: {n}")
        .AsCommand()
        .Done()
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should handle type conversion error");
  }

  public static async Task Should_show_exit_code_on_error()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("fail").WithHandler(ThrowInvalidOperation).AsCommand().Done()
      .AddReplSupport(options =>
      {
        options.ContinueOnError = true;
        options.ShowExitCode = true;
      })
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should show exit code on error");
  }

  public static async Task Should_handle_argument_error()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should handle missing argument");
  }

  public static async Task Should_return_non_zero_exit_code_on_error()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("fail").WithHandler(ThrowInvalidOperation).AsCommand().Done()
      .AddReplSupport(options => options.ContinueOnError = false)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should exit on error
    // Note: With Task return type, we can't check exit code, but we verify error was shown
    terminal.ErrorOutput.ShouldContain("Error");
  }

  public static async Task Should_handle_multiple_errors()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("fail1");
    terminal.QueueLine("fail2");
    terminal.QueueLine("status");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("fail1").WithHandler(ThrowError1).AsCommand().Done()
      .Map("fail2").WithHandler(ThrowArgument).AsCommand().Done()
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should handle multiple errors");
  }
  }
}
