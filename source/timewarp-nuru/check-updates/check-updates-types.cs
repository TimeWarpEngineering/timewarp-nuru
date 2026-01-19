namespace TimeWarp.Nuru;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

/// <summary>
/// GitHub release information for check-updates functionality.
/// </summary>
/// <remarks>
/// This is a DTO for JSON deserialization from GitHub's API.
/// HtmlUrl is kept as string to match the API response format.
/// </remarks>
[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings", Justification = "JSON DTO from GitHub API")]
[SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "JSON DTO from GitHub API")]
public sealed record CheckUpdatesGitHubRelease(
  [property: JsonPropertyName("tag_name")] string TagName,
  [property: JsonPropertyName("prerelease")] bool Prerelease,
  [property: JsonPropertyName("published_at")] DateTime? PublishedAt,
  [property: JsonPropertyName("html_url")] string HtmlUrl);

/// <summary>
/// JSON serialization context for check-updates with source generation support.
/// </summary>
[JsonSerializable(typeof(CheckUpdatesGitHubRelease))]
[JsonSerializable(typeof(CheckUpdatesGitHubRelease[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class CheckUpdatesJsonSerializerContext : JsonSerializerContext;
