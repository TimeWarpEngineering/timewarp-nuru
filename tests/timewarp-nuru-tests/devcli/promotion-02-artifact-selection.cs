#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Packages-* artifact selection matrix for CiRunPromotion.SelectPackagesArtifact
/// (kanban task 458-002, Design B — build-once/promote). workflow.yml uploads
/// the nupkg set as `Packages-{run_number}`; artifacts expire per repo
/// retention and the REST API keeps listing an expired one (expired=true, not
/// downloadable) rather than omitting it, so Expired must be distinguished
/// from no match at all (NoneMatching).
/// </summary>
[TestTag("DevCli")]
public class ArtifactSelectionTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ArtifactSelectionTests>();

  public static async Task Single_live_match_is_found()
  {
    RunArtifact artifact = new() { Id = 1, Name = "Packages-42", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([artifact]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Found);
    outcome.Artifact.ShouldBe(artifact);
    outcome.ExpiredNames.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task No_matching_name_yields_none_matching()
  {
    RunArtifact unrelated = new() { Id = 1, Name = "coverage-report", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([unrelated]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.NoneMatching);
    outcome.Artifact.ShouldBeNull();
    outcome.ExpiredNames.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Empty_artifact_list_yields_none_matching()
  {
    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.NoneMatching);
    outcome.Artifact.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Only_expired_match_yields_expired_with_names()
  {
    RunArtifact expired = new() { Id = 1, Name = "Packages-40", Expired = true };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([expired]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Expired);
    outcome.Artifact.ShouldBeNull();
    outcome.ExpiredNames.ShouldBe(["Packages-40"]);

    await Task.CompletedTask;
  }

  public static async Task Mixed_expired_and_live_prefers_the_live_one()
  {
    RunArtifact expired = new() { Id = 1, Name = "Packages-40", Expired = true };
    RunArtifact live = new() { Id = 2, Name = "Packages-41", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([expired, live]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Found);
    outcome.Artifact.ShouldBe(live);
    outcome.ExpiredNames.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Multiple_live_matches_pick_highest_id()
  {
    RunArtifact lower = new() { Id = 5, Name = "Packages-a", Expired = false };
    RunArtifact higher = new() { Id = 9, Name = "Packages-b", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([lower, higher]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Found);
    outcome.Artifact.ShouldBe(higher);

    await Task.CompletedTask;
  }

  public static async Task Non_matching_names_are_ignored_even_when_mixed_with_a_live_match()
  {
    RunArtifact unrelated = new() { Id = 1, Name = "coverage-report", Expired = false };
    RunArtifact live = new() { Id = 2, Name = "Packages-42", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([unrelated, live]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.Found);
    outcome.Artifact.ShouldBe(live);

    await Task.CompletedTask;
  }

  public static async Task Name_prefix_match_is_case_sensitive_ordinal()
  {
    RunArtifact lowercasePrefix = new() { Id = 1, Name = "packages-42", Expired = false };

    PackagesArtifactOutcome outcome = CiRunPromotion.SelectPackagesArtifact([lowercasePrefix]);

    outcome.Status.ShouldBe(PackagesArtifactStatus.NoneMatching);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
