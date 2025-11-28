#!/usr/bin/dotnet --

return await RunTests<CustomSourcesTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class CustomSourcesTests
{
  public static async Task Should_use_custom_source_for_parameter()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}", (string env) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      EnvironmentSource source = new();
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert
    output.ShouldContain("production");
    output.ShouldContain("staging");
    output.ShouldContain("development");
  }

  public static async Task Should_use_custom_source_for_file_paths()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("open {file}", (string file) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      FilePathSource source = new(["/tmp/file1.txt", "/tmp/file2.txt", "/home/user/doc.md"]);
      registry.RegisterForParameter("file", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "open"]));

    // Assert
    output.ShouldContain("/tmp/file1.txt");
    output.ShouldContain("/tmp/file2.txt");
    output.ShouldContain("/home/user/doc.md");
  }

  public static async Task Should_support_multiple_custom_sources_for_different_parameters()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {version}", (string env, string version) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      registry.RegisterForParameter("env", new EnvironmentSource());
      registry.RegisterForParameter("version", new VersionSource());
    });

    NuruCoreApp app = builder.Build();

    // Act - Complete env
    (int _, string envOutput, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Should show environments
    envOutput.ShouldContain("production");
    envOutput.ShouldNotContain("v1.0.0");
  }

  public static async Task Should_provide_context_to_custom_source()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("show {item}", (string item) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      ContextAwareSource source = new();
      registry.RegisterForParameter("item", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "show"]));

    // Assert - Source should have received context
    output.ShouldContain("context-received");
  }

  public static async Task Should_handle_source_that_returns_empty_list()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("select {item}", (string item) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      EmptySource source = new();
      registry.RegisterForParameter("item", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "select"]));

    // Assert - Should not crash, should still output directive
    exitCode.ShouldBe(0);
    output.ShouldContain(":4");
  }

  public static async Task Should_handle_source_with_many_completions()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("pick {number}", (string number) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      LargeSource source = new(100);
      registry.RegisterForParameter("number", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "pick"]));

    // Assert
    exitCode.ShouldBe(0);
    string[] lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    // Should have 100 items plus directive line
    lines.Length.ShouldBeGreaterThanOrEqualTo(101);
  }

  public static async Task Should_support_descriptions_in_custom_source()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("select {region}", (string region) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      RegionSource source = new();
      registry.RegisterForParameter("region", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "select"]));

    // Assert - Should include descriptions
    output.ShouldContain("us-east-1\tUS East (N. Virginia)");
    output.ShouldContain("eu-west-1\tEU West (Ireland)");
  }

  public static async Task Should_support_stateful_source()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("use {item}", (string item) => 0);

    StatefulSource statefulSource = new();
    statefulSource.AddItem("first");
    statefulSource.AddItem("second");

    builder.EnableDynamicCompletion(configure: registry =>
    {
      registry.RegisterForParameter("item", statefulSource);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "use"]));

    // Assert
    output.ShouldContain("first");
    output.ShouldContain("second");
  }

  public static async Task Should_allow_source_to_access_endpoints()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("info {item}", (string item) => 0);

    builder.EnableDynamicCompletion(configure: registry =>
    {
      EndpointAwareSource source = new();
      registry.RegisterForParameter("item", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "info"]));

    // Assert - Source can inspect endpoints
    output.ShouldContain("endpoint-count:");
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
// Custom Completion Sources
// =============================================================================

sealed class EnvironmentSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate("production", "Live production environment", CompletionType.Parameter);
    yield return new CompletionCandidate("staging", "Pre-production testing", CompletionType.Parameter);
    yield return new CompletionCandidate("development", "Local development", CompletionType.Parameter);
  }
}

sealed class VersionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate("v1.0.0", null, CompletionType.Parameter);
    yield return new CompletionCandidate("v1.1.0", null, CompletionType.Parameter);
    yield return new CompletionCandidate("v2.0.0", null, CompletionType.Parameter);
  }
}

sealed class FilePathSource(string[] paths) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    foreach (string path in paths)
    {
      yield return new CompletionCandidate(path, null, CompletionType.Parameter);
    }
  }
}

sealed class ContextAwareSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Demonstrate that context is received
    if (context is not null)
    {
      yield return new CompletionCandidate("context-received", $"Args: {context.Args.Length}", CompletionType.Parameter);
    }
  }
}

sealed class EmptySource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield break;
  }
}

sealed class LargeSource(int count) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    for (int i = 1; i <= count; i++)
    {
      yield return new CompletionCandidate($"item{i}", null, CompletionType.Parameter);
    }
  }
}

sealed class RegionSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate("us-east-1", "US East (N. Virginia)", CompletionType.Parameter);
    yield return new CompletionCandidate("us-west-2", "US West (Oregon)", CompletionType.Parameter);
    yield return new CompletionCandidate("eu-west-1", "EU West (Ireland)", CompletionType.Parameter);
  }
}

sealed class StatefulSource : ICompletionSource
{
  private readonly List<string> items = [];

  public void AddItem(string item) => items.Add(item);

  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    foreach (string item in items)
    {
      yield return new CompletionCandidate(item, null, CompletionType.Parameter);
    }
  }
}

sealed class EndpointAwareSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    int count = context.Endpoints.Count;
    yield return new CompletionCandidate($"endpoint-count:{count}", null, CompletionType.Parameter);
  }
}
