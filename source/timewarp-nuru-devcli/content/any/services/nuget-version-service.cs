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

    string[] parts1 = version1.Split('.');
    string[] parts2 = version2.Split('.');

    int maxLen = Math.Max(parts1.Length, parts2.Length);

    for (int i = 0; i < maxLen; i++)
    {
      int v1 = i < parts1.Length && int.TryParse(parts1[i], out int p1) ? p1 : 0;
      int v2 = i < parts2.Length && int.TryParse(parts2[i], out int p2) ? p2 : 0;

      int cmp = v1.CompareTo(v2);
      if (cmp != 0)
      {
        return cmp;
      }
    }

    return string.Compare(version1, version2, StringComparison.OrdinalIgnoreCase);
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
