#!/usr/bin/dotnet --
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
    result.ShouldBe("documentation-reference-foo");

    await Task.CompletedTask;
  }

  public static async Task Should_handle_top_level_filename_without_directory()
  {
    string result = GitHubCacheService.GetSafeCacheFileName("foo.md");

    result.ShouldBe("foo");

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
