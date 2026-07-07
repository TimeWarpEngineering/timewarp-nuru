#region Purpose
// HttpClient-based NuGet version checking — replaces TimeWarp.Amuru's NuGetPackageService
// which depends on NuGet.Protocol/NuGet.Packaging (pulls in Newtonsoft.Json, incompatible with AOT).
// Uses the NuGet.org V3 Flat Container API directly with System.Text.Json.
#endregion
#region Design
// Calls https://api.nuget.org/v3-flatcontainer/{id}/index.json
// which returns a simple JSON object with a "versions" array.
// No NuGet.Protocol, no NuGet.Packaging, no Newtonsoft.Json — fully AOT-compatible.
#endregion

namespace DevCli;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class NuGetVersionService : IDisposable
{
  private readonly HttpClient HttpClient;

  public NuGetVersionService()
  {
    HttpClient = new HttpClient
    (
      new HttpClientHandler
      {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
      }
    );
  }

  public async Task<IReadOnlyList<string>> GetPackageVersionsAsync
  (
    string packageId,
    CancellationToken cancellationToken
  )
  {
    ArgumentNullException.ThrowIfNull(packageId);

    Uri url = new($"https://api.nuget.org/v3-flatcontainer/{packageId.ToLowerInvariant()}/index.json");
    HttpResponseMessage response = await HttpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

    if (!response.IsSuccessStatusCode)
    {
      return [];
    }

    Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    await using (stream.ConfigureAwait(false))
    {
      NuGetVersionIndex? index = await JsonSerializer
        .DeserializeAsync(stream, DevCliJsonContext.Default.NuGetVersionIndex, cancellationToken)
        .ConfigureAwait(false);

      return index?.Versions ?? [];
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
        cmp = int.Parse(id1, CultureInfo.InvariantCulture).CompareTo(int.Parse(id2, CultureInfo.InvariantCulture));
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

  private static bool IsNumeric(string identifier)
  {
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
