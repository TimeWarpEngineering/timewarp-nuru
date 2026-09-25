#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using TimeWarp.Nuru.Search.Endpoints;
using TimeWarp.Nuru.Search.Services;

// 470-008 (M11): `nuru search --version` is documented as "Show CLI version in results" but
// used to print result.Endpoint.Kind. The header formatter must print clis.version instead.
[TestTag("Search")]
public class SearchVersionFormatterTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SearchVersionFormatterTests>();

  private static SearchResult CreateResult(string? cliVersion, string groupPath = "", EndpointKind kind = EndpointKind.Command) =>
    new()
    {
      CliName = "mycli",
      CliVersion = cliVersion,
      Pattern = "deploy {env}",
      Description = "Deploy the thing",
      GroupPath = groupPath,
      Endpoint = new EndpointCapability
      {
        Pattern = "deploy {env}",
        GroupPath = [],
        Kind = kind,
        Parameters = [],
        Options = []
      }
    };

  public static async Task Should_print_cli_name_only_without_version_flag()
  {
    string line = SearchQuery.Handler.FormatResultHeader(CreateResult("1.2.3"), showVersion: false);

    line.ShouldBe("  deploy {env} [mycli]");

    await Task.CompletedTask;
  }

  public static async Task Should_print_cli_version_with_version_flag()
  {
    string line = SearchQuery.Handler.FormatResultHeader(CreateResult("1.2.3"), showVersion: true);

    line.ShouldBe("  deploy {env} [mycli@1.2.3]");

    await Task.CompletedTask;
  }

  public static async Task Should_not_print_endpoint_kind_as_version()
  {
    // Regression for M11: Kind must never leak into the version slot.
    string line = SearchQuery.Handler.FormatResultHeader(CreateResult("1.2.3", kind: EndpointKind.Query), showVersion: true);

    line.ShouldNotContain("Query");
    line.ShouldNotContain("Command");
    line.ShouldContain("@1.2.3");

    await Task.CompletedTask;
  }

  public static async Task Should_print_unknown_when_version_is_missing()
  {
    string line = SearchQuery.Handler.FormatResultHeader(CreateResult(cliVersion: null), showVersion: true);

    line.ShouldBe("  deploy {env} [mycli@unknown]");

    await Task.CompletedTask;
  }

  public static async Task Should_prefix_group_path_to_pattern()
  {
    string line = SearchQuery.Handler.FormatResultHeader(CreateResult("2.0.0", groupPath: "index"), showVersion: true);

    line.ShouldBe("  index deploy {env} [mycli@2.0.0]");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
