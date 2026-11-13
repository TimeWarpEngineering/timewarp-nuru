#region Purpose
// GitHub Packages half of NuGetVersionService: credential, base address discovery, requests and JSON reads.
#endregion

namespace DevCli;

using System.Text.Json;

internal sealed partial class NuGetVersionService
{
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
}
