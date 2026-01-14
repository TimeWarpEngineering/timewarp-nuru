#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.OutputFormat
{

[TestTag("Completion")]
public class OutputFormatTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<OutputFormatTests>();

  public static async Task Should_output_value_only_when_no_description()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Line should contain just "status" without a tab
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    string statusLine = lines.First(l => l.StartsWith("status", StringComparison.Ordinal));
    statusLine.ShouldNotContain("\t");
  }

  public static async Task Should_output_value_tab_description_format()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSourceWithDescriptions source = new([
        ("production", "Live production environment")
      ]);
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert
    output.ShouldContain("production\tLive production environment");
  }

  public static async Task Should_end_with_directive_line()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Output should contain directive line (starts with :)
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    lines.Any(l => l.Length > 0 && l[0] == ':').ShouldBeTrue();
  }

  public static async Task Should_output_directive_code_4_for_no_file_comp()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - NoFileComp directive is 4
    output.ShouldContain(":4");
  }

  public static async Task Should_preserve_completion_order()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("apple")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.Map("banana")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.Map("cherry")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Should be alphabetically sorted
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    List<string> completions = [.. lines.Where(l => l.Length > 0 && l[0] != ':')];

    int appleIndex = completions.FindIndex(c => c == "apple");
    int bananaIndex = completions.FindIndex(c => c == "banana");
    int cherryIndex = completions.FindIndex(c => c == "cherry");

    appleIndex.ShouldBeLessThan(bananaIndex);
    bananaIndex.ShouldBeLessThan(cherryIndex);
  }

  public static async Task Should_handle_empty_description()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSourceWithDescriptions source = new([
        ("production", "") // Empty description
      ]);
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Should output without tab when description is empty
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    string productionLine = lines.First(l => l.StartsWith("production", StringComparison.Ordinal));
    productionLine.ShouldBe("production");
  }

  public static async Task Should_handle_multiword_descriptions()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSourceWithDescriptions source = new([
        ("production", "The live production environment with all active users")
      ]);
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert
    output.ShouldContain("production\tThe live production environment with all active users");
  }

  public static async Task Should_output_newline_after_each_completion()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.Map("version")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Each completion should end with newline
    output.ShouldContain("status\n");
    output.ShouldContain("version\n");
  }

  public static async Task Should_output_stderr_diagnostic_for_debugging()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("status")
      .WithHandler(() => { })
      .AsQuery()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string _, string stderr) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "1", "app"]));

    // Assert - Stderr should have useful debug info
    stderr.ShouldContain("Completion ended with directive: NoFileComp");
  }

  public static async Task Should_handle_special_characters_in_values()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("connect {url}")
      .WithHandler((string url) => 0)
      .AsCommand()
      .Done();

    builder.EnableDynamicCompletion(configure: registry =>
    {
      TestCompletionSourceWithDescriptions source = new([
        ("https://api.example.com", "API endpoint"),
        ("user@host:path", "SSH-style path")
      ]);
      registry.RegisterForParameter("url", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "connect"]));

    // Assert - Special chars should be preserved
    output.ShouldContain("https://api.example.com\tAPI endpoint");
    output.ShouldContain("user@host:path\tSSH-style path");
  }

  public static async Task Should_handle_options_with_dashes()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --verbose --quiet")
      .WithHandler((bool verbose, bool quiet) => 0)
      .AsCommand()
      .Done();
    builder.EnableDynamicCompletion();

    NuruCoreApp app = builder.Build();

    // Act - Complete options
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "build", "-"]));

    // Assert - Options should include their dashes
    output.ShouldContain("--verbose");
    output.ShouldContain("--quiet");
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

} // namespace TimeWarp.Nuru.Tests.Completion.OutputFormat
