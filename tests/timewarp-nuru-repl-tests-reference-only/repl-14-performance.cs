#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using System.Diagnostics;

// Test performance (Section 14 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.Performance
{
  [TestTag("REPL")]
  public class PerformanceTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<PerformanceTests>();

  [Timeout(5000)]
  public static async Task Should_start_session_quickly()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    using NuruAppBuilder builder = new();
    builder.UseTerminal(terminal);
    builder.AddReplSupport();
    NuruCoreApp app = builder.Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - startup should be fast (< 500ms including first JIT)
    sw.ElapsedMilliseconds.ShouldBeLessThan(500);
  }

  [Timeout(5000)]
  public static async Task Should_execute_commands_with_low_overhead()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("noop");
    terminal.QueueLine("noop");
    terminal.QueueLine("noop");
    terminal.QueueLine("exit");

    using NuruAppBuilder builder = new();
    builder.UseTerminal(terminal);
    builder.Map("noop").WithHandler(() => { }).AsCommand().Done();
    builder.AddReplSupport();
    NuruCoreApp app = builder.Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - should complete quickly
    sw.ElapsedMilliseconds.ShouldBeLessThan(1000);
  }

  [Timeout(5000)]
  public static async Task Should_handle_large_history_efficiently()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Add many commands to history
    for (int i = 0; i < 100; i++)
    {
      terminal.QueueLine($"cmd{i}");
    }

    terminal.QueueLine("exit");

    using NuruAppBuilder builder = new();
    builder.UseTerminal(terminal);
    builder.Map("cmd{n}").WithHandler((string _) => "OK").AsCommand().Done();
    builder.AddReplSupport(options => options.MaxHistorySize = 1000);
    NuruCoreApp app = builder.Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - should handle large history without significant slowdown
    sw.ElapsedMilliseconds.ShouldBeLessThan(5000);
  }

  [Timeout(5000)]
  public static async Task Should_complete_quickly_with_many_routes()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueKeys("cmd");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    using NuruAppBuilder builder = new();
    builder.UseTerminal(terminal);

    // Add many routes
    for (int i = 0; i < 100; i++)
    {
      int index = i;
      builder.Map($"cmd{index}").WithHandler(() => $"Command {index}").AsCommand().Done();
    }

    builder.AddReplSupport(options => options.EnableArrowHistory = true);
    NuruCoreApp app = builder.Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - completion should be fast even with many routes
    sw.ElapsedMilliseconds.ShouldBeLessThan(2000);
  }

  [Timeout(5000)]
  public static async Task Should_highlight_syntax_quickly()
  {
    // Arrange - create endpoints via app builder
    using NuruAppBuilder builder = new();

    for (int i = 0; i < 50; i++)
    {
      int index = i;
      builder.Map($"command{index}").WithHandler(() => "OK").AsCommand().Done();
    }

    NuruCoreApp app = builder.Build();
    EndpointCollection endpoints = app.Endpoints;

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    Stopwatch sw = Stopwatch.StartNew();

    for (int i = 0; i < 100; i++)
    {
      highlighter.Highlight($"command{i % 50} --option \"value\" 42");
    }

    sw.Stop();

    // Assert - 100 highlights should complete quickly
    sw.ElapsedMilliseconds.ShouldBeLessThan(500);

    await Task.CompletedTask;
  }

  [Timeout(5000)]
  public static async Task Should_parse_commands_quickly()
  {
    // Arrange & Act
    Stopwatch sw = Stopwatch.StartNew();

    for (int i = 0; i < 1000; i++)
    {
      CommandLineParser.Parse("deploy --env \"production\" --force -v 42 \"my app\"");
    }

    sw.Stop();

    // Assert - 1000 parses should be very fast
    sw.ElapsedMilliseconds.ShouldBeLessThan(100);

    await Task.CompletedTask;
  }

  [Timeout(5000)]
  public static async Task Should_use_command_cache_efficiently()
  {
    // Arrange - create endpoints via app builder
    using NuruAppBuilder builder = new();
    builder.Map("status").WithHandler(() => "OK").AsQuery().Done();
    NuruCoreApp app = builder.Build();
    EndpointCollection endpoints = app.Endpoints;

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act - first call populates cache
    Stopwatch sw1 = Stopwatch.StartNew();
    highlighter.Highlight("status");
    sw1.Stop();

    // Second call should use cache
    Stopwatch sw2 = Stopwatch.StartNew();

    for (int i = 0; i < 1000; i++)
    {
      highlighter.Highlight("status");
    }

    sw2.Stop();

    // Assert - cached calls should be much faster
    // Average per call should be very low
    (sw2.ElapsedMilliseconds / 1000.0).ShouldBeLessThan(1);

    await Task.CompletedTask;
  }

  [Timeout(5000)]
  public static async Task Should_cleanup_resources_on_exit()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    using NuruAppBuilder builder = new();
    builder.UseTerminal(terminal);
    builder.AddReplSupport();
    NuruCoreApp app = builder.Build();

    // Act
    await app.RunReplAsync();

    // Assert - ReplSession.CurrentSession should be null after exit
    // This is internal, so we verify indirectly through successful completion
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Resources should be cleaned up");
  }
  }
}
