#!/usr/bin/dotnet --

return await RunTests<DynamicHandlerTests>(clearCache: true);

[TestTag("Completion")]
[ClearRunfileCache]
public class DynamicHandlerTests
{
  public static async Task Should_output_completions_to_stdout()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);
    builder.Map("version", () => 0);

    CompletionSourceRegistry registry = new();
    string[] words = ["app"];
    int cursorIndex = 1;

    // Act
    (int exitCode, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain("status");
    output.ShouldContain("version");

    await Task.CompletedTask;
  }

  public static async Task Should_output_completion_directive()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    CompletionSourceRegistry registry = new();
    string[] words = ["app"];

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(1, words, registry, builder.EndpointCollection));

    // Assert - Should end with :4 (NoFileComp directive = 4)
    output.ShouldContain(":4");

    await Task.CompletedTask;
  }

  public static async Task Should_output_diagnostic_to_stderr()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    CompletionSourceRegistry registry = new();
    string[] words = ["app"];

    // Act
    (int _, string _, string errorOutput) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(1, words, registry, builder.EndpointCollection));

    // Assert
    errorOutput.ShouldContain("Completion ended with directive");

    await Task.CompletedTask;
  }

  public static async Task Should_use_registered_parameter_source()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);

    CompletionSourceRegistry registry = new();
    TestCompletionSource envSource = new(["production", "staging", "development"]);
    registry.RegisterForParameter("env", envSource);

    string[] words = ["app", "deploy"];
    int cursorIndex = 2;

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    output.ShouldContain("production");
    output.ShouldContain("staging");
    output.ShouldContain("development");

    await Task.CompletedTask;
  }

  public static async Task Should_use_registered_type_source()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env} --mode {mode}", (string env, DeploymentMode mode) => 0);

    CompletionSourceRegistry registry = new();
    EnumCompletionSource<DeploymentMode> enumSource = new();
    registry.RegisterForType(typeof(DeploymentMode), enumSource);

    string[] words = ["app", "deploy", "production", "--mode"];
    int cursorIndex = 4;

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    output.ShouldContain("Fast");
    output.ShouldContain("Standard");
    output.ShouldContain("Slow");

    await Task.CompletedTask;
  }

  public static async Task Should_fallback_to_default_source_when_no_registration()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("git status", () => 0);
    builder.Map("git push", () => 0);
    builder.Map("git pull", () => 0);

    CompletionSourceRegistry registry = new(); // Empty registry
    string[] words = ["app", "git"];
    int cursorIndex = 2;

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    output.ShouldContain("status");
    output.ShouldContain("push");
    output.ShouldContain("pull");

    await Task.CompletedTask;
  }

  public static async Task Should_include_descriptions_in_output()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);

    CompletionSourceRegistry registry = new();
    TestCompletionSourceWithDescriptions source = new([
      ("production", "Production environment"),
      ("staging", "Staging environment")
    ]);
    registry.RegisterForParameter("env", source);

    string[] words = ["app", "deploy"];
    int cursorIndex = 2;

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    output.ShouldContain("production\tProduction environment");
    output.ShouldContain("staging\tStaging environment");

    await Task.CompletedTask;
  }

  public static async Task Should_return_zero_exit_code()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    CompletionSourceRegistry registry = new();

    // Act
    (int exitCode, string _, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(1, ["app"], registry, builder.EndpointCollection));

    // Assert
    exitCode.ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Should_prefer_parameter_name_over_type_registration()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("deploy {env}", (string env) => 0);

    CompletionSourceRegistry registry = new();

    // Register both parameter-specific and type-specific sources
    TestCompletionSource paramSource = new(["custom-env"]);
    TestCompletionSource typeSource = new(["generic-string"]);
    registry.RegisterForParameter("env", paramSource);
    registry.RegisterForType(typeof(string), typeSource);

    string[] words = ["app", "deploy"];
    int cursorIndex = 2;

    // Act
    (int _, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    output.ShouldContain("custom-env"); // Parameter source wins
    output.ShouldNotContain("generic-string");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_empty_words_array()
  {
    // Arrange
    var builder = new NuruAppBuilder();
    builder.Map("status", () => 0);

    CompletionSourceRegistry registry = new();
    string[] words = [];
    int cursorIndex = 0;

    // Act
    (int exitCode, string output, string _) = CaptureHandlerOutput(() =>
      DynamicCompletionHandler.HandleCompletion(cursorIndex, words, registry, builder.EndpointCollection));

    // Assert
    exitCode.ShouldBe(0);
    output.ShouldContain(":4");

    await Task.CompletedTask;
  }

  private static (int exitCode, string stdout, string stderr) CaptureHandlerOutput(Func<int> action)
  {
    TextWriter originalOut = Console.Out;
    TextWriter originalError = Console.Error;

    using StringWriter stdoutWriter = new();
    using StringWriter stderrWriter = new();

    try
    {
      Console.SetOut(stdoutWriter);
      Console.SetError(stderrWriter);

      int exitCode = action();

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

sealed class TestCompletionSource(string[] values) : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    foreach (string value in values)
    {
      yield return new CompletionCandidate(
        Value: value,
        Description: null,
        Type: CompletionType.Parameter
      );
    }
  }
}

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

enum DeploymentMode
{
  Fast,
  Standard,
  Slow
}
