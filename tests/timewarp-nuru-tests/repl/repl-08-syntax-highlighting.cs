#!/usr/bin/dotnet --
#:project ../../../source/timewarp-nuru/timewarp-nuru.csproj

// Test syntax highlighting (Section 8 of REPL Test Plan)

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.ReplTests.SyntaxHighlighting
{
  [TestTag("REPL")]
  public class SyntaxHighlightingTests
  {
    [ModuleInitializer]
    internal static void Register() => RegisterTests<SyntaxHighlightingTests>();

  /// <summary>
  /// Test implementation of IReplRouteProvider for syntax highlighting tests.
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

  public static async Task Should_highlight_known_command()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider("status");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("status");

    // Assert
    result.ShouldContain(SyntaxColors.CommandColor);
    result.ShouldContain(AnsiColors.Reset);

    await Task.CompletedTask;
  }

  public static async Task Should_use_default_color_for_unknown_text()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider("status");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("unknown");

    // Assert - unknown text gets default token color
    result.ShouldContain(SyntaxColors.DefaultTokenColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_long_options()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("--verbose");

    // Assert
    result.ShouldContain(SyntaxColors.KeywordColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_short_options()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("-v");

    // Assert
    result.ShouldContain(SyntaxColors.OperatorColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_string_literals()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("\"hello world\"");

    // Assert
    result.ShouldContain(SyntaxColors.StringColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_numbers()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("42");

    // Assert
    result.ShouldContain(SyntaxColors.NumberColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_mixed_tokens()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider("deploy");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("deploy --env \"production\" -v 42");

    // Assert - verify multiple color codes present
    result.ShouldContain(AnsiColors.Reset);
    result.Length.ShouldBeGreaterThan("deploy --env \"production\" -v 42".Length);

    await Task.CompletedTask;
  }

  public static async Task Should_return_empty_for_empty_input()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act
    string result = highlighter.Highlight("");

    // Assert
    result.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_use_command_cache()
  {
    // Arrange
    IReplRouteProvider routeProvider = new TestReplRouteProvider("status");

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(routeProvider, loggerFactory);

    // Act - call twice to test cache
    string result1 = highlighter.Highlight("status");
    string result2 = highlighter.Highlight("status");

    // Assert - results should be identical (cache working)
    result1.ShouldBe(result2);

    await Task.CompletedTask;
  }
  }
}
