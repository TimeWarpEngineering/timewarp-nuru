namespace TimeWarp.Nuru;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a GitHub release from the GitHub API.
/// Contains only the fields needed for version checking.
/// </summary>
/// <param name="TagName">The release tag (e.g., "v1.2.0" or "1.2.0-beta.5").</param>
/// <param name="Prerelease">Whether this is a pre-release version.</param>
/// <param name="PublishedAt">The date and time when the release was published.</param>
/// <param name="HtmlUrl">The URL to view the release on GitHub.</param>
internal sealed record GitHubRelease(
  [property: JsonPropertyName("tag_name")] string TagName,
  [property: JsonPropertyName("prerelease")] bool Prerelease,
  [property: JsonPropertyName("published_at")] DateTime? PublishedAt,
  [property: JsonPropertyName("html_url")] string HtmlUrl);

/// <summary>
/// JSON serialization context for GitHub API responses.
/// Uses source generation for AOT compatibility.
/// </summary>
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRelease[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class GitHubJsonSerializerContext : JsonSerializerContext;
