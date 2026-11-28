#!/usr/bin/env -S dotnet --

#region Purpose
// Pins the green-key-v1 rules (task 540): manifest, event class, marker choice,
// carry order, the shadow would-line, and the workflow wiring that does not skip.
#endregion

// The whole file is #if !JARIBU_MULTI. The rules compile only into this directory's
// standalone props. Listed in CiTestExcludes and run by run-standalone-tests.cs.
//   dotnet run --file tests/timewarp-nuru-tests/devcli/green-key-01-rules.cs

#if !JARIBU_MULTI
return await RunAllTests();

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Green-key rules for the shadow ledger. The skip stays off until the shadow window is read.
/// </summary>
[TestTag("DevCli")]
public class GreenKeyRulesTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<GreenKeyRulesTests>();

  private const string PinnedManifest =
    "green-key-v1\n"
    + "sdk 11.0.0\n"
    + "global.json abcdef\n"
    + "workflow .github/workflows/workflow.yml 111111\n"
    + "script .github/scripts/build.sh 222222\n"
    + "runner dpkg:abc\n"
    + "images none\n"
    + "tree readme.md 333333\n"
    + "tree source/app.cs 444444\n";

  private const string PinnedKey = "46f6f62f0418508c38629450c2b8c2ec5b61061d8ead8763408304d9783f0761";

  public static async Task Manifest_hash_matches_the_pinned_text()
  {
    GreenKeyManifest.Hash(PinnedManifest).ShouldBe(PinnedKey);
    await Task.CompletedTask;
  }

  public static async Task From_ls_tree_drops_non_code_paths()
  {
    string lsTree =
      "100644 blob abcdef\tglobal.json\n"
      + "100644 blob 111111\t.github/workflows/workflow.yml\n"
      + "100644 blob 222222\t.github/scripts/build.sh\n"
      + "100644 blob 333333\treadme.md\n"
      + "100644 blob 444444\tsource/app.cs\n"
      + "100644 blob 555555\tkanban/task.md\n"
      + "100644 blob 666666\tchangelog.md\n"
      + "100644 blob 777777\tassets/mascot/nuru.png\n"
      + "100644 blob 888888\tassets/timewarp-nuru-avatar.svg\n";

    GreenKeyComputation computed = GreenKeyTree.FromLsTree(lsTree, "11.0.0", "dpkg:abc", []);
    computed.Key.ShouldBe(PinnedKey);
    computed.Manifest.ShouldBe(PinnedManifest);
    computed.TreeCount.ShouldBe(2);
    await Task.CompletedTask;
  }

  public static async Task Code_inputs_keep_packed_files_and_drop_nuru_exclusions()
  {
    GreenKeyManifest.IsCodeInputPath("readme.md").ShouldBeTrue();
    GreenKeyManifest.IsCodeInputPath("assets/logo.png").ShouldBeTrue();
    GreenKeyManifest.IsCodeInputPath("source/app.cs").ShouldBeTrue();
    GreenKeyManifest.IsCodeInputPath("kanban/task.md").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("documentation/guide.md").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath(".agent/notes.md").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath(".claude/settings.json").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("skills/tw-nuru/SKILL.md").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("assets/mascot/nuru.png").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("changelog.md").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("assets/timewarp-nuru-avatar.svg").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath("global.json").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath(".github/workflows/workflow.yml").ShouldBeFalse();
    GreenKeyManifest.IsCodeInputPath(".github/scripts/build.sh").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Event_class_matches_the_ledger_rules()
  {
    Classify("push", "refs/heads/master", "o/r", "o/r", false).ShouldBe((GreenKeyDisposition.Candidate, "master-push"));
    Classify("push", "refs/heads/feature", "o/r", null, false).ShouldBe((GreenKeyDisposition.Excluded, "non-master-push"));
    Classify("pull_request", "refs/pull/1/merge", "o/r", "o/r", false)
      .ShouldBe((GreenKeyDisposition.Candidate, "same-repo-pull-request"));
    Classify("pull_request", "refs/pull/1/merge", "o/r", "fork/r", false)
      .ShouldBe((GreenKeyDisposition.Fork, "fork-pull-request"));
    Classify("pull_request", "refs/pull/1/merge", "o/r", null, false)
      .ShouldBe((GreenKeyDisposition.Fork, "pull-request-repo-unknown"));
    Classify("release", "refs/tags/v1", "o/r", null, false).ShouldBe((GreenKeyDisposition.Excluded, "release"));
    Classify("schedule", "refs/heads/master", "o/r", null, false).ShouldBe((GreenKeyDisposition.Excluded, "schedule"));
    Classify("workflow_dispatch", "refs/heads/master", "o/r", null, false)
      .ShouldBe((GreenKeyDisposition.Excluded, "workflow_dispatch"));
    Classify(null, null, null, null, false).ShouldBe((GreenKeyDisposition.Excluded, "not-a-candidate-event"));
    Classify("push", "refs/heads/master", "o/r", null, true).ShouldBe((GreenKeyDisposition.Excluded, "release"));
    await Task.CompletedTask;
  }

  public static async Task Marker_selects_the_earliest_live_artifact()
  {
    const string Key = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    GreenKeyArtifactRow newer = new(20, false, "2026-02-01T00:00:00Z", 9, "bbb");
    GreenKeyArtifactRow older = new(11, false, "2026-01-01T00:00:00Z", 3, "aaa");
    GreenKeyArtifactRow expired = new(1, true, "2025-01-01T00:00:00Z", 2, "old");
    GreenKeyArtifactRow tiedLater = new(12, false, "2026-01-01T00:00:00Z", 4, "ccc");

    GreenKeyMarker? marker = GreenKeyMarkers.SelectMarker(Key, [newer, tiedLater, expired, older]);
    marker.ShouldNotBeNull();
    marker!.ArtifactId.ShouldBe(11);
    marker.RunId.ShouldBe(3);
    GreenKeyMarkers.SelectMarker(Key, [expired]).ShouldBeNull();
    await Task.CompletedTask;
  }

  public static async Task Carry_candidates_are_newest_first_and_distinct()
  {
    GreenKeyArtifactRow[] rows =
    [
      new(1, false, "2026-01-01T00:00:00Z", 10, "a"),
      new(2, false, "2026-03-01T00:00:00Z", 30, "c"),
      new(3, false, "2026-03-01T00:00:00Z", 30, "c-dup"),
      new(4, true, "2026-04-01T00:00:00Z", 40, "expired"),
      new(5, false, "2026-02-01T00:00:00Z", 20, "b")
    ];

    GreenKeySkip.CarryCandidateRuns(rows).ShouldBe([30L, 20L, 10L]);
    await Task.CompletedTask;
  }

  public static async Task Live_packages_ignore_expired_and_other_names()
  {
    GreenKeySkip.HasLivePackagesArtifact(
      """{"artifacts":[{"name":"Packages-12","expired":false}]}""").ShouldBeTrue();
    GreenKeySkip.HasLivePackagesArtifact(
      """{"artifacts":[{"name":"Packages-12","expired":true},{"name":"staged-packages","expired":false}]}""")
      .ShouldBeFalse();
    GreenKeySkip.HasLivePackagesArtifact("").ShouldBeFalse();
    GreenKeySkip.HasLivePackagesArtifact("not-json").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Force_trailer_is_case_insensitive()
  {
    GreenKeySkip.IsForceFullRun("full-run").ShouldBeTrue();
    GreenKeySkip.IsForceFullRun("Full-Run\n").ShouldBeTrue();
    GreenKeySkip.IsForceFullRun("other").ShouldBeFalse();
    GreenKeySkip.IsForceFullRun(null).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Run_kind_matches_phase_two()
  {
    GreenKeySkip.RunKind(GreenKeyDisposition.Candidate, true, false, false, false).ShouldBe(GreenKeyRunKind.Skip);
    GreenKeySkip.RunKind(GreenKeyDisposition.Candidate, true, false, true, true).ShouldBe(GreenKeyRunKind.Skip);
    GreenKeySkip.RunKind(GreenKeyDisposition.Candidate, true, false, true, false).ShouldBe(GreenKeyRunKind.PackOnly);
    GreenKeySkip.RunKind(GreenKeyDisposition.Candidate, true, true, true, true).ShouldBe(GreenKeyRunKind.Full);
    GreenKeySkip.RunKind(GreenKeyDisposition.Candidate, false, false, false, false).ShouldBe(GreenKeyRunKind.Full);
    GreenKeySkip.RunKind(GreenKeyDisposition.Fork, true, false, false, false).ShouldBe(GreenKeyRunKind.Full);
    GreenKeySkip.RunKind(GreenKeyDisposition.Excluded, true, false, true, true).ShouldBe(GreenKeyRunKind.Full);
    await Task.CompletedTask;
  }

  public static async Task End_line_marks_a_failed_hit_and_leaves_path_skips_out_of_the_rate()
  {
    string failed = GreenKeyLog.FormatPipelineEnd("hit", GreenKeyLog.Failed, "abc", "1", "def");
    failed.ShouldContain("false_skip=true");
    failed.ShouldContain("rate=in");

    string skipped = GreenKeyLog.FormatPipelineEnd("miss", GreenKeyLog.PathSkipped, "abc", "1", "def");
    skipped.ShouldContain("false_skip=false");
    skipped.ShouldContain("rate=out");
    skipped.ShouldContain("pipeline=path-skipped");

    string passed = GreenKeyLog.FormatPipelineEnd("hit", GreenKeyLog.Succeeded, "abc", "1", "def");
    passed.ShouldContain("false_skip=false");
    passed.ShouldContain("rate=in");
    await Task.CompletedTask;
  }

  public static async Task Would_line_says_shadow_and_not_skip_mode()
  {
    string line = GreenKeyLog.FormatWould("skip", "hit", PinnedKey, "9", "abc", 44);
    line.ShouldContain("shadow=log-only");
    line.ShouldContain("would=skip");
    line.ShouldContain("carry_run=44");
    line.ShouldNotContain("mode=skip");
    line.ShouldNotContain("skip=yes");
    await Task.CompletedTask;
  }

  public static async Task Shadow_mode_forces_a_full_run()
  {
    GreenKeyShadow.Enabled.ShouldBeTrue();
    GreenKeyShadow.Effective(GreenKeyRunKind.Skip).ShouldBe(GreenKeyRunKind.Full);
    GreenKeyShadow.Effective(GreenKeyRunKind.PackOnly).ShouldBe(GreenKeyRunKind.Full);
    await Task.CompletedTask;
  }

  public static async Task Runner_describe_and_workflow_image_pulls()
  {
    GreenKeyRunnerImage.Describe("override", "ubuntu", "1", "abc").ShouldBe("override");
    GreenKeyRunnerImage.Describe(null, "ubuntu24", "20260101.1.1", "ABCDEF")
      .ShouldBe("hosted-image:ubuntu24:20260101.1.1|dpkg:abcdef");
    GreenKeyRunnerImage.Describe(null, null, null, "abc").ShouldBe("dpkg:abc");
    GreenKeyRunnerImage.Describe(null, null, null, null).ShouldBe("unknown");

    string workflow = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "workflow.yml"));
    GreenKeyRunnerImage.FindImagePulls(workflow).ShouldBeEmpty();
    GreenKeyRunnerImage.FindImagePulls("docker pull example.invalid/app:1\nimage: other.invalid/job:2\n")
      .ShouldBe(["example.invalid/app:1", "other.invalid/job:2"]);
    GreenKeyManifest.IsKey(PinnedKey).ShouldBeTrue();
    GreenKeyManifest.IsKey("abc").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Workflow_logs_the_ledger_and_does_not_skip_stages()
  {
    string workflow = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "workflow.yml"));
    workflow.ShouldContain("workflow --stage green-key\n");
    workflow.ShouldContain("workflow --stage green-key-finish\n");
    workflow.ShouldContain("retention-days: 90");
    workflow.ShouldContain("Shadow mode");
    workflow.ShouldContain("needs: [changes, stage, green-key]");
    workflow.ShouldContain("if: github.ref == 'refs/heads/master'");

    int stageAt = workflow.IndexOf("\n  stage:\n", StringComparison.Ordinal);
    int ciAt = workflow.IndexOf("\n  ci:\n", stageAt, StringComparison.Ordinal);
    stageAt.ShouldBeGreaterThan(0);
    ciAt.ShouldBeGreaterThan(stageAt);
    string stageJob = workflow[stageAt..ciAt];
    stageJob.ShouldContain("needs.changes.outputs.code");
    stageJob.ShouldNotContain("green_key");
    await Task.CompletedTask;
  }

  private static (GreenKeyDisposition Disposition, string Reason) Classify(
    string? eventName,
    string? githubRef,
    string? repository,
    string? headRepository,
    bool releaseMode)
  {
    GreenKeyDisposition disposition = GreenKeyEvent.Classify(
      eventName,
      githubRef,
      repository,
      headRepository,
      releaseMode,
      out string reason);
    return (disposition, reason);
  }

  private static string RepoRoot()
  {
    string start = AppContext.GetData("EntryPointFileDirectoryPath") as string ?? Directory.GetCurrentDirectory();
    DirectoryInfo? directory = new(start);
    while (directory is not null)
    {
      if (File.Exists(Path.Combine(directory.FullName, "timewarp-nuru.slnx")))
      {
        return directory.FullName;
      }

      directory = directory.Parent;
    }

    throw new InvalidOperationException("timewarp-nuru.slnx was not found.");
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
#endif
