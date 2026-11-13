#!/usr/bin/env -S dotnet --

#region Purpose
// Pins the declared-feed behavior of the publish lookup (task 564): nuget.org only, GitHub Packages only, empty or unknown feed sets, and credential order.
#endregion

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCliDeclaredFeeds
{

using System.Text;
using global::DevCli;

/// <summary>
/// Declared-feed tests for NuGetVersionService.LookupAsync (kanban task 564), split from check-version-09. The fake
/// handler answers with the same live shapes as the two-feed tests.
/// </summary>
[TestTag("DevCli")]
public class DeclaredFeedLookupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<DeclaredFeedLookupTests>();

  // --- Declared feeds (kanban task 564) ------------------------------------

  public static async Task Nuget_org_only_never_touches_github_packages_or_a_credential()
  {
    int credentialCalls = 0;
    FeedStub feeds = new() { NuGetOrg = _ => FeedStub.Versions("1.0.0") };
    using NuGetVersionService service = new(feeds, _ =>
    {
      credentialCalls++;
      return Task.FromResult<GitHubPackagesCredential?>(new GitHubPackagesCredential(FeedStub.Token, "GITHUB_TOKEN"));
    });

    PackageFeedLookup published = await service.LookupAsync("TimeWarp.Mediator", PackageFeeds.NuGetOrg, CancellationToken.None);
    PackageFeedLookup unpublished = await service.LookupAsync("TimeWarp.Mediator.Generators", PackageFeeds.NuGetOrg, CancellationToken.None);

    published.Check("1.0.0").ShouldBe(PackagePublishVerdict.Published);
    unpublished.Check("15.0.0-beta.3").ShouldBe(PackagePublishVerdict.NotPublished);
    unpublished.GitHubPackagesFailure.ShouldBeNull();
    unpublished.Feeds.ShouldBe(PackageFeeds.NuGetOrg);
    feeds.GitHubRequestCount.ShouldBe(0);
    credentialCalls.ShouldBe(0);
  }

  public static async Task Nuget_org_only_without_a_token_is_not_unknown()
  {
    // The Mediator v15.0.0-beta.3 failure: no token anywhere, not on nuget.org.
    FeedStub feeds = new();
    using NuGetVersionService service = feeds.CreateService(credential: null);

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Mediator", PackageFeeds.NuGetOrg, CancellationToken.None);

    lookup.Check("15.0.0-beta.3").ShouldBe(PackagePublishVerdict.NotPublished);
    feeds.GitHubRequestCount.ShouldBe(0);
    feeds.NuGetOrgAuthorizations.Count.ShouldBe(1);
  }

  public static async Task Github_packages_only_never_touches_nuget_org()
  {
    FeedStub feeds = new() { GitHubPackages = _ => FeedStub.Versions("1.0.0-beta.16", "1.0.0-beta.15") };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.GitHubPackages, CancellationToken.None);

    lookup.Check("1.0.0-beta.15").ShouldBe(PackagePublishVerdict.Published);
    lookup.LatestVersion.ShouldBe("1.0.0-beta.16");
    feeds.NuGetOrgAuthorizations.Count.ShouldBe(0);
  }

  public static async Task Github_packages_only_refusal_does_not_claim_a_nuget_org_lookup()
  {
    FeedStub feeds = new() { GitHubIndex = () => FeedStub.Unauthorized() };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.GitHubPackages, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    string refusal = string.Join("\n", lookup.FormatUnknownRefusal("1.0.0"));
    refusal.ShouldContain("HTTP 401 Unauthorized");
    refusal.ShouldContain(NuGetVersionService.GitHubPackagesServiceIndexUrl);
    refusal.ShouldContain("packages: read");
    refusal.ShouldNotContain("nuget.org");
    refusal.ShouldNotContain("already");
  }

  public static async Task Both_feeds_keep_the_536_refusal_text()
  {
    FeedStub feeds = new() { GitHubIndex = () => FeedStub.Unauthorized() };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Ganda", PackageFeeds.Both, CancellationToken.None);

    lookup.FormatUnknownRefusal("1.0.0").ShouldBe(
    [
      "Error: GitHub Packages lookup for 'TimeWarp.Ganda' failed (HTTP 401 Unauthorized): GitHub Packages answered HTTP 401 (Unauthorized) for its service index https://nuget.pkg.github.com/TimeWarpEngineering/index.json using the GITHUB_TOKEN token.",
      "  TimeWarp.Ganda 1.0.0 is not on nuget.org, and the org GitHub Packages feed (https://nuget.pkg.github.com/TimeWarpEngineering/index.json) could not be read, so whether it is published is unknown. Refusing to continue.",
      "  Fix (local): gh auth refresh -h github.com -s read:packages, or set GITHUB_TOKEN / GH_TOKEN to a token with read:packages.",
      "  Fix (GitHub Actions): add 'packages: read' to the job's permissions."
    ]);
  }

  public static async Task Empty_feed_set_throws_before_any_request()
  {
    int credentialCalls = 0;
    FeedStub feeds = new();
    using NuGetVersionService service = new(feeds, _ =>
    {
      credentialCalls++;
      return Task.FromResult<GitHubPackagesCredential?>(null);
    });

    await Should.ThrowAsync<ArgumentException>(async () =>
      await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.None, CancellationToken.None));

    feeds.NuGetOrgAuthorizations.Count.ShouldBe(0);
    feeds.GitHubRequestCount.ShouldBe(0);
    credentialCalls.ShouldBe(0);
  }

  public static async Task Absent_feeds_resolve_to_nuget_org()
  {
    PackageFeedsResolver.Resolve(null).ShouldBe(new PackageFeedsResolution(PackageFeeds.NuGetOrg, null));
    PackageFeedsResolver.DefaultFeeds.ShouldBe(PackageFeeds.NuGetOrg);

    // Absent key and absent section both give the default.
    RepoConfig noKey = RepoConfigService.Parse("""{ "checkVersionConfig": { "packages": "TimeWarp.Mediator" } }""");
    RepoConfig noSection = RepoConfigService.Parse("{}");
    PackageFeedsResolver.Resolve(noKey.CheckVersionConfig?.Feeds).Feeds.ShouldBe(PackageFeeds.NuGetOrg);
    PackageFeedsResolver.Resolve(noSection.CheckVersionConfig?.Feeds).Feeds.ShouldBe(PackageFeeds.NuGetOrg);
    await Task.CompletedTask;
  }

  public static async Task Declared_feeds_parse_like_packages()
  {
    PackageFeedsResolver.Resolve("nuget.org").Feeds.ShouldBe(PackageFeeds.NuGetOrg);
    PackageFeedsResolver.Resolve("github-packages").Feeds.ShouldBe(PackageFeeds.GitHubPackages);
    PackageFeedsResolver.Resolve("nuget.org,github-packages").Feeds.ShouldBe(PackageFeeds.Both);
    PackageFeedsResolver.Resolve(" GitHub-Packages , NuGet.org ,").ShouldBe(new PackageFeedsResolution(PackageFeeds.Both, null));

    RepoConfig config = RepoConfigService.Parse(
      """
      {
        "checkVersionConfig": {
          // feeds: nuget.org | github-packages, comma-separated.
          "feeds": "nuget.org,github-packages",
        }
      }
      """);
    PackageFeedsResolver.Resolve(config.CheckVersionConfig?.Feeds).Feeds.ShouldBe(PackageFeeds.Both);
    await Task.CompletedTask;
  }

  public static async Task Unknown_or_empty_feeds_are_configuration_errors()
  {
    PackageFeedsResolution typo = PackageFeedsResolver.Resolve("nuget.org,github-package");
    typo.Feeds.ShouldBe(PackageFeeds.None);
    string typoError = typo.Error.ShouldNotBeNull();
    typoError.ShouldContain("'github-package'");
    typoError.ShouldContain("checkVersionConfig.feeds");
    typoError.ShouldContain("nuget.org, github-packages");

    foreach (string empty in new[] { "", " ", "," })
    {
      PackageFeedsResolution resolution = PackageFeedsResolver.Resolve(empty);
      resolution.Feeds.ShouldBe(PackageFeeds.None);
      resolution.Error.ShouldNotBeNull().ShouldContain("names no feed");
    }

    await Task.CompletedTask;
  }

  public static async Task Credential_order_is_github_token_then_gh_token()
  {
    string? githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    string? ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");

    try
    {
      Environment.SetEnvironmentVariable("GITHUB_TOKEN", "first");
      Environment.SetEnvironmentVariable("GH_TOKEN", "second");
      GitHubPackagesCredential? both = await GitHubPackagesCredential.ResolveAsync(CancellationToken.None);
      both.ShouldBe(new GitHubPackagesCredential("first", "GITHUB_TOKEN"));

      Environment.SetEnvironmentVariable("GITHUB_TOKEN", " ");
      GitHubPackagesCredential? ghOnly = await GitHubPackagesCredential.ResolveAsync(CancellationToken.None);
      ghOnly.ShouldBe(new GitHubPackagesCredential("second", "GH_TOKEN"));
      ghOnly!.ToString().ShouldNotContain("second");
    }
    finally
    {
      Environment.SetEnvironmentVariable("GITHUB_TOKEN", githubToken);
      Environment.SetEnvironmentVariable("GH_TOKEN", ghToken);
    }
  }

  /// <summary>Routes requests by feed, answering like the live feeds. Defaults: nuget.org 404, the real
  /// GitHub Packages service index, and a NAME_UNKNOWN 404 for every id.</summary>
  private sealed class FeedStub : HttpMessageHandler
  {
    public const string Token = "ghs_fake_token_for_tests";
    private const string GitHubHost = "nuget.pkg.github.com";

    public Func<string, HttpResponseMessage> NuGetOrg { get; init; } = _ => new HttpResponseMessage(HttpStatusCode.NotFound);

    public Func<string, HttpResponseMessage> GitHubPackages { get; init; } = GitHubNotFound;

    public Func<HttpResponseMessage>? GitHubIndex { get; init; }

    public string PackageBaseAddress { get; init; } = "https://nuget.pkg.github.com/TimeWarpEngineering/download";

    public List<string> GitHubUris { get; } = [];

    public List<string?> GitHubAuthorizations { get; } = [];

    public List<string?> NuGetOrgAuthorizations { get; } = [];

    public int GitHubRequestCount => GitHubUris.Count;

    public NuGetVersionService CreateService() =>
      new(this, _ => Task.FromResult<GitHubPackagesCredential?>(new GitHubPackagesCredential(Token, "GITHUB_TOKEN")));

    public NuGetVersionService CreateService(GitHubPackagesCredential? credential) =>
      new(this, _ => Task.FromResult(credential));

    public static HttpResponseMessage Versions(params string[] versions) =>
      Json($"{{\"versions\":[{string.Join(",", versions.Select(v => $"\"{v}\""))}]}}");

    public static HttpResponseMessage Json(string body) =>
      new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage Unauthorized() =>
      new(HttpStatusCode.Unauthorized);

    public static HttpResponseMessage GitHubNotFound(string id) =>
      new(HttpStatusCode.NotFound)
      {
        Content = new StringContent($"{{\"errors\":[{{\"code\":\"NAME_UNKNOWN\",\"message\":\"Package with name '{id}' was not found in the package source 'TimeWarpEngineering'\"}}]}}", Encoding.UTF8, "application/json")
      };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
      Uri uri = request.RequestUri!;
      string? authorization = request.Headers.Authorization?.ToString();

      if (uri.Host == "api.nuget.org")
      {
        NuGetOrgAuthorizations.Add(authorization);
        return Task.FromResult(NuGetOrg(IdFrom(uri)));
      }

      GitHubUris.Add(uri.ToString());
      GitHubAuthorizations.Add(authorization);

      if (uri.ToString() == NuGetVersionService.GitHubPackagesServiceIndexUrl)
      {
        return Task.FromResult(GitHubIndex?.Invoke() ?? Json(ServiceIndexBody()));
      }

      if (uri.Host == GitHubHost || uri.ToString().StartsWith(PackageBaseAddress, StringComparison.Ordinal))
      {
        return Task.FromResult(GitHubPackages(IdFrom(uri)));
      }

      throw new InvalidOperationException($"Unexpected request {uri}");
    }

    // .../{id}/index.json -> id
    private static string IdFrom(Uri uri) => uri.Segments[^2].TrimEnd('/');

    // The live service index, abridged to the resources that matter.
    private string ServiceIndexBody() =>
      $$"""
      {"version":"3.0.0-beta.1","resources":[
        {"@id":"{{PackageBaseAddress}}","@type":"PackageBaseAddress/3.0.0","comment":"Get package content (.nupkg)."},
        {"@id":"https://nuget.pkg.github.com/TimeWarpEngineering/query","@type":"SearchQueryService","comment":"Filter and search for packages by keyword."},
        {"@id":"https://nuget.pkg.github.com/TimeWarpEngineering","@type":"PackagePublish/2.0.0","comment":"Push and delete (or unlist) packages."},
        {"@id":"https://nuget.pkg.github.com/TimeWarpEngineering","@type":"RegistrationsBaseUrl","comment":"Get package metadata."}
      ]}
      """;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCliDeclaredFeeds
