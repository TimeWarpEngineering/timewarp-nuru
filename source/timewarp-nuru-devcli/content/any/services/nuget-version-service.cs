#region Purpose
// HttpClient-based NuGet version checking — replaces TimeWarp.Amuru's NuGetPackageService
// which depends on NuGet.Protocol/NuGet.Packaging (pulls in Newtonsoft.Json, incompatible with AOT).
// Uses the NuGet V3 Flat Container API directly with System.Text.Json.
// Reads the feeds the repo publishes to (checkVersionConfig.feeds, kanban
// task 564): nuget.org, the org GitHub Packages feed, or both, so the
// already-released gates see packages published to any of them (task 536).
#endregion
#region Design
// Calls https://api.nuget.org/v3-flatcontainer/{id}/index.json
// which returns a simple JSON object with a "versions" array.
// No NuGet.Protocol, no NuGet.Packaging, no Newtonsoft.Json — fully AOT-compatible.
//
// Fail-closed lookup (kanban task 470-007, parent-470 findings M9/M31): this
// service feeds the already-released gate, whose callers treat an empty list
// as "never published". Only HTTP 404 (the flat container's answer for an
// unknown id) maps to empty; every other non-success status (429, 5xx, auth)
// throws HttpRequestException carrying the status code so a NuGet outage can
// never clear the gate. The package id is validated against NuGet id grammar
// (ASCII word chars with single '.'/'-' separators, max 100 chars) and the
// path segment is escaped, so a --package like "../evil" cannot walk off
// v3-flatcontainer. The HttpMessageHandler constructor exists for tests.
//
// The service owns its handler in a field and disposes it after the client
// (kanban task 560). Consumers compile this file, and CA2000 does not treat
// new HttpClient(handler) as taking ownership, so it flagged the handler the
// parameterless constructor creates. A field the service disposes is an
// ownership the analyzer sees; it needs no suppression.
//
// The feed set is a property of the repo, not of the tool (kanban task 564).
// TimeWarp publishes to two feeds only, nuget.org and the org GitHub Packages
// feed, so both URLs are constants here. Which of them a repo uses is not:
// TimeWarp.Mediator and this repo publish to nuget.org only, TimeWarp.Taratibu
// and TimeWarp.Zana to GitHub Packages only, TimeWarp.Ganda and
// TimeWarp.Flexbox to both. Task 536 read both feeds for every repo, and the
// Mediator v15.0.0-beta.3 release (2026-10-31) failed because its CI had no
// packages: read token for a feed it never publishes to. Steven's direction
// that day: a repo must not depend on a feed it does not publish to. So
// LookupAsync takes the PackageFeeds the caller resolved from
// checkVersionConfig.feeds (PackageFeedsResolver, default nuget.org) and reads
// only those. When GitHub Packages is not declared, no request goes to it and
// the credential provider is never called, so no token is resolved. No
// attestation or ganda rule requires GitHub Packages everywhere; repo
// visibility is no guide either (Roslynk is public and publishes to GitHub
// Packages), so the repo declares it.
//
// GitHub Packages shapes (task 536), observed
// live 2026-10-29 with a read:packages gh token:
//   - The service index answers 200; its PackageBaseAddress/3.0.0 resource is
//     https://nuget.pkg.github.com/TimeWarpEngineering/download (no trailing
//     slash). The flat container is {base}/{lowercase id}/index.json, read
//     from the index rather than assumed.
//   - Known id: 200 {"versions":[...]} listed NEWEST FIRST (nuget.org lists
//     oldest first), so callers take the SemVer maximum, not versions[^1].
//   - Unknown id: 404 {"errors":[{"code":"NAME_UNKNOWN",...}]} = not found.
//   - No token: 401 (WWW-Authenticate: Basic) for the index, a known id and
//     an unknown id alike.
//   - An invalid token: index 200, unknown id 404, known id 401. A bad token
//     cannot make a published package look unpublished.
//   - A gh token without read:packages: 403 on the flat container (ganda task
//     363, 2026-10-08).
// Auth is HTTP Basic with the token as password, the scheme NuGet clients
// use; GitHubPackagesCredential finds the token. Every GitHub Packages
// failure (no token, 401/403, other non-404, timeout, malformed answer) is a
// PackageFeedUnreadableException. LookupAsync keeps it on the result instead
// of throwing, because a version found on nuget.org is published regardless;
// the gates refuse only when the version is on no other declared feed. A
// nuget.org failure still throws HttpRequestException (470-007). The service
// index is read once per service instance, and its answer (base address or
// failure) is reused for every package.
#endregion

