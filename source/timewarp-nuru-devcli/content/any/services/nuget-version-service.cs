#region Purpose
// HttpClient-based NuGet version checking — replaces TimeWarp.Amuru's NuGetPackageService
// which depends on NuGet.Protocol/NuGet.Packaging (pulls in Newtonsoft.Json, incompatible with AOT).
// Uses the NuGet.org V3 Flat Container API directly with System.Text.Json.
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
#endregion

namespace DevCli;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class NuGetVersionService : IDisposable
{
  private readonly HttpClient HttpClient;

  public const int MaxPackageIdLength = 100;

  public NuGetVersionService()
    : this
    (
      new HttpClientHandler
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
      }
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
  {
    ArgumentNullException.ThrowIfNull(handler);
    HttpClient = new HttpClient(handler);
  }

  /// <summary>
  /// Builds the flat-container index URL for <paramref name="packageId"/>. Throws
  /// <see cref="ArgumentException"/> when the id fails <see cref="IsValidPackageId"/>.
  /// </summary>
  public static Uri GetIndexUrl(string packageId)
  {
    ArgumentNullException.ThrowIfNull(packageId);

    if (!IsValidPackageId(packageId))
    {
      throw new ArgumentException($"'{packageId}' is not a valid NuGet package id (letters, digits, '_', with single '.' or '-' separators; max {MaxPackageIdLength} chars).", nameof(packageId));
    }

    string segment = Uri.EscapeDataString(packageId.ToLowerInvariant());
    return new Uri($"https://api.nuget.org/v3-flatcontainer/{segment}/index.json");
  }

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

  // Strips build metadata (+...) and returns the core (pre-`-`/pre-`+`) portion.
  private static string GetCoreVersion(string version)
  {
    int plus = version.IndexOf('+');
    string noBuild = plus >= 0 ? version[..plus] : version;
    int dash = noBuild.IndexOf('-');
    return dash >= 0 ? noBuild[..dash] : noBuild;
  }

  // Strips build metadata and returns the pre-release portion (after first `-`).
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
  }
}

public sealed class NuGetVersionIndex
{
  [JsonPropertyName("versions")]
  public IReadOnlyList<string> Versions { get; init; } = [];
}
