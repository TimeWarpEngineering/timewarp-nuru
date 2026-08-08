#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// Task 458-003: the `checkVersionStrategy` key was removed from
/// `CheckVersionConfig` when the git-tag strategy was deleted. Repos with a
/// legacy `.timewarp/dev.jsonc` (e.g. timewarp-architecture) must keep parsing
/// without error — the unknown key is silently ignored by System.Text.Json's
/// default deserialize behavior — landing on the one remaining methodology
/// (nuget-search membership) with `Packages` preserved.
/// </summary>
[TestTag("DevCli")]
public class LegacyStrategyConfigTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<LegacyStrategyConfigTests>();

  public static async Task Legacy_git_tag_strategy_config_parses_without_throw()
  {
    const string json = """
    {
      // Per-repo configuration for TimeWarp.Nuru dev-cli
      "checkVersionConfig": {
        // checkVersionStrategy is no longer a recognized key — ignored on purpose
        "checkVersionStrategy": "git-tag",
        "packages": "TimeWarp.Nuru,TimeWarp.Nuru.Analyzers",
      },
    }
    """;

    RepoConfig config = Should.NotThrow(() => RepoConfigService.Parse(json));
    config.CheckVersionConfig.ShouldNotBeNull();
    config.CheckVersionConfig.Packages.ShouldBe("TimeWarp.Nuru,TimeWarp.Nuru.Analyzers");

    await Task.CompletedTask;
  }

  public static async Task Legacy_nuget_search_strategy_config_parses_without_throw()
  {
    const string json = """
    {
      "checkVersionConfig": {
        "checkVersionStrategy": "nuget-search",
        "packages": "TimeWarp.Nuru"
      }
    }
    """;

    RepoConfig config = Should.NotThrow(() => RepoConfigService.Parse(json));
    config.CheckVersionConfig.ShouldNotBeNull();
    config.CheckVersionConfig.Packages.ShouldBe("TimeWarp.Nuru");

    await Task.CompletedTask;
  }

  public static async Task Empty_object_returns_non_null_config_with_null_check_version_config()
  {
    RepoConfig config = RepoConfigService.Parse("{}");
    config.ShouldNotBeNull();
    config.CheckVersionConfig.ShouldBeNull();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