namespace DevCli;

using System.Text.Json;
using System.Text.Json.Serialization;

internal sealed class NuGetVersionService : IDisposable
{
  private readonly HttpMessageHandler Handler;
  private readonly HttpClient HttpClient;

  private readonly Func<CancellationToken, Task<GitHubPackagesCredential?>> GitHubCredentialProvider;
  private bool GitHubCredentialResolved;
  private GitHubPackagesCredential? GitHubCredential;
  private Uri? GitHubPackagesBaseAddress;
  private PackageFeedUnreadableException? GitHubPackagesIndexFailure;

  public const int MaxPackageIdLength = 100;

  public const string NuGetOrgFlatContainerUrl = "https://api.nuget.org/v3-flatcontainer/";

  /// <summary>The org's GitHub Packages feed, the only other feed TimeWarp publishes to. Read only when a repo declares <c>github-packages</c>.</summary>
  public const string GitHubPackagesServiceIndexUrl = "https://nuget.pkg.github.com/TimeWarpEngineering/index.json";

  public const string PackageBaseAddressResourceType = "PackageBaseAddress/3.0.0";

  public NuGetVersionService()
    : this
    (
      new HttpClientHandler
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
      },
      GitHubPackagesCredential.ResolveAsync
    )
  {
  }

  /// <summary>
  /// Test seam: build the service over a caller-supplied handler (e.g. a stub that
  /// returns a fixed status code). The handler is disposed with the service.
  /// Internal on purpose: the Nuru DI generator resolves the PUBLIC constructor with
  /// the most parameters, so a public overload would make it demand a registered
  /// HttpMessageHandler (NURU051). Test runfiles compile this file into their own
  /// assembly, so internal is visible there.
  /// </summary>
  internal NuGetVersionService(HttpMessageHandler handler)
    : this(handler, _ => Task.FromResult<GitHubPackagesCredential?>(null))
  {
  }

  /// <summary>
  /// Test seam with a fixed GitHub Packages credential source, so a test never
  /// reads the machine's GITHUB_TOKEN, GH_TOKEN or gh login. The one-argument
  /// overload has no credential, so GitHub Packages is always unreadable there.
  /// </summary>
  internal NuGetVersionService
  (
    HttpMessageHandler handler,
    Func<CancellationToken, Task<GitHubPackagesCredential?>> gitHubCredentialProvider
  )
  {
    ArgumentNullException.ThrowIfNull(handler);
    ArgumentNullException.ThrowIfNull(gitHubCredentialProvider);
    Handler = handler;
    HttpClient = new HttpClient(handler, disposeHandler: false);
    GitHubCredentialProvider = gitHubCredentialProvider;
  }

  /// <summary>
  /// Builds the flat-container index URL for <paramref name="packageId"/>. Throws
  /// <see cref="ArgumentException"/> when the id fails <see cref="IsValidPackageId"/>.
  /// </summary>
  public static Uri GetIndexUrl(string packageId)
  {
    ArgumentNullException.ThrowIfNull(packageId);

    ThrowIfInvalidPackageId(packageId);

    return new Uri($"{NuGetOrgFlatContainerUrl}{GetIdSegment(packageId)}/index.json");
  }

  /// <summary>
  /// Builds the flat-container index URL for <paramref name="packageId"/> under a
  /// feed's <c>PackageBaseAddress/3.0.0</c> resource. Throws
  /// <see cref="ArgumentException"/> when the id fails <see cref="IsValidPackageId"/>.
  /// </summary>
  public static Uri GetIndexUrl(Uri packageBaseAddress, string packageId)
  {
    ArgumentNullException.ThrowIfNull(packageBaseAddress);
    ArgumentNullException.ThrowIfNull(packageId);

    ThrowIfInvalidPackageId(packageId);

    return new Uri($"{packageBaseAddress.AbsoluteUri.TrimEnd('/')}/{GetIdSegment(packageId)}/index.json");
  }

  private static void ThrowIfInvalidPackageId(string packageId)
  {
    if (!IsValidPackageId(packageId))
    {
      throw new ArgumentException($"'{packageId}' is not a valid NuGet package id (letters, digits, '_', with single '.' or '-' separators; max {MaxPackageIdLength} chars).", nameof(packageId));
    }
  }

  private static string GetIdSegment(string packageId) => Uri.EscapeDataString(packageId.ToLowerInvariant());

  /// <summary>
  /// NuGet package id grammar (mirrors NuGet.Packaging's PackageIdValidator):
  /// one or more ASCII letters/digits/underscores, optionally joined by single
  /// '.' or '-' separators; no leading/trailing/consecutive separators; at most
  /// <see cref="MaxPackageIdLength"/> characters.
  /// </summary>
  public static bool IsValidPackageId(string? packageId)
  {
    if (string.IsNullOrEmpty(packageId) || packageId.Length > MaxPackageIdLength)
    {
      return false;
    }

    bool previousWasSeparator = true; // disallow a leading separator

    foreach (char c in packageId)
    {
      if (IsWordChar(c))
      {
        previousWasSeparator = false;
      }
      else if (c is '.' or '-')
      {
        if (previousWasSeparator)
        {
          return false;
        }

        previousWasSeparator = true;
      }
      else
      {
        return false;
      }
    }

    return !previousWasSeparator; // disallow a trailing separator
  }

  private static bool IsWordChar(char c) =>
    c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or (>= '0' and <= '9') or '_';

  public async Task<IReadOnlyList<string>> GetPackageVersionsAsync
  (
    string packageId,
    CancellationToken cancellationToken
  )
  {
    Uri url = GetIndexUrl(packageId);
    using HttpResponseMessage response = await SendAsync(url, packageId, cancellationToken).ConfigureAwait(false);

    // 404 is the flat container's "no such package" — the only status that
    // legitimately means "nothing published". Anything else is unknown state
    // and must fail closed: callers treat [] as "safe to release".
    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return [];
    }

    if (!response.IsSuccessStatusCode)
    {
      throw new HttpRequestException
      (
        $"NuGet version lookup for '{packageId}' failed with HTTP {(int)response.StatusCode} ({response.StatusCode}) from {url}.",
        inner: null,
        response.StatusCode
      );
    }

    Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    await using (stream.ConfigureAwait(false))
    {
      NuGetVersionIndex? index;
      try
      {
        index = await JsonSerializer
          .DeserializeAsync(stream, DevCliJsonContext.Default.NuGetVersionIndex, cancellationToken)
          .ConfigureAwait(false);
      }
      catch (JsonException ex)
      {
        // A non-JSON 200 (captive portal, proxy error page) is a failed lookup,
        // not "never published"; surface it through the same fail-closed path.
        throw new HttpRequestException($"NuGet version lookup for '{packageId}' returned HTTP 200 with a non-JSON body from {url}: {ex.Message}", ex, response.StatusCode);
      }

      // A 200 with no index object, or an index with no versions, is a malformed
      // response, not "never published": the flat container answers 404 for an
      // unknown id and never returns an empty versions array for a known one.
      if (index?.Versions is not { Count: > 0 } versions)
      {
        throw new HttpRequestException($"NuGet version lookup for '{packageId}' returned HTTP 200 with no versions index from {url}.", inner: null, response.StatusCode);
      }

      return versions;
    }
  }

  /// <summary>
  /// Sends the index request, converting an HttpClient timeout (an
  /// <see cref="OperationCanceledException"/> while the caller's token is NOT
  /// cancelled) into <see cref="HttpRequestException"/> so every lookup failure
  /// reaches the callers' fail-closed catch. Caller cancellation propagates as is.
  /// </summary>
  private async Task<HttpResponseMessage> SendAsync(Uri url, string packageId, CancellationToken cancellationToken)
  {
    try
    {
      return await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
    {
      throw new HttpRequestException($"NuGet version lookup for '{packageId}' timed out requesting {url}.", ex, System.Net.HttpStatusCode.RequestTimeout);
    }
  }

  /// <summary>
  /// Reads <paramref name="packageId"/> from the declared <paramref name="feeds"/> only.
  /// A nuget.org failure throws <see cref="HttpRequestException"/>, as
  /// <see cref="GetPackageVersionsAsync"/> does. A GitHub Packages failure is returned
  /// on the result, never thrown. When <paramref name="feeds"/> lacks
  /// <see cref="PackageFeeds.GitHubPackages"/>, GitHub Packages is not contacted and no
  /// credential is resolved. Throws <see cref="ArgumentException"/> when
  /// <paramref name="feeds"/> names no feed.
  /// </summary>
  public async Task<PackageFeedLookup> LookupAsync(string packageId, PackageFeeds feeds, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(packageId);

    if ((feeds & PackageFeeds.Both) == PackageFeeds.None || (feeds & ~PackageFeeds.Both) != PackageFeeds.None)
    {
      throw new ArgumentException($"'{feeds}' is not a feed set; declare {PackageFeeds.NuGetOrg}, {PackageFeeds.GitHubPackages} or both.", nameof(feeds));
    }

    ThrowIfInvalidPackageId(packageId);

    IReadOnlyList<string> nuGetOrgVersions = feeds.HasFlag(PackageFeeds.NuGetOrg)
      ? await GetPackageVersionsAsync(packageId, cancellationToken).ConfigureAwait(false)
      : [];

    if (!feeds.HasFlag(PackageFeeds.GitHubPackages))
    {
      return new PackageFeedLookup(packageId, feeds, nuGetOrgVersions, [], gitHubPackagesFailure: null);
    }

    try
    {
      IReadOnlyList<string> gitHubPackagesVersions = await GetGitHubPackagesVersionsAsync(packageId, cancellationToken).ConfigureAwait(false);
      return new PackageFeedLookup(packageId, feeds, nuGetOrgVersions, gitHubPackagesVersions, gitHubPackagesFailure: null);
    }
    catch (PackageFeedUnreadableException ex)
    {
      return new PackageFeedLookup(packageId, feeds, nuGetOrgVersions, [], ex);
    }
  }

  /// <summary>
  /// Versions of <paramref name="packageId"/> on the org GitHub Packages feed; empty
  /// on 404. Throws <see cref="PackageFeedUnreadableException"/> when the feed could
  /// not be read, and <see cref="ArgumentException"/> for an invalid id.
  /// </summary>
  public async Task<IReadOnlyList<string>> GetGitHubPackagesVersionsAsync
  (
    string packageId,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(packageId);

    // Reject before any credential lookup or request.
    ThrowIfInvalidPackageId(packageId);

    GitHubPackagesCredential credential = await GetGitHubCredentialAsync(cancellationToken).ConfigureAwait(false);
    Uri baseAddress = await GetGitHubPackagesBaseAddressAsync(credential, cancellationToken).ConfigureAwait(false);
    Uri url = GetIndexUrl(baseAddress, packageId);

    using HttpResponseMessage response = await SendGitHubPackagesAsync(url, credential, cancellationToken).ConfigureAwait(false);

    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
      return [];
    }

    if (!response.IsSuccessStatusCode)
    {
      throw new PackageFeedUnreadableException
      (
        $"GitHub Packages answered HTTP {(int)response.StatusCode} ({response.StatusCode}) for {url} using the {credential.Source} token.",
        inner: null,
        response.StatusCode
      );
    }

    NuGetVersionIndex? index = await ReadGitHubPackagesJsonAsync(response, url, DevCliJsonContext.Default.NuGetVersionIndex, cancellationToken).ConfigureAwait(false);

    if (index?.Versions is not { Count: > 0 } versions)
    {
      throw new PackageFeedUnreadableException($"GitHub Packages returned HTTP 200 with no versions index from {url}.", inner: null, response.StatusCode);
    }

    return versions;
  }

  private async Task<GitHubPackagesCredential> GetGitHubCredentialAsync(CancellationToken cancellationToken)
  {
    if (!GitHubCredentialResolved)
    {
      GitHubCredential = await GitHubCredentialProvider(cancellationToken).ConfigureAwait(false);
      GitHubCredentialResolved = true;
    }

    return GitHubCredential ?? throw new PackageFeedUnreadableException
    (
      $"no GitHub token: {GitHubPackagesCredential.GitHubTokenVariable} and {GitHubPackagesCredential.GhTokenVariable} are unset and '{GitHubPackagesCredential.GhAuthTokenSource}' returned none.",
      inner: null,
      statusCode: null
    );
  }

  private async Task<Uri> GetGitHubPackagesBaseAddressAsync(GitHubPackagesCredential credential, CancellationToken cancellationToken)
  {
    if (GitHubPackagesBaseAddress is not null)
    {
      return GitHubPackagesBaseAddress;
    }

    if (GitHubPackagesIndexFailure is not null)
    {
      throw GitHubPackagesIndexFailure;
    }

    try
    {
      GitHubPackagesBaseAddress = await ReadGitHubPackagesBaseAddressAsync(credential, cancellationToken).ConfigureAwait(false);
      return GitHubPackagesBaseAddress;
    }
    catch (PackageFeedUnreadableException ex)
    {
      GitHubPackagesIndexFailure = ex;
      throw;
    }
  }

  private async Task<Uri> ReadGitHubPackagesBaseAddressAsync(GitHubPackagesCredential credential, CancellationToken cancellationToken)
  {
    Uri indexUrl = new(GitHubPackagesServiceIndexUrl);
    using HttpResponseMessage response = await SendGitHubPackagesAsync(indexUrl, credential, cancellationToken).ConfigureAwait(false);

    if (!response.IsSuccessStatusCode)
    {
      throw new PackageFeedUnreadableException
      (
        $"GitHub Packages answered HTTP {(int)response.StatusCode} ({response.StatusCode}) for its service index {indexUrl} using the {credential.Source} token.",
        inner: null,
        response.StatusCode
      );
    }

    NuGetServiceIndex? serviceIndex = await ReadGitHubPackagesJsonAsync(response, indexUrl, DevCliJsonContext.Default.NuGetServiceIndex, cancellationToken).ConfigureAwait(false);

    foreach (NuGetServiceIndexResource resource in serviceIndex?.Resources ?? [])
    {
      if (string.Equals(resource.Type, PackageBaseAddressResourceType, StringComparison.Ordinal) &&
          Uri.TryCreate(resource.Id, UriKind.Absolute, out Uri? baseAddress) &&
          baseAddress.Scheme == Uri.UriSchemeHttps)
      {
        return baseAddress;
      }
    }

    throw new PackageFeedUnreadableException($"GitHub Packages service index {indexUrl} has no https {PackageBaseAddressResourceType} resource.", inner: null, response.StatusCode);
  }

  private async Task<HttpResponseMessage> SendGitHubPackagesAsync(Uri url, GitHubPackagesCredential credential, CancellationToken cancellationToken)
  {
    using HttpRequestMessage request = new(HttpMethod.Get, url);
    string basic = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"DevCli:{credential.Token}"));
    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);

    try
    {
      return await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
    catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
    {
      throw new PackageFeedUnreadableException($"GitHub Packages request timed out for {url}.", ex, System.Net.HttpStatusCode.RequestTimeout);
    }
    catch (HttpRequestException ex)
    {
      throw new PackageFeedUnreadableException($"GitHub Packages is unreachable at {url}: {ex.Message}", ex, ex.StatusCode);
    }
  }

  private static async Task<T?> ReadGitHubPackagesJsonAsync<T>
  (
    HttpResponseMessage response,
    Uri url,
    System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo,
    CancellationToken cancellationToken
  )
  {
    Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    await using (stream.ConfigureAwait(false))
    {
      try
      {
        return await JsonSerializer.DeserializeAsync(stream, typeInfo, cancellationToken).ConfigureAwait(false);
      }
      catch (JsonException ex)
      {
        throw new PackageFeedUnreadableException($"GitHub Packages returned HTTP {(int)response.StatusCode} with a non-JSON body from {url}: {ex.Message}", ex, response.StatusCode);
      }
    }
  }

  public static int CompareVersions(string version1, string version2)
  {
    ArgumentNullException.ThrowIfNull(version1);
    ArgumentNullException.ThrowIfNull(version2);

    string core1 = GetCoreVersion(version1);
    string pre1 = GetPreRelease(version1);
    string core2 = GetCoreVersion(version2);
    string pre2 = GetPreRelease(version2);

    int coreCmp = CompareCoreVersions(core1, core2);
    if (coreCmp != 0)
    {
      return coreCmp;
    }

    // SemVer §11.3: a version WITHOUT pre-release has HIGHER precedence.
    bool hasPre1 = pre1.Length > 0;
    bool hasPre2 = pre2.Length > 0;
    if (!hasPre1 && !hasPre2)
    {
      return 0;
    }

    if (!hasPre1)
    {
      return 1;
    }

    if (!hasPre2)
    {
      return -1;
    }

    return ComparePreRelease(pre1, pre2);
  }

  /// <summary>
  /// Checks whether <paramref name="sourceVersion"/> matches any entry in
  /// <paramref name="publishedVersions"/> using full SemVer 2.0 precedence
  /// (build metadata stripped, case-insensitive). Replaces the old
  /// <c>versions[^1]</c>-only membership check that missed non-latest duplicates.
  /// </summary>
  public static bool IsVersionPublished(string sourceVersion, IEnumerable<string> publishedVersions)
  {
    ArgumentNullException.ThrowIfNull(sourceVersion);
    ArgumentNullException.ThrowIfNull(publishedVersions);

    foreach (string published in publishedVersions)
    {
      if (CompareVersions(sourceVersion, published) == 0)
      {
        return true;
      }
    }

    return false;
  }

  /// <summary>Strips build metadata (+...) and returns the core (pre-`-`/pre-`+`) portion.</summary>
  /// <param name="version">The version string.</param>
  private static string GetCoreVersion(string version)
  {
    int plus = version.IndexOf('+');
    string noBuild = plus >= 0 ? version[..plus] : version;
    int dash = noBuild.IndexOf('-');
    return dash >= 0 ? noBuild[..dash] : noBuild;
  }

  /// <summary>Strips build metadata and returns the pre-release portion (after first `-`).</summary>
  /// <param name="version">The version string.</param>
  private static string GetPreRelease(string version)
  {
    int plus = version.IndexOf('+');
    string noBuild = plus >= 0 ? version[..plus] : version;
    int dash = noBuild.IndexOf('-');
    return dash >= 0 ? noBuild[(dash + 1)..] : "";
  }

  private static int CompareCoreVersions(string core1, string core2)
  {
    string[] parts1 = core1.Split('.');
    string[] parts2 = core2.Split('.');
    int maxLen = Math.Max(parts1.Length, parts2.Length);

    for (int i = 0; i < maxLen; i++)
    {
      // Tolerate missing/extra parts as 0 (NuGet normalization: 1.0 == 1.0.0 == 1.0.0.0)
      int v1 = i < parts1.Length && int.TryParse(parts1[i], out int p1) ? p1 : 0;
      int v2 = i < parts2.Length && int.TryParse(parts2[i], out int p2) ? p2 : 0;

      int cmp = v1.CompareTo(v2);
      if (cmp != 0)
      {
        return cmp;
      }
    }

    return 0;
  }

  private static int ComparePreRelease(string pre1, string pre2)
  {
    string[] ids1 = pre1.Split('.');
    string[] ids2 = pre2.Split('.');
    int maxLen = Math.Max(ids1.Length, ids2.Length);

    for (int i = 0; i < maxLen; i++)
    {
      // §11.4.4: a larger set of pre-release fields has HIGHER precedence (prefix rule).
      if (i >= ids1.Length)
      {
        return -1;
      }

      if (i >= ids2.Length)
      {
        return 1;
      }

      string id1 = ids1[i];
      string id2 = ids2[i];
      bool isNum1 = IsNumeric(id1);
      bool isNum2 = IsNumeric(id2);

      int cmp;
      if (isNum1 && isNum2)
      {
        // §11.4.1: numeric identifier compared numerically (beta.9 < beta.10).
        // Parse-free: date-stamped identifiers (1.0.0-ci.20260707191500) overflow
        // Int32, and the gate iterates EVERY published version, so one odd entry
        // in feed history must not crash it. Trim leading zeros, then compare by
        // length and ordinal digits — equivalent to arbitrary-precision compare.
        cmp = CompareNumericIdentifiers(id1, id2);
      }
      else if (!isNum1 && !isNum2)
      {
        // §11.4.2: alphanumeric compared lexically, case-insensitive (NuGet normalization).
        cmp = string.Compare(id1, id2, StringComparison.OrdinalIgnoreCase);
      }
      else
      {
        // §11.4.3: numeric identifiers have LOWER precedence than alphanumeric.
        cmp = isNum1 ? -1 : 1;
      }

      if (cmp != 0)
      {
        return cmp;
      }
    }

    return 0;
  }

  private static int CompareNumericIdentifiers(string id1, string id2)
  {
    string trimmed1 = id1.TrimStart('0');
    string trimmed2 = id2.TrimStart('0');

    if (trimmed1.Length != trimmed2.Length)
    {
      return trimmed1.Length.CompareTo(trimmed2.Length);
    }

    return string.CompareOrdinal(trimmed1, trimmed2);
  }

  private static bool IsNumeric(string identifier)
  {
    // Empty identifiers (malformed input like "1.0.0-") are treated as numeric zero
    // rather than crashing; the gate must tolerate garbage in feed history.
    foreach (char c in identifier)
    {
      if (!char.IsDigit(c))
      {
        return false;
      }
    }

    return identifier.Length > 0;
  }

  public void Dispose()
  {
    HttpClient.Dispose();
    Handler.Dispose();
  }
}

internal sealed class NuGetVersionIndex
{
  [JsonPropertyName("versions")]
  public IReadOnlyList<string> Versions { get; init; } = [];
}

/// <summary>A NuGet V3 service index; only the resources list is read.</summary>
internal sealed class NuGetServiceIndex
{
  [JsonPropertyName("resources")]
  public IReadOnlyList<NuGetServiceIndexResource>? Resources { get; init; }
}

internal sealed class NuGetServiceIndexResource
{
  [JsonPropertyName("@id")]
  public string? Id { get; init; }

  [JsonPropertyName("@type")]
  public string? Type { get; init; }
}
