#!/usr/bin/dotnet --

return await RunTests<CallbackProtocolTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CallbackProtocolTests
{
  public static async Task Should_accept_cursor_index_as_first_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Pass cursor index as first argument
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Should not error on parsing
    exitCode.ShouldBe(0);
  }

  public static async Task Should_accept_words_as_remaining_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("git status", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Pass multiple words after cursor index
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "git"]));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain("status");
  }

  public static async Task Should_handle_cursor_at_app_name_position()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Cursor at position 0 (the app name itself)
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "0", "app"]));

    // Assert - Should not crash
    exitCode.ShouldBe(0);
  }

  public static async Task Should_handle_cursor_beyond_typed_words()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}", (string env) => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Cursor at position 3 but only 2 words provided
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "3", "app", "deploy"]));

    // Assert
    exitCode.ShouldBe(0);
  }

  public static async Task Should_include_directive_in_output()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Output must end with directive line
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    bool hasDirective = lines.Any(line => line.StartsWith(':'));
    hasDirective.ShouldBeTrue();
  }

  public static async Task Should_output_each_completion_on_separate_line()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);
    builder.Map("help", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Should have separate lines for each completion
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    // Should have at least 3 completions plus the directive
    lines.Length.ShouldBeGreaterThanOrEqualTo(4);
  }

  public static async Task Should_use_tab_separator_for_description()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}", (string env) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSourceWithDescriptions source = new([
        ("production", "Production environment")
      ]);
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Should use tab to separate value from description
    output.ShouldContain("production\tProduction environment");
  }

  public static async Task Should_output_diagnostics_to_stderr()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string _, string stderr) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Stderr should have diagnostic info
    stderr.ShouldContain("directive");
  }

  public static async Task Should_handle_empty_completion_results()
  {
    // Arrange
    NuruAppBuilder builder = new();
    // No routes registered
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Should still output directive even with no completions
    exitCode.ShouldBe(0);
    output.ShouldContain(":"); // Directive line
  }

  public static async Task Should_parse_integer_cursor_index()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test", () => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Large cursor index
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "100", "app"]));

    // Assert - Should handle large index without error
    exitCode.ShouldBe(0);
  }

  public static async Task Should_handle_special_characters_in_words()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("connect {url}", (string url) => 0);
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Words with special characters
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "connect", "https://example.com:8080/path"]));

    // Assert - Should handle without crashing
    exitCode.ShouldBe(0);
  }

  private static async Task<(int exitCode, string stdout, string stderr)> CaptureAppOutputAsync(Func<Task<int>> action)
  {
    TextWriter originalOut = Console.Out;
    TextWriter originalError = Console.Error;

    using StringWriter stdoutWriter = new();
    using StringWriter stderrWriter = new();

    try
    {
      Console.SetOut(stdoutWriter);
      Console.SetError(stderrWriter);

      int exitCode = await action();

      return (exitCode, stdoutWriter.ToString(), stderrWriter.ToString());
    }
    finally
    {
      Console.SetOut(originalOut);
      Console.SetError(originalError);
    }
  }
}

// =============================================================================
// Test Helpers
// =============================================================================

sealed class TestCompletionSourceWithDescriptions((string value, string desc)[] items) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    foreach ((string value, string desc) in items)
    {
      yield return new CompletionCandidate(
        Value: value,
        Description: desc,
        Type: CompletionType.Parameter
      );
    }
  }
}
