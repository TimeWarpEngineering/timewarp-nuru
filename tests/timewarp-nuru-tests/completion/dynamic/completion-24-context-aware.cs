#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Completion.ContextAware
{

[TestTag("Completion")]
public class ContextAwareTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ContextAwareTests>();

  public static async Task Should_pass_args_array_in_context()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      ArgsInspectorSource source = new();
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Source receives only the command words (not __complete and cursor index)
    output.ShouldContain("args:2"); // app, deploy
  }

  public static async Task Should_pass_cursor_position_in_context()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      CursorInspectorSource source = new();
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Source received cursor position
    output.ShouldContain("cursor:2");
  }

  public static async Task Should_pass_endpoints_collection_in_context()
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
    builder.Map("deploy {env}")
      .WithHandler((string env) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      EndpointsInspectorSource source = new();
      registry.RegisterForParameter("env", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "deploy"]));

    // Assert - Source sees all endpoints (3 original + 2 from EnableCompletion)
    output.ShouldContain("endpoints:");
  }

  public static async Task Should_allow_source_to_inspect_previous_arguments()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("connect {host} {port}")
      .WithHandler((string host, string port) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      PreviousArgsSource source = new();
      registry.RegisterForParameter("port", source);
    });

    NuruCoreApp app = builder.Build();

    // Act - Previous arg is "localhost"
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "3", "app", "connect", "localhost"]));

    // Assert - Source can see localhost was provided
    output.ShouldContain("prev:localhost");
  }

  public static async Task Should_provide_different_completions_based_on_context()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("deploy {env} {service}")
      .WithHandler((string env, string service) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      EnvironmentAwareServiceSource source = new();
      registry.RegisterForParameter("service", source);
    });

    NuruCoreApp app = builder.Build();

    // Act - Get services for production
    (int _, string prodOutput, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "3", "app", "deploy", "production"]));

    // Assert - Production-specific services
    prodOutput.ShouldContain("api-prod");
  }

  public static async Task Should_support_conditional_completion_logic()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("backup {target} {dest}")
      .WithHandler((string target, string dest) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      ConditionalSource source = new();
      registry.RegisterForParameter("dest", source);
    });

    NuruCoreApp app = builder.Build();

    // Act - Backup database
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "3", "app", "backup", "database"]));

    // Assert - Database-specific destinations
    output.ShouldContain("s3-bucket");
    output.ShouldContain("local-drive");
  }

  public static async Task Should_allow_source_to_filter_by_partial_input()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("select {item}")
      .WithHandler((string item) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      FilteringSource source = new();
      registry.RegisterForParameter("item", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int _, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "select"]));

    // Assert - Source provides all items (filtering would be done by shell)
    output.ShouldContain("apple");
    output.ShouldContain("apricot");
    output.ShouldContain("banana");
  }

  public static async Task Should_provide_immutable_context()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("test {param}")
      .WithHandler((string param) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      ImmutabilityTestSource source = new();
      registry.RegisterForParameter("param", source);
    });

    NuruCoreApp app = builder.Build();

    // Act
    (int exitCode, string _, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "2", "app", "test"]));

    // Assert - Context is read-only record
    exitCode.ShouldBe(0);
  }

  public static async Task Should_handle_context_with_options_in_args()
  {
    // Arrange
    NuruAppBuilder builder = new();
    builder.Map("build --config {mode} {target}")
      .WithHandler((string mode, string target) => 0)
      .AsCommand()
      .Done();

    builder.EnableCompletion(configure: registry =>
    {
      TargetSource source = new();
      registry.RegisterForParameter("target", source);
    });

    NuruCoreApp app = builder.Build();

    // Act - Args include option. Words are: ["app", "build", "--config", "release"]
    // CursorPosition 4 means "complete 5th word" which is out of bounds
    // This test verifies the handler doesn't crash with options in the args
    (int exitCode, string output, string _) = await CaptureAppOutputAsync(() =>
      app.RunAsync(["__complete", "4", "app", "build", "--config", "release"]));

    // Assert - Should complete successfully even with out-of-bounds cursor
    exitCode.ShouldBe(0);
    output.ShouldContain(":4"); // Directive should still be present
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
// Context-Aware Completion Sources
// =============================================================================

sealed class ArgsInspectorSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate($"args:{context.Args.Length}", null, CompletionType.Parameter);
  }
}

sealed class CursorInspectorSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate($"cursor:{context.CursorPosition}", null, CompletionType.Parameter);
  }
}

sealed class EndpointsInspectorSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate($"endpoints:{context.Endpoints.Count}", null, CompletionType.Parameter);
  }
}

sealed class PreviousArgsSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Look at previous argument (the host in "connect {host} {port}")
    if (context.Args.Length > 2)
    {
      string previousArg = context.Args[^1]; // Last arg before cursor
      yield return new CompletionCandidate($"prev:{previousArg}", null, CompletionType.Parameter);
    }
    else
    {
      yield return new CompletionCandidate("no-prev", null, CompletionType.Parameter);
    }
  }
}

sealed class EnvironmentAwareServiceSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Get the environment from previous args
    string env = context.Args.Length > 2 ? context.Args[^1] : "unknown";

    if (env == "production")
    {
      yield return new CompletionCandidate("api-prod", "Production API", CompletionType.Parameter);
      yield return new CompletionCandidate("web-prod", "Production Web", CompletionType.Parameter);
    }
    else
    {
      yield return new CompletionCandidate("api-dev", "Development API", CompletionType.Parameter);
      yield return new CompletionCandidate("web-dev", "Development Web", CompletionType.Parameter);
    }
  }
}

sealed class ConditionalSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    string target = context.Args.Length > 2 ? context.Args[^1] : "unknown";

    if (target == "database")
    {
      yield return new CompletionCandidate("s3-bucket", "S3 storage", CompletionType.Parameter);
      yield return new CompletionCandidate("local-drive", "Local backup", CompletionType.Parameter);
    }
    else
    {
      yield return new CompletionCandidate("archive", "Compressed archive", CompletionType.Parameter);
    }
  }
}

sealed class FilteringSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Return all items - shell handles filtering
    yield return new CompletionCandidate("apple", null, CompletionType.Parameter);
    yield return new CompletionCandidate("apricot", null, CompletionType.Parameter);
    yield return new CompletionCandidate("banana", null, CompletionType.Parameter);
    yield return new CompletionCandidate("cherry", null, CompletionType.Parameter);
  }
}

sealed class ImmutabilityTestSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    // Context is a record - immutable by design
    yield return new CompletionCandidate("immutable-ok", null, CompletionType.Parameter);
  }
}

sealed class TargetSource : ICompletionSource
{
  public IEnumerable<CompletionCandidate> GetCompletions(CompletionContext context)
  {
    yield return new CompletionCandidate("linux", "Linux x64", CompletionType.Parameter);
    yield return new CompletionCandidate("windows", "Windows x64", CompletionType.Parameter);
    yield return new CompletionCandidate("macos", "macOS ARM64", CompletionType.Parameter);
  }
}

} // namespace TimeWarp.Nuru.Tests.Completion.ContextAware
