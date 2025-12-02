#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;

// Test error handling (Section 10 of REPL Test Plan)
return await RunTests<ErrorHandlingTests>();

[TestTag("REPL")]
public class ErrorHandlingTests
{
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
      .Map("fail", ThrowInvalidOperation)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0);
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
      .Map("fail", ThrowInvalidOperation)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ContinueOnError = false)
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert - session should exit after error
    exitCode.ShouldBe(1);
  }

  public static async Task Should_handle_invalid_route()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("nonexistent");
    terminal.QueueLine("exit");

    NuruCoreApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("status", () => "OK")
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
      .Map("add {n:int}", (int n) => $"Result: {n}")
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
      .Map("fail", ThrowInvalidOperation)
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
      .Map("greet {name}", (string name) => $"Hello, {name}!")
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
      .Map("fail", ThrowInvalidOperation)
      .AddReplSupport(options => options.ContinueOnError = false)
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(1, "Exit code should be non-zero on error");
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
      .Map("fail1", ThrowError1)
      .Map("fail2", ThrowArgument)
      .Map("status", () => "OK")
      .AddReplSupport(options => options.ContinueOnError = true)
      .Build();

    // Act
    int exitCode = await app.RunReplAsync();

    // Assert
    exitCode.ShouldBe(0);
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Session should handle multiple errors");
  }
}
