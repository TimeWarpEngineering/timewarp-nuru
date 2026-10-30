#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Guard matrix for ReleaseGuard's pure classifiers (kanban task
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

  public static async Task Empty_branch_fails_naming_unknown()
  {
    GuardVerdict verdict = ReleaseGuard.CheckBranch("");
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

  // --- SelectReleaseTarget (kanban task 543) ---

  public static async Task Green_head_is_the_target()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget("head", [new ReleaseTargetCommit("head", [], 101)]);
    verdict.Ok.ShouldBeTrue();
    verdict.TargetSha.ShouldBe("head");
    verdict.RunId.ShouldBe(101);
    verdict.SkippedCommitCount.ShouldBe(0);
    verdict.Reason.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Kanban_only_commits_on_top_of_green_target_the_green_commit()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget
    (
      "head",
      [
        new ReleaseTargetCommit("head", ["kanban/done/416-x/task.md"], null),
        new ReleaseTargetCommit("land", ["kanban/in-progress/401-y/task.md", "kanban/done/401-y/task.md"], null),
        new ReleaseTargetCommit("empty", [], null),
        new ReleaseTargetCommit("green", [], 202)
      ]
    );
    verdict.Ok.ShouldBeTrue();
    verdict.TargetSha.ShouldBe("green");
    verdict.RunId.ShouldBe(202);
    verdict.SkippedCommitCount.ShouldBe(3);

    await Task.CompletedTask;
  }

  public static async Task Code_commit_between_head_and_green_refuses_naming_commit_and_path()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget
    (
      "head",
      [
        new ReleaseTargetCommit("head", ["kanban/done/416-x/task.md"], null),
        new ReleaseTargetCommit("code", ["kanban/a.md", "source/x.cs", "source/y.cs"], null),
        new ReleaseTargetCommit("green", [], 202)
      ]
    );
    verdict.Ok.ShouldBeFalse();
    verdict.TargetSha.ShouldBeNull();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("no successful CI run of workflow.yml exists for head");
    verdict.Reason.ShouldContain("Commit code changes source/x.cs (and 1 more)");
    verdict.Reason.ShouldContain("outside kanban/");

    await Task.CompletedTask;
  }

  public static async Task Code_commit_at_head_without_run_refuses()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget("head", [new ReleaseTargetCommit("head", ["source/x.cs"], null)]);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("Commit head changes source/x.cs,");

    await Task.CompletedTask;
  }

  public static async Task No_green_run_in_range_refuses_with_todays_message()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget
    (
      "head",
      [
        new ReleaseTargetCommit("head", ["kanban/a.md"], null),
        new ReleaseTargetCommit("older", ["kanban/b.md"], null)
      ]
    );
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldBe("no successful CI run of workflow.yml exists for head. The Release event would fail at locate-run — wait for CI on this commit or rerun it (gh run rerun <id>).");

    await Task.CompletedTask;
  }

  public static async Task Empty_walk_refuses()
  {
    ReleaseTargetVerdict verdict = ReleaseGuard.SelectReleaseTarget("head", []);
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("no successful CI run");

    await Task.CompletedTask;
  }

  public static async Task Kanban_lookalike_prefix_is_a_shipping_path()
  {
    ReleaseGuard.FindShippingPath(["kanban-tools/x.cs"]).ShouldBe("kanban-tools/x.cs");
    ReleaseGuard.FindShippingPath(["Kanban/x.md"]).ShouldBe("Kanban/x.md");
    ReleaseGuard.FindShippingPath(["kanban/x.md"]).ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- CheckTargetVersion ---

  public static async Task Matching_target_version_passes()
  {
    ReleaseGuard.CheckTargetVersion("1.0.0-beta.2", "1.0.0-beta.2", "green").Ok.ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Different_target_version_fails()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTargetVersion("1.0.0-beta.2", "1.0.0-beta.1", "green");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("1.0.0-beta.1");
    verdict.Reason.ShouldContain("green");
    verdict.Reason.ShouldContain("not kanban-only");

    await Task.CompletedTask;
  }

  public static async Task Unreadable_target_version_fails()
  {
    GuardVerdict verdict = ReleaseGuard.CheckTargetVersion("1.0.0", null, "green");
    verdict.Ok.ShouldBeFalse();
    verdict.Reason.ShouldNotBeNull();
    verdict.Reason.ShouldContain("unreadable");
    verdict.Reason.ShouldContain("source/Directory.Build.props");
    verdict.Reason.ShouldNotContain("not kanban-only");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
