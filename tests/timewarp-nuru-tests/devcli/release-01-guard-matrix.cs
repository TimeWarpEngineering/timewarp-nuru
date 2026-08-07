#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Guard matrix for ReleaseGuard's five pure classifiers (kanban task
/// 458-006, parent 458 finding F7). No process execution — release-command.cs
/// owns the git/gh orchestration that feeds these; --dry-run simulations
/// exercise that wiring separately (this task's verification steps).
/// </summary>
[TestTag("DevCli")]
public class ReleaseGuardTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ReleaseGuardTests>();

  // --- CheckWorkingTree ---

  public static async Task Empty_status_output_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckWorkingTree("");
    verdict.Ok.ShouldBeTrue();
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Whitespace_only_status_output_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckWorkingTree("   \n  \t\n");
    verdict.Ok.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Modified_file_status_output_fails()
  {
    GuardVerdict verdict = ReleaseGuard.CheckWorkingTree(" M file.cs\n");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("commit or stash");

    await Task.CompletedTask;
  }

  public static async Task Untracked_file_status_output_fails()
  {
    GuardVerdict verdict = ReleaseGuard.CheckWorkingTree("?? new.cs\n");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("commit or stash");

    await Task.CompletedTask;
  }

  // --- CheckBranch ---

  public static async Task Master_branch_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckBranch("master");
    verdict.Ok.ShouldBeTrue();
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Dev_branch_fails_naming_branch()
  {
    GuardVerdict verdict = ReleaseGuard.CheckBranch("dev");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("dev");

    await Task.CompletedTask;
  }

  public static async Task Null_branch_fails_naming_unknown()
  {
    GuardVerdict verdict = ReleaseGuard.CheckBranch(null);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("(unknown)");

    await Task.CompletedTask;
  }

  public static async Task Detached_head_fails_naming_head()
  {
    GuardVerdict verdict = ReleaseGuard.CheckBranch("HEAD");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("HEAD");

    await Task.CompletedTask;
  }

  // --- CheckSync ---

  public static async Task Zero_ahead_zero_behind_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckSync(0, 0);
    verdict.Ok.ShouldBeTrue();
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Ahead_only_yields_push_message()
  {
    GuardVerdict verdict = ReleaseGuard.CheckSync(2, 0);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("push");

    await Task.CompletedTask;
  }

  public static async Task Behind_only_yields_pull_message()
  {
    GuardVerdict verdict = ReleaseGuard.CheckSync(0, 3);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("pull");

    await Task.CompletedTask;
  }

  public static async Task Ahead_and_behind_yields_diverged_message()
  {
    GuardVerdict verdict = ReleaseGuard.CheckSync(1, 1);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("diverged");

    await Task.CompletedTask;
  }

  public static async Task Negative_ahead_throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => ReleaseGuard.CheckSync(-1, 0));
    await Task.CompletedTask;
  }

  public static async Task Negative_behind_throws()
  {
    Should.Throw<ArgumentOutOfRangeException>(() => ReleaseGuard.CheckSync(0, -1));
    await Task.CompletedTask;
  }

  // --- CheckTagAvailability ---

  public static async Task Tag_absent_everywhere_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTagAvailability("v1.0.0", existsLocally: false, existsOnRemote: false);
    verdict.Ok.ShouldBeTrue();
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Tag_local_only_fails_naming_local()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTagAvailability("v1.0.0", existsLocally: true, existsOnRemote: false);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("v1.0.0");
    verdict.Reason.ShouldContain("locally");
    verdict.Reason.ShouldContain("not on origin");

    await Task.CompletedTask;
  }

  public static async Task Tag_remote_only_fails_naming_remote()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTagAvailability("v1.0.0", existsLocally: false, existsOnRemote: true);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("origin");
    verdict.Reason.ShouldContain("not locally");

    await Task.CompletedTask;
  }

  public static async Task Tag_local_and_remote_fails_naming_both()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTagAvailability("v1.0.0", existsLocally: true, existsOnRemote: true);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("locally and on origin");

    await Task.CompletedTask;
  }

  // --- CheckPublishState ---

  public static async Task Publish_state_none_passes()
  {
    GuardVerdict verdict = ReleaseGuard.CheckPublishState(PublishState.None, "1.0.0");
    verdict.Ok.ShouldBeTrue();
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Publish_state_all_fails_with_bump_message()
  {
    GuardVerdict verdict = ReleaseGuard.CheckPublishState(PublishState.All, "1.0.0");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("already released");
    verdict.Reason.ShouldContain("bump");

    await Task.CompletedTask;
  }

  public static async Task Publish_state_partial_fails_with_resume_or_bump_message()
  {
    GuardVerdict verdict = ReleaseGuard.CheckPublishState(PublishState.Partial, "1.0.0");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("never tagged");
    verdict.Reason.ShouldContain("break-glass");
    verdict.Reason.ShouldContain("bump");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
