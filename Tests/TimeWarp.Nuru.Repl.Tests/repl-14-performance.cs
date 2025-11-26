#!/usr/bin/dotnet --
#:project ../../Source/TimeWarp.Nuru/TimeWarp.Nuru.csproj
#:project ../../Source/TimeWarp.Nuru.Repl/TimeWarp.Nuru.Repl.csproj

using System.Diagnostics;
using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Input;

// Test performance (Section 14 of REPL Test Plan)
return await RunTests<PerformanceTests>();

[TestTag("REPL")]
public class PerformanceTests
{
  [Timeout(5000)]
  public static async Task Should_start_session_quickly()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    var sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - startup should be fast (< 500ms including first JIT)
    sw.ElapsedMilliseconds.ShouldBeLessThan(500);
  }

  [Timeout(5000)]
  public static async Task Should_execute_commands_with_low_overhead()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueLine("noop");
    terminal.QueueLine("noop");
    terminal.QueueLine("noop");
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("noop", () => { })
      .AddReplSupport()
      .Build();

    // Act
    var sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - should complete quickly
    sw.ElapsedMilliseconds.ShouldBeLessThan(1000);
  }

  [Timeout(5000)]
  public static async Task Should_handle_large_history_efficiently()
  {
    // Arrange
    using var terminal = new TestTerminal();

    // Add many commands to history
    for (int i = 0; i < 100; i++)
    {
      terminal.QueueLine($"cmd{i}");
    }

    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .Map("cmd{n}", (string _) => "OK")
      .AddReplSupport(options => options.MaxHistorySize = 1000)
      .Build();

    // Act
    var sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - should handle large history without significant slowdown
    sw.ElapsedMilliseconds.ShouldBeLessThan(5000);
  }

  [Timeout(5000)]
  public static async Task Should_complete_quickly_with_many_routes()
  {
    // Arrange
    using var terminal = new TestTerminal();
    terminal.QueueKeys("cmd");
    terminal.QueueKey(ConsoleKey.Tab);
    terminal.QueueKey(ConsoleKey.Escape);
    terminal.QueueLine("exit");

    NuruAppBuilder builder = new NuruAppBuilder().UseTerminal(terminal);

    // Add many routes
    for (int i = 0; i < 100; i++)
    {
      int index = i;
      builder.Map($"cmd{index}", () => $"Command {index}");
    }

    builder.AddReplSupport(options => options.EnableArrowHistory = true);
    NuruApp app = builder.Build();

    // Act
    var sw = Stopwatch.StartNew();
    await app.RunReplAsync();
    sw.Stop();

    // Assert - completion should be fast even with many routes
    sw.ElapsedMilliseconds.ShouldBeLessThan(2000);
  }

  [Timeout(5000)]
  public static async Task Should_highlight_syntax_quickly()
  {
    // Arrange - create endpoints via app builder
    var builder = new NuruAppBuilder();

    for (int i = 0; i < 50; i++)
    {
      int index = i;
      builder.Map($"command{index}", () => "OK");
    }

    NuruApp app = builder.Build();
    EndpointCollection endpoints = app.Endpoints;

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    var highlighter = new SyntaxHighlighter(endpoints, loggerFactory);

    // Act
    var sw = Stopwatch.StartNew();

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
    var sw = Stopwatch.StartNew();

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
    var builder = new NuruAppBuilder();
    builder.Map("status", () => "OK");
    NuruApp app = builder.Build();
    EndpointCollection endpoints = app.Endpoints;

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    var highlighter = new SyntaxHighlighter(endpoints, loggerFactory);

    // Act - first call populates cache
    var sw1 = Stopwatch.StartNew();
    highlighter.Highlight("status");
    sw1.Stop();

    // Second call should use cache
    var sw2 = Stopwatch.StartNew();

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
    using var terminal = new TestTerminal();
    terminal.QueueLine("exit");

    NuruApp app = new NuruAppBuilder()
      .UseTerminal(terminal)
      .AddReplSupport()
      .Build();

    // Act
    await app.RunReplAsync();

    // Assert - ReplSession.CurrentSession should be null after exit
    // This is internal, so we verify indirectly through successful completion
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Resources should be cleaned up");
  }
}
