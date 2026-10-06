#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-search/timewarp-nuru-search.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Search
{

using Microsoft.Extensions.Logging.Abstractions;
using TimeWarp.Amuru;
using TimeWarp.Nuru.Search.Services;

// 482-009 (S-1): `nuru search` wrote the human listing and then returned SearchResult[], which
// the generated invoker serialized and appended to stdout as a JSON array. The listing must be
// the only stdout for both a hit and a miss. The real TimeWarp.Nuru.Search assembly runs as a
// child process (its generated invoker lives in that assembly) against an index seeded under a
// temporary HOME, so ~/.nuru is never touched.
[TestTag("Search")]
public class SearchQueryOutputTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<SearchQueryOutputTests>();

  private static async Task<string> SeedHomeAsync()
  {
    string home = Path.Combine(Path.GetTempPath(), $"nuru-search-482-009-{Guid.NewGuid():N}");
    string indexPath = DatabasePath.EnsureIndexPath(Path.Combine(home, ".nuru"));

    await using SearchIndex index = new(NullLogger<SearchIndex>.Instance, indexPath);

    await index.IndexCliAsync("mycli", "1.2.3", "{}",
    [
      new EndpointCapability
      {
        Pattern = "deploy {env}",
        GroupPath = [],
        Description = "Deploy hello world",
        Kind = EndpointKind.Command,
        Parameters = [],
        Options = []
      }
    ]);

    return home;
  }

  private static async Task<string> RunSearchAsync(params string[] terms)
  {
    string home = await SeedHomeAsync();

    try
    {
      string searchDll = Path.Combine(AppContext.BaseDirectory, "TimeWarp.Nuru.Search.dll");

      CommandOutput output = await Shell.Builder("dotnet")
        .WithArguments([searchDll, "search", .. terms])
        .WithEnvironmentVariable("HOME", home)
        .WithEnvironmentVariable("USERPROFILE", home)
        .WithNoValidation()
        .CaptureAsync();

      output.Success.ShouldBeTrue($"nuru search exited {output.ExitCode}: {output.Stderr}");
      return output.Stdout;
    }
    finally
    {
      Directory.Delete(home, recursive: true);
    }
  }

  public static async Task Should_print_listing_without_json_array_for_hit()
  {
    string stdout = await RunSearchAsync("deploy");

    stdout.ShouldContain("Found 1 result(s):");
    stdout.ShouldContain("  deploy {env} [mycli]");
    stdout.TrimEnd().ShouldEndWith("    Deploy hello world");
    stdout.ShouldNotContain("[{");
  }

  public static async Task Should_print_only_no_results_line_for_miss()
  {
    string stdout = await RunSearchAsync("nonexistentzzz");

    stdout.TrimEnd().ShouldBe("No results found.");
    stdout.ShouldNotContain("[]");
  }
}

} // namespace TimeWarp.Nuru.Tests.Search
