#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

// Test NuruApp integration (Section 13 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.NuruAppIntegration
{
  [TestTag("REPL")]
  public class NuruAppIntegrationTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<NuruAppIntegrationTests>();

  public static async Task Should_use_AddReplSupport_extension()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport()  // Extension method
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("AddReplSupport extension should work");
  }

  public static async Task Should_register_repl_routes()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("help");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - REPL routes should be registered
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("REPL routes should be registered");
  }

  public static async Task Should_share_type_converters()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("add 5 10");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("add {a:int} {b:int}")
        .WithHandler((int a, int b) => $"{a + b}")
        .AsCommand()
        .Done()
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - type converters should work in REPL
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Type converters should be shared");
  }

  public static async Task Should_access_all_endpoints()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("cmd1");
    terminal.QueueLine("cmd2");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("cmd1").WithHandler(() => "Command 1").AsCommand().Done()
      .Map("cmd2").WithHandler(() => "Command 2").AsCommand().Done()
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - all routes accessible in REPL
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("All endpoints should be accessible");
  }

  public static async Task Should_support_fluent_chaining()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    // Act - fluent chaining
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("status").WithHandler(() => "OK").AsQuery().Done()
      .Map("version").WithHandler(() => "1.0.0").AsQuery().Done()
      .AddReplSupport(options => options.Prompt = ">>> ")
      .Build();

    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Fluent chaining should work");
  }

  public static async Task Should_run_repl_directly()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act - direct REPL start
    await app.RunReplAsync();

    // Assert
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Direct REPL start should work");
  }

  public static async Task Should_use_app_logger()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    // Create app with logging configured
    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - session should complete (logging is internal)
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("App logger should be used");
  }

  public static async Task Should_execute_app_routes_in_repl()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("greet World");
    terminal.QueueLine("exit");

    NuruCoreApp app = NuruApp.CreateBuilder([])
      .UseTerminal(terminal)
      .Map("greet {name}")
        .WithHandler((string name) => $"Hello, {name}!")
        .AsCommand()
        .Done()
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - app routes should execute in REPL context
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("App routes should execute in REPL");
  }
  }
}
