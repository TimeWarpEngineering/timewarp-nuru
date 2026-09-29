#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Honest prerelease-increment distance for check-version (kanban task 456).
/// Distance is only defined when major.minor.patch and the prerelease label
/// match and the numeric identifier differs. The warning fires above 1; the
/// count is the intermediates (distance minus the source version).
/// </summary>
[TestTag("DevCli")]
public class PrereleaseDistanceTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PrereleaseDistanceTests>();

  public static async Task Distance_zero_has_no_warning_and_is_not_a_strict_failure()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.9", "2.0.0-beta.9").ShouldBe(0);
    PrereleaseDistance.FormatSkippedReleaseWarning(0).ShouldBeNull();
    PrereleaseDistance.IsStrictFailure(0).ShouldBeFalse();
    PrereleaseDistance.FormatDistanceLine(0, "2.0.0-beta.9")
      .ShouldBe("Source is 0 prerelease increments ahead of v2.0.0-beta.9");

    await Task.CompletedTask;
  }

  public static async Task Distance_one_is_a_normal_bump_without_a_warning()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.10", "2.0.0-beta.9").ShouldBe(1);
    PrereleaseDistance.FormatSkippedReleaseWarning(1).ShouldBeNull();
    PrereleaseDistance.IsStrictFailure(1).ShouldBeFalse();
    PrereleaseDistance.FormatDistanceLine(1, "2.0.0-beta.9")
      .ShouldBe("Source is 1 prerelease increment ahead of v2.0.0-beta.9");

    await Task.CompletedTask;
  }

  public static async Task Distance_above_one_warns_about_the_unreleased_intermediates()
  {
    // Incident shape: beta.9 published, beta.14 in source. Five increments,
    // four intermediate numbers never published.
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.14", "2.0.0-beta.9").ShouldBe(5);
    PrereleaseDistance.IsStrictFailure(5).ShouldBeTrue();
    PrereleaseDistance.FormatDistanceLine(5, "2.0.0-beta.9")
      .ShouldBe("Source is 5 prerelease increments ahead of v2.0.0-beta.9");
    PrereleaseDistance.FormatSkippedReleaseWarning(5)
      .ShouldBe("4 version(s) were bumped but never released — was a release step skipped?");

    await Task.CompletedTask;
  }

  public static async Task Two_ahead_warns_about_one_skipped_version()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.11", "2.0.0-beta.9").ShouldBe(2);
    PrereleaseDistance.FormatSkippedReleaseWarning(2)
      .ShouldBe("1 version(s) were bumped but never released — was a release step skipped?");

    await Task.CompletedTask;
  }

  public static async Task Mismatched_core_skips_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("3.0.0-beta.1", "2.0.0-beta.9").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.1.0-beta.1", "2.0.0-beta.9").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.0.1-beta.1", "2.0.0-beta.9").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Different_label_skips_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-rc.14", "2.0.0-beta.9").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Non_label_number_shapes_skip_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0", "2.0.0-beta.9").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.0.0-beta", "2.0.0-beta.1").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.1.2", "2.0.0-beta.1.1").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("1.2.0", "1.2.0").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task No_prior_release_skips_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.1", null).ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.1", "").ShouldBeNull();
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.1", "   ").ShouldBeNull();
    PrereleaseDistance.IsStrictFailure(null).ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Source_behind_latest_is_a_negative_distance_without_a_warning()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.9", "2.0.0-beta.14").ShouldBe(-5);
    PrereleaseDistance.IsStrictFailure(-5).ShouldBeFalse();
    PrereleaseDistance.FormatSkippedReleaseWarning(-5).ShouldBeNull();
    PrereleaseDistance.FormatDistanceLine(-5, "2.0.0-beta.14")
      .ShouldBe("Source is 5 prerelease increments behind v2.0.0-beta.14");

    await Task.CompletedTask;
  }

  public static async Task Build_metadata_and_label_case_do_not_change_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.14+sha", "2.0.0-beta.9+build").ShouldBe(5);
    PrereleaseDistance.TryGetIncrements("2.0.0-BETA.14", "2.0.0-beta.9").ShouldBe(5);
    PrereleaseDistance.FormatDistanceLine(5, "2.0.0-beta.9+build")
      .ShouldBe("Source is 5 prerelease increments ahead of v2.0.0-beta.9");

    await Task.CompletedTask;
  }

  public static async Task Leading_zeros_compare_as_the_same_number()
  {
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.007", "2.0.0-beta.7").ShouldBe(0);
    PrereleaseDistance.TryGetIncrements("2.0.0-beta.014", "2.0.0-beta.9").ShouldBe(5);

    await Task.CompletedTask;
  }

  public static async Task Core_normalization_allows_a_missing_patch()
  {
    PrereleaseDistance.TryGetIncrements("2.0-beta.3", "2.0.0-beta.1").ShouldBe(2);

    await Task.CompletedTask;
  }

  public static async Task Huge_numeric_identifier_skips_the_distance()
  {
    PrereleaseDistance.TryGetIncrements("1.0.0-beta.99999999999999999999", "1.0.0-beta.1").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Null_source_throws()
  {
    Should.Throw<ArgumentNullException>(() => PrereleaseDistance.TryGetIncrements(null!, "1.0.0-beta.1"));

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
