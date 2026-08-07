#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Candidate-run ordering matrix for CiRunPromotion.OrderCandidateRuns
/// (kanban task 458-002, Design B — build-once/promote). Fixture values for
/// the push-preferred-at-same-sha case are real, live-verified data from
/// `gh run list --workflow workflow.yml --commit b2eea2c9... --status
/// success --json databaseId,event,headSha,createdAt` against this repo: a
/// push run (27553073284) and a later release run (27553776617) both exist
/// at the same commit — the push run must still win the ordering, even
/// though its createdAt is EARLIER, because push-event runs ran the full
/// pr pipeline (build -> verify-samples -> test) against this exact commit,
/// while the release-event run only re-ran the same pipeline.
/// </summary>
[TestTag("DevCli")]
public class RunSelectionOrderingTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<RunSelectionOrderingTests>();

  private const string HeadSha = "b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1";

  private static readonly CiRunSummary PushRun = new()
  {
    DatabaseId = 27553073284,
    Event = "push",
    HeadSha = HeadSha,
    CreatedAt = "2026-06-15T14:23:42Z"
  };

  private static readonly CiRunSummary ReleaseRun = new()
  {
    DatabaseId = 27553776617,
    Event = "release",
    HeadSha = HeadSha,
    CreatedAt = "2026-06-15T14:34:37Z"
  };

  public static async Task Push_preferred_over_release_at_same_sha_despite_earlier_createdAt()
  {
    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([ReleaseRun, PushRun], HeadSha);

    ordered.Count.ShouldBe(2);
    ordered[0].DatabaseId.ShouldBe(PushRun.DatabaseId);
    ordered[1].DatabaseId.ShouldBe(ReleaseRun.DatabaseId);

    await Task.CompletedTask;
  }

  public static async Task Push_preferred_regardless_of_input_order()
  {
    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([PushRun, ReleaseRun], HeadSha);

    ordered[0].DatabaseId.ShouldBe(PushRun.DatabaseId);
    ordered[1].DatabaseId.ShouldBe(ReleaseRun.DatabaseId);

    await Task.CompletedTask;
  }

  public static async Task Newest_first_within_same_event()
  {
    CiRunSummary older = new() { DatabaseId = 100, Event = "push", HeadSha = HeadSha, CreatedAt = "2026-06-15T10:00:00Z" };
    CiRunSummary newer = new() { DatabaseId = 200, Event = "push", HeadSha = HeadSha, CreatedAt = "2026-06-15T11:00:00Z" };

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([older, newer], HeadSha);

    ordered[0].DatabaseId.ShouldBe(newer.DatabaseId);
    ordered[1].DatabaseId.ShouldBe(older.DatabaseId);

    await Task.CompletedTask;
  }

  public static async Task Higher_database_id_wins_tie_on_createdAt()
  {
    CiRunSummary lower = new() { DatabaseId = 100, Event = "push", HeadSha = HeadSha, CreatedAt = "2026-06-15T10:00:00Z" };
    CiRunSummary higher = new() { DatabaseId = 200, Event = "push", HeadSha = HeadSha, CreatedAt = "2026-06-15T10:00:00Z" };

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([lower, higher], HeadSha);

    ordered[0].DatabaseId.ShouldBe(higher.DatabaseId);
    ordered[1].DatabaseId.ShouldBe(lower.DatabaseId);

    await Task.CompletedTask;
  }

  public static async Task Runs_at_a_foreign_sha_are_filtered_out()
  {
    CiRunSummary foreign = new() { DatabaseId = 999, Event = "push", HeadSha = "0000000000000000000000000000000000000f", CreatedAt = PushRun.CreatedAt };

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([PushRun, foreign], HeadSha);

    ordered.Count.ShouldBe(1);
    ordered[0].DatabaseId.ShouldBe(PushRun.DatabaseId);

    await Task.CompletedTask;
  }

  public static async Task Empty_run_list_yields_empty_result()
  {
    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([], HeadSha);
    ordered.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task No_runs_at_the_target_sha_yields_empty_result()
  {
    CiRunSummary foreign = new() { DatabaseId = PushRun.DatabaseId, Event = PushRun.Event, HeadSha = "0000000000000000000000000000000000000f", CreatedAt = PushRun.CreatedAt };

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([foreign], HeadSha);
    ordered.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Sha_comparison_is_case_sensitive_ordinal()
  {
    CiRunSummary upperCaseSha = new() { DatabaseId = PushRun.DatabaseId, Event = PushRun.Event, HeadSha = HeadSha.ToUpperInvariant(), CreatedAt = PushRun.CreatedAt };

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns([upperCaseSha], HeadSha);
    ordered.ShouldBeEmpty();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
