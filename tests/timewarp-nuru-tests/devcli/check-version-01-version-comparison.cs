#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// SemVer 2.0 §11 pre-release comparison + published-version membership.
/// Task 454-022: verify the release gate ranks beta.9 < beta.10 and checks
/// the FULL published-versions list, not just versions[^1].
/// </summary>
[TestTag("DevCli")]
public class VersionComparisonTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<VersionComparisonTests>();

  // --- CompareVersions: SemVer §11 precedence ---

  public static async Task Release_greater_than_prerelease()
  {
    NuGetVersionService.CompareVersions("1.0.0-beta", "1.0.0").ShouldBeLessThan(0);
    await Task.CompletedTask;
  }

  public static async Task Numeric_prerelease_identifiers_compare_numerically()
  {
    // The bug that started this task: beta.9 must NOT rank above beta.10.
    NuGetVersionService.CompareVersions("1.0.0-beta.2", "1.0.0-beta.10").ShouldBeLessThan(0);
    NuGetVersionService.CompareVersions("1.0.0-beta.10", "1.0.0-beta.2").ShouldBeGreaterThan(0);
    await Task.CompletedTask;
  }

  public static async Task Alpha_less_than_beta()
  {
    NuGetVersionService.CompareVersions("1.0.0-alpha", "1.0.0-beta").ShouldBeLessThan(0);
    await Task.CompletedTask;
  }

  public static async Task Shorter_prerelease_less_than_longer_prefix()
  {
    NuGetVersionService.CompareVersions("1.0.0-beta", "1.0.0-beta.1").ShouldBeLessThan(0);
    await Task.CompletedTask;
  }

  public static async Task Build_metadata_ignored()
  {
    NuGetVersionService.CompareVersions("1.2.0+build", "1.2.0").ShouldBe(0);
    await Task.CompletedTask;
  }

  public static async Task Case_insensitive_prerelease()
  {
    NuGetVersionService.CompareVersions("1.0.0-BETA", "1.0.0-beta").ShouldBe(0);
    await Task.CompletedTask;
  }

  public static async Task Missing_parts_normalized_to_zero()
  {
    // NuGet normalization: 1.0 == 1.0.0 == 1.0.0.0
    NuGetVersionService.CompareVersions("1.0", "1.0.0").ShouldBe(0);
    NuGetVersionService.CompareVersions("1.0.0", "1.0.0.0").ShouldBe(0);
    await Task.CompletedTask;
  }

  // --- IsVersionPublished: full-list membership ---

  public static async Task Published_returns_true_when_version_in_list()
  {
    NuGetVersionService.IsVersionPublished("1.2.0", ["1.2.0", "1.3.0"]).ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Published_returns_false_when_version_not_in_list()
  {
    NuGetVersionService.IsVersionPublished("1.2.0", ["1.3.0"]).ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Published_matches_prerelease_against_full_list()
  {
    // The membership bug: versions[^1]-only check would have missed beta.70
    // when beta.71 was latest. Full-list check must find beta.71.
    NuGetVersionService
      .IsVersionPublished("1.0.0-beta.71", ["1.0.0-beta.70", "1.0.0-beta.71"])
      .ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Huge_numeric_identifiers_do_not_overflow()
  {
    // Date-stamped pre-release identifiers exceed Int32/Int64 range; the gate
    // iterates every published version, so this must compare, not crash.
    NuGetVersionService.CompareVersions("1.0.0-ci.20260707191500", "1.0.0-ci.20260707191501")
      .ShouldBeLessThan(0);
    NuGetVersionService.CompareVersions("1.0.0-ci.99999999999999999999", "1.0.0-ci.9")
      .ShouldBeGreaterThan(0);

    await Task.CompletedTask;
  }

  public static async Task Malformed_empty_prerelease_does_not_crash()
  {
    // "1.0.0-" is malformed SemVer but present-in-the-wild garbage must not
    // crash the gate; empty identifier is tolerated (treated as lowest).
    Should.NotThrow(() => NuGetVersionService.CompareVersions("1.0.0-", "1.0.0-beta"));
    Should.NotThrow(() => NuGetVersionService.IsVersionPublished("1.0.0", ["1.0.0-", "1.0.0"]));
    NuGetVersionService.IsVersionPublished("1.0.0", ["1.0.0-", "1.0.0"]).ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Leading_zeros_compare_numerically()
  {
    // §11.4.1 numeric comparison: 007 == 7 numerically.
    NuGetVersionService.CompareVersions("1.0.0-beta.007", "1.0.0-beta.7").ShouldBe(0);

    await Task.CompletedTask;
  }

  public static async Task Published_ignores_build_metadata()
  {
    NuGetVersionService.IsVersionPublished("1.2.0+build", ["1.2.0"]).ShouldBeTrue();
    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
