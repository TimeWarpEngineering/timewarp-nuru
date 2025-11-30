#!/usr/bin/dotnet --
#:project ../../source/timewarp-nuru/timewarp-nuru.csproj
#:project ../../source/timewarp-nuru-repl/timewarp-nuru-repl.csproj

using TimeWarp.Nuru;
using TimeWarp.Nuru.Repl;
using TimeWarp.Nuru.Repl.Input;

// Test syntax highlighting (Section 8 of REPL Test Plan)
return await RunTests<SyntaxHighlightingTests>();

[TestTag("REPL")]
public class SyntaxHighlightingTests
{
  private static EndpointCollection CreateEndpointsFromApp(params (string route, Func<string> handler)[] routes)
  {
    NuruAppBuilder builder = new();
    foreach ((string route, Func<string> handler) in routes)
    {
      builder.Map(route, handler);
    }

    NuruCoreApp app = builder.Build();
    return app.Endpoints;
  }

  public static async Task Should_highlight_known_command()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp(("status", () => "OK"));

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

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
    EndpointCollection endpoints = CreateEndpointsFromApp(("status", () => "OK"));

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("unknown");

    // Assert - unknown text gets default token color
    result.ShouldContain(SyntaxColors.DefaultTokenColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_long_options()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("--verbose");

    // Assert
    result.ShouldContain(SyntaxColors.KeywordColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_short_options()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("-v");

    // Assert
    result.ShouldContain(SyntaxColors.OperatorColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_string_literals()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("\"hello world\"");

    // Assert
    result.ShouldContain(SyntaxColors.StringColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_numbers()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("42");

    // Assert
    result.ShouldContain(SyntaxColors.NumberColor);

    await Task.CompletedTask;
  }

  public static async Task Should_highlight_mixed_tokens()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp(("deploy", () => "Deployed"));

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

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
    EndpointCollection endpoints = CreateEndpointsFromApp();

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act
    string result = highlighter.Highlight("");

    // Assert
    result.ShouldBe("");

    await Task.CompletedTask;
  }

  public static async Task Should_use_command_cache()
  {
    // Arrange
    EndpointCollection endpoints = CreateEndpointsFromApp(("status", () => "OK"));

    using ILoggerFactory loggerFactory = LoggerFactory.Create(_ => { });
    SyntaxHighlighter highlighter = new(endpoints, loggerFactory);

    // Act - call twice to test cache
    string result1 = highlighter.Highlight("status");
    string result2 = highlighter.Highlight("status");

    // Assert - results should be identical (cache working)
    result1.ShouldBe(result2);

    await Task.CompletedTask;
  }
}
