#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// `dev release` marks the GitHub Release as prerelease exactly when the props
/// version has a SemVer prerelease suffix, mirroring NuGet (kanban task 486).
/// The printed command (dry-run / recovery) and the executed gh arguments come
/// from the same argument list, so they cannot drift.
/// </summary>
[TestTag("DevCli")]
public class ReleasePrereleaseFlagTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<ReleasePrereleaseFlagTests>();

  public static async Task Beta_version_is_prerelease()
  {
    ReleaseGuard.IsPrerelease("3.0.0-beta.79").ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Stable_version_is_not_prerelease()
  {
    ReleaseGuard.IsPrerelease("3.0.0").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Build_metadata_hyphen_is_not_prerelease()
  {
    ReleaseGuard.IsPrerelease("3.0.0+build-7").ShouldBeFalse();
    await Task.CompletedTask;
  }

  public static async Task Prerelease_with_build_metadata_is_prerelease()
  {
    ReleaseGuard.IsPrerelease("3.0.0-rc.1+sha.abc").ShouldBeTrue();
    await Task.CompletedTask;
  }

  public static async Task Beta_version_arguments_include_prerelease()
  {
    IReadOnlyList<string> arguments = ReleaseGuard.BuildReleaseCreateArguments("v3.0.0-beta.79", "3.0.0-beta.79");

    arguments.ShouldContain("--prerelease");
    ReleaseGuard.FormatReleaseCreateCommand(arguments)
      .ShouldBe("gh release create v3.0.0-beta.79 --title v3.0.0-beta.79 --generate-notes --verify-tag --prerelease");

    await Task.CompletedTask;
  }

  public static async Task Stable_version_arguments_exclude_prerelease()
  {
    IReadOnlyList<string> arguments = ReleaseGuard.BuildReleaseCreateArguments("v3.0.0", "3.0.0");

    arguments.ShouldNotContain("--prerelease");
    ReleaseGuard.FormatReleaseCreateCommand(arguments)
      .ShouldBe("gh release create v3.0.0 --title v3.0.0 --generate-notes --verify-tag");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
