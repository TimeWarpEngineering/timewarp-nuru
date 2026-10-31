#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text;
using global::DevCli;

/// <summary>
/// Two-feed publish lookup (kanban task 536): NuGetVersionService.LookupAsync reads
/// nuget.org and the org GitHub Packages feed, and PackageFeedLookup.Check counts a
/// version on either as published. The fake handler answers with the shapes observed
/// live on 2026-10-29 (see nuget-version-service.cs Design): the real service index,
/// newest-first version lists, NAME_UNKNOWN 404s, and 401/403 for missing access.
/// No test touches the network or the machine's tokens.
/// These tests declare both feeds; the declared-feed tests (kanban task 564) follow them.
/// </summary>
[TestTag("DevCli")]
public class TwoFeedLookupTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<TwoFeedLookupTests>();

  public static async Task Found_on_nuget_org_only_is_published()
  {
    FeedStub feeds = new() { NuGetOrg = _ => FeedStub.Versions("1.0.0", "1.1.0") };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.1.0").ShouldBe(PackagePublishVerdict.Published);
    lookup.GitHubPackagesVersions.Count.ShouldBe(0);
    lookup.GitHubPackagesFailure.ShouldBeNull();
  }

  public static async Task Found_on_github_packages_only_is_published()
  {
    // GitHub Packages lists newest first; the latest must still be the SemVer maximum.
    FeedStub feeds = new() { GitHubPackages = _ => FeedStub.Versions("1.0.0-beta.16", "1.0.0-beta.15", "1.0.0-beta.9") };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0-beta.15").ShouldBe(PackagePublishVerdict.Published);
    lookup.NuGetOrgVersions.Count.ShouldBe(0);
    lookup.LatestVersion.ShouldBe("1.0.0-beta.16");
  }

  public static async Task Found_on_neither_is_not_published()
  {
    FeedStub feeds = new()
    {
      NuGetOrg = _ => FeedStub.Versions("1.0.0"),
      GitHubPackages = _ => FeedStub.Versions("0.9.0")
    };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("2.0.0").ShouldBe(PackagePublishVerdict.NotPublished);
    lookup.LatestVersion.ShouldBe("1.0.0");
  }

  public static async Task Github_packages_404_for_unknown_id_is_not_found()
  {
    // nuget.org-only repos: GitHub Packages answers NAME_UNKNOWN, which is "not found".
    FeedStub feeds = new();
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.NotPublished);
    lookup.GitHubPackagesFailure.ShouldBeNull();
    lookup.LatestVersion.ShouldBeNull();
  }

  public static async Task Github_packages_auth_failure_is_unknown()
  {
    // No usable token: the feed answers 401 for the service index (observed).
    FeedStub feeds = new() { GitHubIndex = () => FeedStub.Unauthorized() };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    lookup.GitHubPackagesFailure.ShouldNotBeNull();
    lookup.GitHubPackagesFailure!.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

    string refusal = string.Join("\n", lookup.FormatUnknownRefusal("1.0.0"));
    refusal.ShouldContain("HTTP 401 Unauthorized");
    refusal.ShouldContain(NuGetVersionService.GitHubPackagesServiceIndexUrl);
    refusal.ShouldContain("gh auth refresh -h github.com -s read:packages");
    refusal.ShouldContain("packages: read");
    refusal.ShouldNotContain("already");
  }

  public static async Task Token_without_packages_access_is_unknown_not_published()
  {
    // A token lacking read:packages: 403 on the flat container (ganda 363).
    FeedStub feeds = new() { GitHubPackages = _ => new HttpResponseMessage(HttpStatusCode.Forbidden) };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    lookup.GitHubPackagesFailure!.StatusDisplay.ShouldBe("HTTP 403 Forbidden");
    lookup.GitHubPackagesFailure.Message.ShouldContain("using the GITHUB_TOKEN token");
  }

  public static async Task No_token_is_unknown_without_a_request()
  {
    FeedStub feeds = new();
    using NuGetVersionService service = feeds.CreateService(credential: null);

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    lookup.GitHubPackagesFailure!.Message.ShouldContain("no GitHub token");
    lookup.GitHubPackagesFailure.StatusDisplay.ShouldBe("no HTTP response");
    feeds.GitHubRequestCount.ShouldBe(0);
  }

  public static async Task Found_on_nuget_org_is_published_even_when_github_packages_fails()
  {
    FeedStub feeds = new()
    {
      NuGetOrg = _ => FeedStub.Versions("1.0.0"),
      GitHubIndex = () => FeedStub.Unauthorized()
    };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Published);
    lookup.Check("2.0.0").ShouldBe(PackagePublishVerdict.Unknown);
  }

  public static async Task Nuget_org_unreachable_throws()
  {
    FeedStub feeds = new() { NuGetOrg = _ => throw new HttpRequestException("Name or service not known") };
    using NuGetVersionService service = feeds.CreateService();

    HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None));

    exception.ShouldNotBeOfType<PackageFeedUnreadableException>();
  }

  public static async Task Nuget_org_outage_throws_with_status()
  {
    FeedStub feeds = new() { NuGetOrg = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable) };
    using NuGetVersionService service = feeds.CreateService();

    HttpRequestException exception = await Should.ThrowAsync<HttpRequestException>(async () =>
      await service.LookupAsync("TimeWarp.Nuru", PackageFeeds.Both, CancellationToken.None));

    exception.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
  }

  public static async Task Github_packages_unreachable_is_unknown()
  {
    FeedStub feeds = new() { GitHubPackages = _ => throw new HttpRequestException("Connection refused") };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    lookup.GitHubPackagesFailure!.Message.ShouldContain("unreachable");
  }

  public static async Task Github_packages_5xx_and_malformed_answers_are_unknown()
  {
    Func<string, HttpResponseMessage>[] answers =
    [
      _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
      _ => FeedStub.Json("<html>Sign in</html>"),
      _ => FeedStub.Json("{\"versions\":[]}")
    ];

    foreach (Func<string, HttpResponseMessage> answer in answers)
    {
      FeedStub feeds = new() { GitHubPackages = answer };
      using NuGetVersionService service = feeds.CreateService();

      PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

      lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    }
  }

  public static async Task Service_index_without_package_base_address_is_unknown()
  {
    FeedStub feeds = new() { GitHubIndex = () => FeedStub.Json("{\"version\":\"3.0.0-beta.1\",\"resources\":[]}") };
    using NuGetVersionService service = feeds.CreateService();

    PackageFeedLookup lookup = await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);

    lookup.Check("1.0.0").ShouldBe(PackagePublishVerdict.Unknown);
    lookup.GitHubPackagesFailure!.Message.ShouldContain(NuGetVersionService.PackageBaseAddressResourceType);
  }

  public static async Task Flat_container_url_comes_from_the_service_index_with_basic_auth()
  {
    FeedStub feeds = new() { PackageBaseAddress = "https://feed.example.test/elsewhere/download" };
    using NuGetVersionService service = feeds.CreateService();

    await service.LookupAsync("TimeWarp.Taratibu", PackageFeeds.Both, CancellationToken.None);
    await service.LookupAsync("TimeWarp.Taratibu.Routines", PackageFeeds.Both, CancellationToken.None);

    feeds.GitHubUris.ShouldBe(
    [
      NuGetVersionService.GitHubPackagesServiceIndexUrl,
      "https://feed.example.test/elsewhere/download/timewarp.taratibu/index.json",
      "https://feed.example.test/elsewhere/download/timewarp.taratibu.routines/index.json"
    ]);

    string expected = Convert.ToBase64String(Encoding.UTF8.GetBytes($"DevCli:{FeedStub.Token}"));
    feeds.GitHubAuthorizations.ShouldAllBe(header => header == $"Basic {expected}");
    feeds.NuGetOrgAuthorizations.ShouldAllBe(header => header == null);
  }

  public static async Task Release_gate_partial_state_counts_both_feeds()
  {
    // `dev release` step 7: one package only on GitHub Packages, one on neither,
    // is Partial ("partially published but was never tagged"), not None.
    FeedStub feeds = new()
    {
      GitHubPackages = id => id == "timewarp.taratibu" ? FeedStub.Versions("1.0.0") : FeedStub.GitHubNotFound(id)
    };
    using NuGetVersionService service = feeds.CreateService();

    int published = 0;
    foreach (string pkg in new[] { "TimeWarp.Taratibu", "TimeWarp.Taratibu.Routines" })
    {
      PackageFeedLookup lookup = await service.LookupAsync(pkg, PackageFeeds.Both, CancellationToken.None);
      if (lookup.Check("1.0.0") == PackagePublishVerdict.Published)
      {
        published++;
      }
    }

    PublishState state = PublishStateClassifier.Classify(2, published);
    state.ShouldBe(PublishState.Partial);
    ReleaseGuard.CheckPublishState(state, "1.0.0").Reason!.ShouldContain("partially published but was never tagged");
  }

  public static async Task Abort_reason_names_the_feed_not_already_released()
  {
    PackageFeedUnreadableException failure = new("forbidden", inner: null, HttpStatusCode.Forbidden);

    string reason = CheckVersionAbort.FormatAbortReason(CheckVersionOutcome.GitHubPackagesUnreadable, failure);

    reason.ShouldContain(NuGetVersionService.GitHubPackagesServiceIndexUrl);
    reason.ShouldContain("HTTP 403 Forbidden");
    reason.ShouldContain("packages: read");
    reason.ShouldNotContain("already released");
    CheckVersionAbort.FormatAbortReason(CheckVersionOutcome.AlreadyReleased, null).ShouldBe("version already released");
    CheckVersionAbort.FormatAbortReason(CheckVersionOutcome.NuGetOrgLookupFailed, null).ShouldNotContain("already released");
    await Task.CompletedTask;
  }

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

  /// <summary>
  /// Routes requests by feed, answering like the live feeds. Defaults: nuget.org 404,
  /// the real GitHub Packages service index, and a NAME_UNKNOWN 404 for every id.
  /// </summary>
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

} // namespace TimeWarp.Nuru.Tests.DevCli
