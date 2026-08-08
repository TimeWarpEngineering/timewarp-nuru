#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Mcp
{

using TimeWarp.Nuru.Mcp.Services;

[TestTag("MCP")]
public class CacheFileNameTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<CacheFileNameTests>();

  public static async Task Should_not_collide_for_same_filename_in_different_directories()
  {
    string first = GitHubCacheService.GetSafeCacheFileName("examples/routing/foo.md");
    string second = GitHubCacheService.GetSafeCacheFileName("examples/parser/foo.md");

    first.ShouldNotBe(second);
    first.ShouldContain("foo");
    second.ShouldContain("foo");

    await Task.CompletedTask;
  }

  public static async Task Should_replace_path_separators_with_dashes()
  {
    string result = GitHubCacheService.GetSafeCacheFileName("documentation/reference/foo.md");

    result.ShouldNotContain('/');
    result.ShouldNotContain('\\');
    result.ShouldBe("documentation-reference-foo.md");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_top_level_filename_without_directory()
  {
    string result = GitHubCacheService.GetSafeCacheFileName("foo.md");

    result.ShouldBe("foo.md");

    await Task.CompletedTask;
  }

  public static async Task Should_not_collide_for_same_stem_with_different_extensions()
  {
    // Same directory, same stem, different extension must produce distinct cache
    // names — the extension is kept for exactly this reason.
    string markdown = GitHubCacheService.GetSafeCacheFileName("docs/foo.md");
    string json = GitHubCacheService.GetSafeCacheFileName("docs/foo.json");

    markdown.ShouldNotBe(json);

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
