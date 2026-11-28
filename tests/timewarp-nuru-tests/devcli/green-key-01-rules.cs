#!/usr/bin/env -S dotnet --

#region Purpose
// Pins the green-key-v1 rules (task 540, enable task 603): manifest, event class,
// marker choice, carry order, the acted skip line, and the workflow that skips.
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
/// Green-key rules. The skip is on: a hit the workflow acts on logs skip=yes, and ci stays green.
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

  public static async Task Carry_candidates_are_newest_master_rows()
  {
    GreenKeyMarkers.TryParseArtifactRow(
      """{"id":7,"expired":false,"created_at":"2026-06-01T00:00:00Z","run_id":70,"head_sha":"abc","head_branch":"master"}""",
      out GreenKeyArtifactRow parsed).ShouldBeTrue();
    parsed.HeadBranch.ShouldBe(GreenKeySkip.MasterBranchName);
    parsed.RunId.ShouldBe(70L);
    GreenKeyMarkers.ArtifactListJq.ShouldContain("head_branch:.workflow_run.head_branch");

    GreenKeyArtifactRow[] rows =
    [
      new(1, false, "2026-01-01T00:00:00Z", 10, "a", "master"),
      new(2, false, "2026-03-01T00:00:00Z", 30, "c", "master"),
      new(3, false, "2026-03-01T00:00:00Z", 30, "c-dup", "master"),
      new(4, true, "2026-04-01T00:00:00Z", 40, "expired", "master"),
      new(5, false, "2026-02-01T00:00:00Z", 20, "b", "master"),
      new(6, false, "2026-05-01T00:00:00Z", 50, "pr", "task/540-enable"),
      new(8, false, "2026-05-02T00:00:00Z", 60, "missing", null)
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

    string cancelled = GreenKeyLog.FormatPipelineEnd("hit", GreenKeyLog.Cancelled, "abc", "1", "def");
    cancelled.ShouldContain("pipeline=cancelled");
    cancelled.ShouldContain("false_skip=false");
    cancelled.ShouldContain("rate=out");

    string keySkipped = GreenKeyLog.FormatPipelineEnd("hit", GreenKeyLog.KeySkipped, "abc", "1", "def");
    keySkipped.ShouldContain("pipeline=key-skipped");
    keySkipped.ShouldContain("false_skip=false");
    keySkipped.ShouldContain("rate=out");
    GreenKeyLog.CountsTowardHitRate(GreenKeyLog.KeySkipped).ShouldBeFalse();
    GreenKeyLog.CountsTowardHitRate(GreenKeyLog.RunPackOnly).ShouldBeFalse();
    GreenKeyLog.NormalizePipeline(GreenKeyLog.KeySkipped).ShouldBe(GreenKeyLog.KeySkipped);
    GreenKeyLog.NormalizePipeline(GreenKeyLog.Cancelled).ShouldBe(GreenKeyLog.Cancelled);
    GreenKeyLog.NormalizePipeline("nope").ShouldBe(GreenKeyLog.Failed);
    GreenKeyLog.NormalizePipeline(null).ShouldBe(GreenKeyLog.Failed);
    await Task.CompletedTask;
  }

  public static async Task Acted_line_names_the_trusted_run_and_the_carry()
  {
    GreenKeyMarker trusted = new(PinnedKey, 42, "deadbeef", 7);
    string skip = GreenKeyLog.FormatSkip(PinnedKey, "9", "abc", trusted, 44, "Packages-12");
    skip.ShouldBe(
      "green-key-v1 skip=yes key=" + PinnedKey
      + " run=9 commit=abc trusted_run=42 trusted_commit=deadbeef carry_run=44 carry_artifact=Packages-12");
    skip.ShouldNotContain("shadow=log-only");

    string pack = GreenKeyLog.FormatPackOnly(PinnedKey, "9", "abc", trusted, "no-packages-to-carry");
    pack.ShouldContain("skip=pack-only");
    pack.ShouldContain("carry=no-packages-to-carry");

    string declined = GreenKeyLog.FormatSkipDeclined(PinnedKey, "9", "abc", "carry-unknown");
    declined.ShouldContain("skip=no");
    declined.ShouldContain("reason=carry-unknown");

    string? acted = GreenKeyLog.FormatActed(
      GreenKeyRunKind.Skip,
      "carried",
      PinnedKey,
      "9",
      "abc",
      trusted,
      44,
      "hit");
    acted.ShouldNotBeNull();
    acted!.ShouldContain("skip=yes");
    acted.ShouldContain("carry_run=44");
    GreenKeyLog.FormatActed(GreenKeyRunKind.Full, "miss", PinnedKey, "9", "abc", null, null, "miss").ShouldBeNull();

    string would = GreenKeyLog.FormatWould("full", "miss", PinnedKey, "9", "abc", null);
    would.ShouldContain("mode=skip");
    would.ShouldContain("would=full");
    would.ShouldNotContain("shadow=log-only");
    would.ShouldNotContain("skip=yes");
    await Task.CompletedTask;
  }

  public static async Task Enable_acts_on_the_run_kind()
  {
    GreenKeyShadow.Enabled.ShouldBeFalse();
    GreenKeyShadow.Effective(GreenKeyRunKind.Skip).ShouldBe(GreenKeyRunKind.Skip);
    GreenKeyShadow.Effective(GreenKeyRunKind.PackOnly).ShouldBe(GreenKeyRunKind.PackOnly);
    GreenKeyShadow.Effective(GreenKeyRunKind.Full).ShouldBe(GreenKeyRunKind.Full);
    GreenKeyLog.RunKindName(GreenKeyRunKind.Skip).ShouldBe(GreenKeyLog.RunSkip);
    GreenKeyLog.RunKindFromWould("pack-only").ShouldBe(GreenKeyRunKind.PackOnly);
    GreenKeyLog.PipelineFor("skip", "skipped", "success").ShouldBe(GreenKeyLog.KeySkipped);
    GreenKeyLog.PipelineFor("skip", "skipped", "failure").ShouldBe(GreenKeyLog.KeySkipFailed);
    GreenKeyLog.PipelineFor("skip", "cancelled", "success").ShouldBe(GreenKeyLog.Cancelled);
    GreenKeyLog.PipelineFor("pack-only", "success", "skipped").ShouldBe(GreenKeyLog.RunPackOnly);
    GreenKeyLog.PipelineFor("pack-only", "failure", "skipped").ShouldBe(GreenKeyLog.PackOnlyFailed);
    GreenKeyLog.PipelineFor("full", "success", "skipped").ShouldBe(GreenKeyLog.Succeeded);
    GreenKeyLog.PipelineFor("full", "failure", "skipped").ShouldBe(GreenKeyLog.Failed);
    GreenKeyLog.PipelineFor(null, "success", null).ShouldBe(GreenKeyLog.Succeeded);
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

  public static async Task Workflow_skips_stages_and_republishes_carried_packages()
  {
    string workflow = File.ReadAllText(Path.Combine(RepoRoot(), ".github", "workflows", "workflow.yml"));
    workflow.ShouldContain("workflow --stage green-key\n");
    workflow.ShouldContain("workflow --stage green-key-skip\n");
    workflow.ShouldContain("workflow --stage green-key-finish\n");
    workflow.ShouldContain("retention-days: 90");
    workflow.ShouldContain("needs: [changes, stage, green-key]");
    workflow.ShouldContain("needs: [changes, green-key]");
    workflow.ShouldContain("!= 'skip'");
    workflow.ShouldNotContain("!= 'pack-only' || matrix.stage == 'build'");
    workflow.ShouldContain("== 'pack-only' && '[\"build\"]'");
    workflow.ShouldContain("'[\"build\",\"samples\",\"test-multi\",\"test-standalone\"]'");
    workflow.ShouldContain("GREEN_KEY_STARTED_TICKS=");
    workflow.ShouldContain("green-key-seconds=");
    workflow.ShouldContain("GREEN_KEY_CARRY_RUN:");
    workflow.ShouldContain("GREEN_KEY_RUN:");
    workflow.ShouldContain("GREEN_KEY_STAGE:");
    workflow.ShouldContain("GREEN_KEY_CARRY:");
    workflow.ShouldContain("name: Packages-${{ github.run_number }}");
    workflow.ShouldContain("steps.green-key-carry.outcome == 'success'");
    workflow.ShouldContain("!cancelled() &&");
    workflow.ShouldContain("steps.green-key-checkout.outcome == 'success'");
    workflow.ShouldContain("steps.green-key-setup.outcome == 'success'");
    workflow.ShouldContain("ci-finish-seconds=");
    workflow.ShouldContain("pipeline=key-skipped");

    int greenKeyAt = workflow.IndexOf("\n  green-key:\n", StringComparison.Ordinal);
    int stageAt = workflow.IndexOf("\n  stage:\n", StringComparison.Ordinal);
    int ciAt = workflow.IndexOf("\n  ci:\n", stageAt, StringComparison.Ordinal);
    greenKeyAt.ShouldBeGreaterThan(0);
    string greenKeyJob = workflow[greenKeyAt..stageAt];
    greenKeyJob.ShouldContain("continue-on-error: true");
    greenKeyJob.ShouldNotContain("head.repo");
    greenKeyJob.ShouldContain("github.event_name == 'push' &&");
    stageAt.ShouldBeGreaterThan(0);
    ciAt.ShouldBeGreaterThan(stageAt);
    string stageJob = workflow[stageAt..ciAt];
    stageJob.ShouldContain("needs.changes.outputs.code");
    stageJob.ShouldContain("green_key_run");
    stageJob.ShouldContain("fromJSON(");
    int stageIfAt = stageJob.IndexOf("if: >-", StringComparison.Ordinal);
    int stageRunsOnAt = stageJob.IndexOf("runs-on:", StringComparison.Ordinal);
    stageIfAt.ShouldBeGreaterThan(0);
    stageRunsOnAt.ShouldBeGreaterThan(stageIfAt);
    // The job if is evaluated before the matrix exists. matrix there is a
    // workflow-file error. Step ifs below runs-on may read matrix.stage.
    stageJob[stageIfAt..stageRunsOnAt].ShouldNotContain("matrix");
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
