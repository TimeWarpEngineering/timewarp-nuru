#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Downloaded-artifact package-set verification matrix for
/// CiRunPromotion.VerifyPackageSet (kanban task 458-002, Design B —
/// build-once/promote). Mirrors the release push step's pre-existing
/// cross-check (workflow-command.cs PushPackagesAsync), but runs against a
/// downloaded CI artifact's contents instead of a fresh `dotnet pack` output.
/// Expected name per project is exactly "{PackageId}.{version}.nupkg"
/// (Ordinal); Unexpected catches ANY other .nupkg regardless of version —
/// including the SAME PackageId at a stale version, which is how a CI run
/// that predates a version bump gets caught.
/// </summary>
[TestTag("DevCli")]
public class PackageSetVerificationTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PackageSetVerificationTests>();

  private const string Version = "3.0.0-beta.72";

  private static readonly IReadOnlyList<PackableProject> FiveProjects =
  [
    new PackableProject("source/a/a.csproj", "TimeWarp.Nuru"),
    new PackableProject("source/b/b.csproj", "TimeWarp.Nuru.Analyzers"),
    new PackableProject("source/c/c.csproj", "TimeWarp.Nuru.Core"),
    new PackableProject("source/d/d.csproj", "TimeWarp.Nuru.DevCli"),
    new PackableProject("source/e/e.csproj", "TimeWarp.Nuru.Search")
  ];

  private static IReadOnlyList<string> ExpectedFileNamesFor(IReadOnlyList<PackableProject> projects, string version) =>
    [.. projects.Select(p => $"{p.PackageId}.{version}.nupkg")];

  public static async Task Exact_match_of_all_five_packages_is_a_match()
  {
    IReadOnlyList<string> actual = ExpectedFileNamesFor(FiveProjects, Version);

    PackageSetVerification result = CiRunPromotion.VerifyPackageSet(actual, FiveProjects, Version);

    result.IsMatch.ShouldBeTrue();
    result.Missing.ShouldBeEmpty();
    result.Unexpected.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task One_missing_package_is_reported()
  {
    List<string> actual = [.. ExpectedFileNamesFor(FiveProjects, Version)];
    actual.RemoveAt(2); // drop TimeWarp.Nuru.Core

    PackageSetVerification result = CiRunPromotion.VerifyPackageSet(actual, FiveProjects, Version);

    result.IsMatch.ShouldBeFalse();
    result.Missing.ShouldBe(["TimeWarp.Nuru.Core.3.0.0-beta.72.nupkg"]);
    result.Unexpected.ShouldBeEmpty();

    await Task.CompletedTask;
  }

  public static async Task Extra_package_at_the_expected_version_is_unexpected()
  {
    List<string> actual = [.. ExpectedFileNamesFor(FiveProjects, Version), "TimeWarp.Nuru.Extra.3.0.0-beta.72.nupkg"];

    PackageSetVerification result = CiRunPromotion.VerifyPackageSet(actual, FiveProjects, Version);

    result.IsMatch.ShouldBeFalse();
    result.Missing.ShouldBeEmpty();
    result.Unexpected.ShouldBe(["TimeWarp.Nuru.Extra.3.0.0-beta.72.nupkg"]);

    await Task.CompletedTask;
  }

  public static async Task Same_package_at_a_different_version_is_unexpected_not_a_substitute_match()
  {
    // Simulates a CI run whose artifact predates the version bump: the
    // artifact has the right PackageId but the wrong version — must show up
    // as BOTH missing (expected version absent) and unexpected (stale
    // version present), not silently accepted as a match.
    List<string> actual = [.. ExpectedFileNamesFor(FiveProjects, Version)];
    actual[0] = "TimeWarp.Nuru.3.0.0-beta.71.nupkg";

    PackageSetVerification result = CiRunPromotion.VerifyPackageSet(actual, FiveProjects, Version);

    result.IsMatch.ShouldBeFalse();
    result.Missing.ShouldBe(["TimeWarp.Nuru.3.0.0-beta.72.nupkg"]);
    result.Unexpected.ShouldBe(["TimeWarp.Nuru.3.0.0-beta.71.nupkg"]);

    await Task.CompletedTask;
  }

  public static async Task Empty_actual_set_reports_every_project_as_missing()
  {
    PackageSetVerification result = CiRunPromotion.VerifyPackageSet([], FiveProjects, Version);

    result.IsMatch.ShouldBeFalse();
    result.Missing.ShouldBe(ExpectedFileNamesFor(FiveProjects, Version));
    result.Unexpected.ShouldBeEmpty();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
