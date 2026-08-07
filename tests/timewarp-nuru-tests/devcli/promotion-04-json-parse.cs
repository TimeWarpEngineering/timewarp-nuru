#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text.Json;
using global::DevCli;

/// <summary>
/// Parses REAL gh CLI/REST payload shapes through the AOT source-generated
/// DevCliJsonContext (kanban task 458-002, Design B — build-once/promote).
/// Both payloads below are verbatim from live commands run against this repo:
/// <c>gh run list --workflow workflow.yml --commit
/// b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1 --status success --json
/// databaseId,event,headSha,createdAt</c> and <c>gh api
/// repos/{owner}/{repo}/actions/runs/27553073284/artifacts</c>. The REST
/// artifacts payload carries many fields RunArtifact does not declare
/// (node_id, size_in_bytes, url, digest, workflow_run, ...) — System.Text.Json
/// ignores unmapped members by default, so this also proves the source-gen
/// context tolerates the full real response shape, not just a hand-trimmed
/// fixture.
/// </summary>
[TestTag("DevCli")]
public class PromotionJsonParseTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PromotionJsonParseTests>();

  private const string RunListJson =
    """
    [{"createdAt":"2026-06-15T14:34:37Z","databaseId":27553776617,"event":"release","headSha":"b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1"},{"createdAt":"2026-06-15T14:23:42Z","databaseId":27553073284,"event":"push","headSha":"b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1"}]
    """;

  private const string RunArtifactsJson =
    """
    {"total_count":1,"artifacts":[{"id":7641180843,"node_id":"MDg6QXJ0aWZhY3Q3NjQxMTgwODQz","name":"Packages-42","size_in_bytes":21883755,"url":"https://api.github.com/repos/TimeWarpEngineering/timewarp-nuru/actions/artifacts/7641180843","archive_download_url":"https://api.github.com/repos/TimeWarpEngineering/timewarp-nuru/actions/artifacts/7641180843/zip","expired":false,"digest":"sha256:99b15332fc86ac6dea27dfa9b1032d9e873285f655c3df3514d1a59169c69c59","created_at":"2026-06-15T14:30:42Z","updated_at":"2026-06-15T14:30:42Z","expires_at":"2026-09-13T14:23:43Z","workflow_run":{"id":27553073284,"repository_id":1024635367,"head_repository_id":1024635367,"head_branch":"master","head_sha":"b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1"}}]}
    """;

  public static async Task Run_list_array_parses_into_two_summaries()
  {
    List<CiRunSummary>? runs = JsonSerializer.Deserialize(RunListJson, DevCliJsonContext.Default.ListCiRunSummary);

    runs.ShouldNotBeNull();
    runs.Count.ShouldBe(2);

    CiRunSummary release = runs[0];
    release.DatabaseId.ShouldBe(27553776617);
    release.Event.ShouldBe("release");
    release.HeadSha.ShouldBe("b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1");
    release.CreatedAt.ShouldBe("2026-06-15T14:34:37Z");

    CiRunSummary push = runs[1];
    push.DatabaseId.ShouldBe(27553073284);
    push.Event.ShouldBe("push");
    push.HeadSha.ShouldBe("b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1");
    push.CreatedAt.ShouldBe("2026-06-15T14:23:42Z");

    await Task.CompletedTask;
  }

  public static async Task Run_list_parse_feeds_ordering_correctly()
  {
    List<CiRunSummary>? runs = JsonSerializer.Deserialize(RunListJson, DevCliJsonContext.Default.ListCiRunSummary);
    runs.ShouldNotBeNull();

    IReadOnlyList<CiRunSummary> ordered = CiRunPromotion.OrderCandidateRuns(runs, "b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1");

    ordered.Count.ShouldBe(2);
    ordered[0].Event.ShouldBe("push");
    ordered[0].DatabaseId.ShouldBe(27553073284);

    await Task.CompletedTask;
  }

  public static async Task Run_artifacts_response_parses_ignoring_unmapped_fields()
  {
    RunArtifactListResponse? response = JsonSerializer.Deserialize(RunArtifactsJson, DevCliJsonContext.Default.RunArtifactListResponse);

    response.ShouldNotBeNull();
    response.Artifacts.Count.ShouldBe(1);

    RunArtifact artifact = response.Artifacts[0];
    artifact.Id.ShouldBe(7641180843);
    artifact.Name.ShouldBe("Packages-42");
    artifact.Expired.ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Run_artifacts_parse_feeds_selection_correctly()
  {
    RunArtifactListResponse? response = JsonSerializer.Deserialize(RunArtifactsJson, DevCliJsonContext.Default.RunArtifactListResponse);
    response.ShouldNotBeNull();

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact(response.Artifacts);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Found);
    outcome.Artifact.ShouldNotBeNull();
    outcome.Artifact.Name.ShouldBe("Packages-42");

    await Task.CompletedTask;
  }

  public static async Task Empty_run_list_array_parses_to_empty()
  {
    List<CiRunSummary>? runs = JsonSerializer.Deserialize("[]", DevCliJsonContext.Default.ListCiRunSummary);

    runs.ShouldNotBeNull();
    runs.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Empty_artifacts_array_parses_to_empty()
  {
    RunArtifactListResponse? response = JsonSerializer.Deserialize("""{"total_count":0,"artifacts":[]}""", DevCliJsonContext.Default.RunArtifactListResponse);

    response.ShouldNotBeNull();
    response.Artifacts.ShouldBeEmpty();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
