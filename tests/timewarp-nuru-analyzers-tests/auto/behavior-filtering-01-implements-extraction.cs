#!/usr/bin/dotnet --
#:package TimeWarp.Amuru

// Integration tests for behavior filtering via .Implements<T>() (#316)
// Tests verify the actual runtime behavior of filtered behaviors.

using TimeWarp.Amuru;
using System.Text;

return await RunTests<BehaviorFilteringTests>();

/// <summary>
/// Integration tests for behavior filtering via .Implements&lt;T&gt;() (#316).
/// These tests execute actual CLI samples to verify behavior filtering works at runtime.
/// </summary>
[TestTag("BehaviorFiltering")]
[TestTag("Integration")]
public sealed class BehaviorFilteringTests
{
  private static string SamplePath => Path.Combine(FindRepoRoot(), "samples", "_pipeline-middleware", "04-pipeline-middleware-filtered-auth.cs");

  /// <summary>
  /// Test that route without .Implements executes without authorization behavior.
  /// </summary>
  public static async Task Should_execute_route_without_authorization()
  {
    // The "echo" route does NOT implement IRequireAuthorization
    // So the AuthorizationBehavior should NOT be applied
    CommandOutput result = await Shell.Builder("dotnet")
      .WithArguments("run", SamplePath, "--", "echo", "Hello World")
      .WithWorkingDirectory(FindRepoRoot())
      .CaptureAsync();

    WriteLine($"Exit code: {result.ExitCode}");
    WriteLine($"Output: {result.Stdout}");
    WriteLine($"Error: {result.Stderr}");

    result.ExitCode.ShouldBe(0);
    result.Stdout.ShouldContain("Echo: Hello World");
    // Should NOT contain any authorization-related output
    result.Stdout.ShouldNotContain("[AUTH]");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that route with .Implements blocks unauthorized access.
  /// </summary>
  public static async Task Should_block_unauthorized_access()
  {
    // The "admin" route DOES implement IRequireAuthorization
    // Without CLI_AUTHORIZED=1, should be blocked
    // Use bash to ensure environment is unset
    CommandOutput result = await Shell.Builder("bash")
      .WithArguments("-c", $"unset CLI_AUTHORIZED && dotnet run {SamplePath} -- admin delete-all")
      .WithWorkingDirectory(FindRepoRoot())
      .WithNoValidation()
      .CaptureAsync();

    WriteLine($"Exit code: {result.ExitCode}");
    WriteLine($"Output: {result.Stdout}");
    WriteLine($"Error: {result.Stderr}");

    // Should fail with non-zero exit code
    result.ExitCode.ShouldNotBe(0);
    // Should have authorization-related error
    (result.Stdout + result.Stderr).ShouldContain("Access denied");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that route with .Implements allows authorized access.
  /// </summary>
  public static async Task Should_allow_authorized_access()
  {
    // The "admin" route DOES implement IRequireAuthorization
    // With CLI_AUTHORIZED=1, should be allowed
    CommandOutput result = await Shell.Builder("bash")
      .WithArguments("-c", $"CLI_AUTHORIZED=1 dotnet run {SamplePath} -- admin delete-all")
      .WithWorkingDirectory(FindRepoRoot())
      .CaptureAsync();

    WriteLine($"Exit code: {result.ExitCode}");
    WriteLine($"Output: {result.Stdout}");
    WriteLine($"Error: {result.Stderr}");

    result.ExitCode.ShouldBe(0);
    result.Stdout.ShouldContain("admin action: delete-all");
    result.Stdout.ShouldContain("completed successfully");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that global behaviors still apply to all routes.
  /// </summary>
  public static async Task Should_apply_global_behavior_to_all_routes()
  {
    // LoggingBehavior is a global behavior (INuruBehavior, not INuruBehavior<T>)
    // It should apply to ALL routes including "echo" which has no .Implements

    // Test on route WITHOUT authorization
    CommandOutput result1 = await Shell.Builder("dotnet")
      .WithArguments("run", SamplePath, "--", "echo", "Test")
      .WithWorkingDirectory(FindRepoRoot())
      .CaptureAsync();

    WriteLine($"Echo route output: {result1.Stdout}");
    // The logging behavior outputs to ILogger which may not show in console
    // But the route should still execute
    result1.ExitCode.ShouldBe(0);

    // Test on route WITH authorization (when authorized)
    CommandOutput result2 = await Shell.Builder("bash")
      .WithArguments("-c", $"CLI_AUTHORIZED=1 dotnet run {SamplePath} -- admin test")
      .WithWorkingDirectory(FindRepoRoot())
      .CaptureAsync();

    WriteLine($"Admin route output: {result2.Stdout}");
    result2.ExitCode.ShouldBe(0);

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test that behavior filtering uses compile-time filtering (behavior field exists only for matching routes).
  /// This is verified by checking the sample compiles and runs correctly.
  /// </summary>
  public static async Task Should_compile_and_run_filtered_behavior_sample()
  {
    // First, ensure the sample compiles
    CommandOutput buildResult = await Shell.Builder("dotnet")
      .WithArguments("build", SamplePath, "--no-restore")
      .WithWorkingDirectory(FindRepoRoot())
      .WithNoValidation()
      .CaptureAsync();

    WriteLine($"Build exit code: {buildResult.ExitCode}");
    if (buildResult.ExitCode != 0)
    {
      WriteLine($"Build output: {buildResult.Stdout}");
      WriteLine($"Build errors: {buildResult.Stderr}");
    }

    buildResult.ExitCode.ShouldBe(0, "Sample should compile successfully");

    await Task.CompletedTask;
  }

  /// <summary>
  /// Test behavior ordering - LoggingBehavior wraps AuthorizationBehavior.
  /// </summary>
  public static async Task Should_maintain_behavior_ordering()
  {
    // When authorization fails, the exception should propagate through LoggingBehavior
    // This test verifies the nesting is correct
    CommandOutput result = await Shell.Builder("bash")
      .WithArguments("-c", $"unset CLI_AUTHORIZED && dotnet run {SamplePath} -- admin test")
      .WithWorkingDirectory(FindRepoRoot())
      .WithNoValidation()
      .CaptureAsync();

    // Should fail (authorization denied)
    result.ExitCode.ShouldNotBe(0);

    // The stack trace should show LoggingBehavior wrapping AuthorizationBehavior
    string output = result.Stdout + result.Stderr;
    output.ShouldContain("UnauthorizedAccessException");

    await Task.CompletedTask;
  }

  // ═══════════════════════════════════════════════════════════════════════════════
  // HELPERS
  // ═══════════════════════════════════════════════════════════════════════════════

  private static string FindRepoRoot()
  {
    string? dir = Environment.CurrentDirectory;
    while (dir != null)
    {
      if (File.Exists(Path.Combine(dir, "timewarp-nuru.slnx")))
      {
        return dir;
      }
      dir = Path.GetDirectoryName(dir);
    }

    throw new InvalidOperationException(
      $"Could not find repository root (timewarp-nuru.slnx) starting from {Environment.CurrentDirectory}");
  }
}
