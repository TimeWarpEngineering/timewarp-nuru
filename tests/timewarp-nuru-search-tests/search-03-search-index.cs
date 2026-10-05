#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Nuru.Search.Services;

// 470-008 end-to-end coverage of SearchIndex.SearchAsync against the real schema (in-memory
// SQLite via the internal data-source constructor, so ~/.nuru is never touched):
//   M11 — CliVersion is joined from clis.version onto every SearchResult.
//   M12 — a query with an embedded NUL neither throws nor crashes the search.
[TestTag("Search")]
public class SearchIndexTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SearchIndexTests>();

  private static async Task<SearchIndex> CreateIndexedCliAsync(string cliName = "mycli", string version = "1.2.3")
  {
    SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");

    EndpointCapability[] endpoints =
    [
      new EndpointCapability
      {
        Pattern = "deploy {env}",
        GroupPath = [],
        Description = "Deploy hello world",
        Kind = EndpointKind.Command,
        Parameters = [],
        Options = []
      },
      new EndpointCapability
      {
        Pattern = "status",
        GroupPath = ["index"],
        Description = "Show index status",
        Kind = EndpointKind.Query,
        Parameters = [],
        Options = []
      }
    ];

    await index.IndexCliAsync(cliName, version, "{}", endpoints);
    return index;
  }

  public static async Task Should_join_cli_version_onto_search_results()
  {
    await using SearchIndex index = await CreateIndexedCliAsync(version: "4.5.6");

    IReadOnlyList<SearchResult> results = await index.SearchAsync("deploy");

    results.Count.ShouldBe(1);
    results[0].CliName.ShouldBe("mycli");
    results[0].CliVersion.ShouldBe("4.5.6");
    results[0].Pattern.ShouldBe("deploy {env}");
  }

  public static async Task Should_reflect_reindexed_version()
  {
    await using SearchIndex index = await CreateIndexedCliAsync(version: "1.0.0");

    await index.IndexCliAsync("mycli", "2.0.0", "{}",
    [
      new EndpointCapability
      {
        Pattern = "deploy {env}",
        GroupPath = [],
        Kind = EndpointKind.Command,
        Parameters = [],
        Options = []
      }
    ]);

    IReadOnlyList<SearchResult> results = await index.SearchAsync("deploy");

    results.Count.ShouldBe(1);
    results[0].CliVersion.ShouldBe("2.0.0");
  }

  public static async Task Should_not_throw_for_query_with_embedded_nul()
  {
    await using SearchIndex index = await CreateIndexedCliAsync();

    // Before 470-008 this surfaced as SqliteException: fts5: syntax error / unterminated string.
    IReadOnlyList<SearchResult> results = await index.SearchAsync("hello\0world");

    // "hello" and "world" both prefix-match the deploy description.
    results.Count.ShouldBe(1);
    results[0].Pattern.ShouldBe("deploy {env}");
  }

  public static async Task Should_return_empty_for_control_only_query()
  {
    await using SearchIndex index = await CreateIndexedCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("\0\u0001\u001f");

    results.Count.ShouldBe(0);
  }

  public static async Task Should_not_throw_for_control_soup_query()
  {
    await using SearchIndex index = await CreateIndexedCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("\u0001(\0\"*\u007f");

    // Result count is irrelevant; the assertion is that no SqliteException escaped.
    results.ShouldNotBeNull();
  }

  public static async Task Should_store_endpoint_json_in_legacy_pascal_case_format()
  {
    // The endpoint_json column was previously written by reflection-based JsonSerializer
    // (PascalCase, enum via its string converter, unindented). The source-generated context must produce the
    // same shape so existing ~/.nuru/index.db rows remain readable after upgrade.
    EndpointCapability endpoint = new()
    {
      Pattern = "deploy {env}",
      GroupPath = ["ops"],
      Description = "Deploy",
      Kind = EndpointKind.Command,
      Parameters = [],
      Options = []
    };

    string json = System.Text.Json.JsonSerializer.Serialize(endpoint, SearchIndexJsonContext.Default.EndpointCapability);

    json.ShouldContain("\"Pattern\":\"deploy {env}\"");
    json.ShouldContain("\"Kind\":\"command\"");
    json.ShouldNotContain("\"pattern\"", Case.Sensitive);
    json.ShouldNotContain("\n");

    EndpointCapability? roundTrip = System.Text.Json.JsonSerializer.Deserialize(json, SearchIndexJsonContext.Default.EndpointCapability);
    roundTrip.ShouldNotBeNull();
    roundTrip.Pattern.ShouldBe("deploy {env}");
    roundTrip.Kind.ShouldBe(EndpointKind.Command);

    await Task.CompletedTask;
  }

  public static async Task Should_filter_by_group_path_and_keep_version()
  {
    await using SearchIndex index = await CreateIndexedCliAsync(version: "9.9.9");

    IReadOnlyList<SearchResult> results = await index.SearchAsync("status", groupPath: "index");

    results.Count.ShouldBe(1);
    results[0].GroupPath.ShouldBe("index");
    results[0].CliVersion.ShouldBe("9.9.9");
  }

  // 482-010 (S-2): --group accepts the dotted filter the capabilities filter documents and matches
  // whole group-path segments, not raw string prefixes.
  private static async Task<SearchIndex> CreateNestedGroupCliAsync()
  {
    SearchIndex index = new(NullLogger<SearchIndex>.Instance, ":memory:");

    EndpointCapability[] endpoints =
    [
      new EndpointCapability
      {
        Pattern = "docker remote add {name}",
        GroupPath = ["docker", "remote"],
        Description = "Add container remote",
        Kind = EndpointKind.Command,
        Parameters = [],
        Options = []
      },
      new EndpointCapability
      {
        Pattern = "docker remote auth login",
        GroupPath = ["docker", "remote", "auth"],
        Description = "Login container remote",
        Kind = EndpointKind.Command,
        Parameters = [],
        Options = []
      },
      new EndpointCapability
      {
        Pattern = "dockerx remote list",
        GroupPath = ["dockerx", "remote"],
        Description = "List container remote",
        Kind = EndpointKind.Query,
        Parameters = [],
        Options = []
      },
      new EndpointCapability
      {
        Pattern = "docker remotes list",
        GroupPath = ["docker", "remotes"],
        Description = "List container remote aliases",
        Kind = EndpointKind.Query,
        Parameters = [],
        Options = []
      }
    ];

    await index.IndexCliAsync("mycli", "1.0.0", "{}", endpoints);
    return index;
  }

  public static async Task Should_match_dotted_group_filter_and_children()
  {
    await using SearchIndex index = await CreateNestedGroupCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("container", groupPath: "docker.remote");

    results.Select(r => r.Pattern).Order().ToArray()
      .ShouldBe(["docker remote add {name}", "docker remote auth login"]);
  }

  public static async Task Should_match_space_separated_group_filter()
  {
    await using SearchIndex index = await CreateNestedGroupCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("container", groupPath: "docker remote");

    results.Select(r => r.Pattern).Order().ToArray()
      .ShouldBe(["docker remote add {name}", "docker remote auth login"]);
  }

  public static async Task Should_match_first_segment_group_filter()
  {
    await using SearchIndex index = await CreateNestedGroupCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("container", groupPath: "docker");

    results.Select(r => r.Pattern).Order().ToArray()
      .ShouldBe(["docker remote add {name}", "docker remote auth login", "docker remotes list"]);
  }

  public static async Task Should_not_match_group_sharing_only_a_string_prefix()
  {
    await using SearchIndex index = await CreateNestedGroupCliAsync();

    IReadOnlyList<SearchResult> results = await index.SearchAsync("list", groupPath: "docker.remote");

    results.Count.ShouldBe(0);
  }

  public static async Task Should_normalize_group_filter_forms()
  {
    SearchIndex.NormalizeGroupFilter("docker.remote").ShouldBe("docker remote");
    SearchIndex.NormalizeGroupFilter("docker remote").ShouldBe("docker remote");
    SearchIndex.NormalizeGroupFilter(" docker..remote. ").ShouldBe("docker remote");
    SearchIndex.NormalizeGroupFilter(null).ShouldBe(string.Empty);
    SearchIndex.NormalizeGroupFilter(" . ").ShouldBe(string.Empty);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
