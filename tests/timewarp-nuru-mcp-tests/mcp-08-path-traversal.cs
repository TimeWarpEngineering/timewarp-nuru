#!/usr/bin/env -S dotnet --
#:project $(SourceDirectory)timewarp-nuru-mcp/timewarp-nuru-mcp.csproj

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.Mcp
{

using TimeWarp.Nuru.Mcp.Services;

[TestTag("MCP")]
public class PathTraversalGuardTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<PathTraversalGuardTests>();

  public static async Task Should_reject_dotdot_path_that_escapes_repo()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "../../../evil-org/evil-repo/main/secret",
        out Uri? uri,
        "samples/");

    ok.ShouldBeFalse();
    uri.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_percent_encoded_dotdot_leaving_allowlist()
  {
    // Literal ".." check misses %2e%2e; Uri would normalize out of samples/.
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "samples/%2E%2E/x",
        out Uri? uri,
        "samples/");

    ok.ShouldBeFalse();
    uri.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_path_with_uri_scheme()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "https://evil.com/secret",
        out Uri? uri,
        "samples/");

    ok.ShouldBeFalse();
    uri.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_absolute_rooted_path()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "/etc/passwd",
        out Uri? uri,
        "samples/");

    ok.ShouldBeFalse();
    uri.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_reject_path_outside_required_prefix()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "documentation/user/features/endpoints.md",
        out Uri? uri,
        "samples/");

    ok.ShouldBeFalse();
    uri.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Should_accept_samples_path_under_repo_uri()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "samples/fluent/01-hello-world/fluent-hello-world-lambda.cs",
        out Uri? uri,
        "samples/");

    ok.ShouldBeTrue();
    uri.ShouldNotBeNull();
    uri!.Host.ShouldBe(GitHubCacheService.AllowedRawHost);
    uri.AbsolutePath.ShouldStartWith(GitHubCacheService.AllowedRepoPathPrefix);
    uri.AbsoluteUri.ShouldStartWith(GitHubCacheService.GitHubRawBaseUrl);

    await Task.CompletedTask;
  }

  public static async Task Should_accept_documentation_path_with_default_allowlist()
  {
    bool ok = GitHubCacheService.TryResolveRawContentUri(
        "documentation/user/features/endpoints.md",
        out Uri? uri,
        GitHubCacheService.DefaultAllowedPathPrefixes);

    ok.ShouldBeTrue();
    uri.ShouldNotBeNull();
    uri!.AbsolutePath.ShouldStartWith(GitHubCacheService.AllowedRepoPathPrefix);

    await Task.CompletedTask;
  }

  public static async Task Should_reject_unsafe_cache_ids()
  {
    GitHubCacheService.IsSafeCacheId("../evil").ShouldBeFalse();
    GitHubCacheService.IsSafeCacheId("https://evil.com").ShouldBeFalse();
    GitHubCacheService.IsSafeCacheId("foo/bar").ShouldBeFalse();
    GitHubCacheService.IsSafeCacheId("/abs").ShouldBeFalse();
    GitHubCacheService.IsSafeCacheId("hello-world-fluent").ShouldBeTrue();

    await Task.CompletedTask;
  }

  public static async Task Should_sanitize_cache_filename_containing_dotdot()
  {
    string safe = GitHubCacheService.GetSafeCacheFileName("../evil/secret");

    safe.ShouldNotContain('/');
    safe.ShouldNotContain('\\');
    safe.ShouldNotBe("..");
    // Collapsed ".." forms must not remain as a traversable segment name.
    safe.Contains("..", StringComparison.Ordinal).ShouldBeFalse();

    string combined = Path.Combine(Path.GetTempPath(), "mcp-cache-test", $"{safe}.cache");
    string full = Path.GetFullPath(combined);
    string root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcp-cache-test"));
    full.StartsWith(root, StringComparison.Ordinal).ShouldBeTrue();

    await Task.CompletedTask;
  }
}

} // namespace TimeWarp.Nuru.Tests.Mcp
