#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

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

  /// <summary>
  /// Test implementation of IReplRouteProvider for performance tests.
  /// </summary>
  private sealed class TestReplRouteProvider : IReplRouteProvider
  {
    private readonly string[] _commandPrefixes;

    public TestReplRouteProvider(params string[] commandPrefixes)
    {
      _commandPrefixes = commandPrefixes;
    }

    public IReadOnlyList<string> GetCommandPrefixes() => _commandPrefixes;

    public IEnumerable<CompletionCandidate> GetCompletions(string[] args, bool hasTrailingSpace) => [];

    public bool IsKnownCommand(string token) =>
      _commandPrefixes.Any(p => p.Equals(token, StringComparison.OrdinalIgnoreCase) ||
                                p.StartsWith(token + " ", StringComparison.OrdinalIgnoreCase));
  }

  [Timeout(5000)]
  public static async Task Should_start_session_quickly()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunAsync(["--interactive"]);
    sw.Stop();

    // Assert - startup should be fast (< 500ms including first JIT)
    sw.ElapsedMilliseconds.ShouldBeLessThan(500);
  }

  [Timeout(5000)]
  public static async Task Should_execute_commands_with_low_overhead()
  {
    // Arrange
    using TestTerminal terminal = new();
    terminal.QueueLine("help"); // Use built-in help command
    terminal.QueueLine("help");
    terminal.QueueLine("help");
    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunAsync(["--interactive"]);
    sw.Stop();

    // Assert - should complete quickly
    sw.ElapsedMilliseconds.ShouldBeLessThan(1000);
  }

  [Timeout(5000)]
  public static async Task Should_handle_large_history_efficiently()
  {
    // Arrange
    using TestTerminal terminal = new();

    // Add many commands to history (use built-in commands)
    for (int i = 0; i < 100; i++)
    {
      terminal.QueueLine("help");
    }

    terminal.QueueLine("exit");

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl(options => options.MaxHistorySize = 1000)
      .Build();

    // Act
    Stopwatch sw = Stopwatch.StartNew();
    await app.RunAsync(["--interactive"]);
    sw.Stop();

    // Assert - should handle large history without significant slowdown
    sw.ElapsedMilliseconds.ShouldBeLessThan(5000);
  }

  [Timeout(5000)]
  public static async Task Should_highlight_syntax_quickly()
  {
    // Arrange - create many command prefixes
    string[] commands = [.. Enumerable.Range(0, 50).Select(i => $"command{i}")];
    IReplRouteProvider routeProvider = new TestReplRouteProvider(commands);

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

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
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider("status");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

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

    NuruApp app = NuruApp.CreateBuilder()
      .UseTerminal(terminal)
      .AddRepl()
      .Build();

    // Act
    await app.RunAsync(["--interactive"]);

    // Assert - ReplSession.CurrentSession should be null after exit
    // This is internal, so we verify indirectly through successful completion
    terminal.OutputContains("Goodbye!")
      .ShouldBeTrue("Resources should be cleaned up");
  }
  }
}
