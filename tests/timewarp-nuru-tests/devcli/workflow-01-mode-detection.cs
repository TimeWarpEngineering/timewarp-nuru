#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// CI mode detection matrix for the dev CLI workflow command.
/// Task 458-001: workflow_dispatch must auto-detect Merge (not Release) so a
/// manual dispatch never publishes without an explicit --mode release + a
/// confirmed break-glass gate in workflow.yml.
/// </summary>
[TestTag("DevCli")]
public class ModeDetectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ModeDetectionTests>();

  // --- Auto-detect (explicitMode null) ---

  public static async Task Pull_request_auto_detects_pr()
  {
    CiModeDetector.DetermineMode(null, "pull_request").ShouldBe(CiMode.Pr);
    await Task.CompletedTask;
  }

  public static async Task Push_auto_detects_merge()
  {
    CiModeDetector.DetermineMode(null, "push").ShouldBe(CiMode.Merge);
    await Task.CompletedTask;
  }

  public static async Task Release_event_auto_detects_release()
  {
    CiModeDetector.DetermineMode(null, "release").ShouldBe(CiMode.Release);
    await Task.CompletedTask;
  }

  public static async Task Workflow_dispatch_auto_detects_merge_not_release()
  {
    // The bug this task fixes: dispatch must never default to Release.
    CiModeDetector.DetermineMode(null, "workflow_dispatch").ShouldBe(CiMode.Merge);
    await Task.CompletedTask;
  }

  public static async Task Null_event_name_defaults_to_pr()
  {
    CiModeDetector.DetermineMode(null, null).ShouldBe(CiMode.Pr);
    await Task.CompletedTask;
  }

  public static async Task Schedule_event_defaults_to_pr()
  {
    CiModeDetector.DetermineMode(null, "schedule").ShouldBe(CiMode.Pr);
    await Task.CompletedTask;
  }

  public static async Task Merge_group_event_defaults_to_pr()
  {
    CiModeDetector.DetermineMode(null, "merge_group").ShouldBe(CiMode.Pr);
    await Task.CompletedTask;
  }

  // --- Explicit override ---

  public static async Task Explicit_pr_wins()
  {
    CiModeDetector.DetermineMode("pr", "push").ShouldBe(CiMode.Pr);
    await Task.CompletedTask;
  }

  public static async Task Explicit_merge_wins()
  {
    CiModeDetector.DetermineMode("merge", "pull_request").ShouldBe(CiMode.Merge);
    await Task.CompletedTask;
  }

  public static async Task Explicit_release_wins()
  {
    CiModeDetector.DetermineMode("release", "pull_request").ShouldBe(CiMode.Release);
    await Task.CompletedTask;
  }

  public static async Task Explicit_mode_is_case_insensitive()
  {
    CiModeDetector.DetermineMode("RELEASE", "pull_request").ShouldBe(CiMode.Release);
    await Task.CompletedTask;
  }

  public static async Task Bogus_explicit_mode_throws()
  {
    ArgumentException exception = Should.Throw<ArgumentException>(() => CiModeDetector.DetermineMode("bogus", "push"));
    exception.Message.ShouldContain("Valid values");
    await Task.CompletedTask;
  }

  public static async Task Whitespace_explicit_mode_throws()
  {
    Should.Throw<ArgumentException>(() => CiModeDetector.DetermineMode("   ", "push"));
    await Task.CompletedTask;
  }

  public static async Task Empty_explicit_mode_falls_through_to_event_detection()
  {
    CiModeDetector.DetermineMode("", "push").ShouldBe(CiMode.Merge);
    await Task.CompletedTask;
  }

  // --- Break-glass precedence ---

  public static async Task Explicit_release_overrides_workflow_dispatch_auto_merge()
  {
    CiModeDetector.DetermineMode("release", "workflow_dispatch").ShouldBe(CiMode.Release);
    await Task.CompletedTask;
  }

  public static async Task Explicit_merge_overrides_release_event()
  {
    CiModeDetector.DetermineMode("merge", "release").ShouldBe(CiMode.Merge);
    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
