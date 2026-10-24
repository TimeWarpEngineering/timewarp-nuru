#region Purpose
// Runtime types behind the generated --check-updates handler (AddCheckUpdatesRoute): the GitHub
// release DTO and the System.Text.Json source-generated context that deserializes it.
#endregion

#region Design
// These live in the library, not in generated code, because source generators cannot chain: the
// JSON generator never sees types the Nuru generator emits (#358). Generated code runs in the
// consumer's assembly and can only use public surface, so it calls the public
// CheckUpdatesGitHubRelease.ParseReleases and never names CheckUpdatesJsonSerializerContext, which
// stays internal (kanban 528: naming it caused CS0122 in every consumer).
#endregion

namespace TimeWarp.Nuru;

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
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
  [property: JsonPropertyName("html_url")] string HtmlUrl)
{
  /// <summary>
  /// Deserializes a GitHub releases API response. Generated <c>--check-updates</c> code calls this.
  /// </summary>
  /// <param name="json">The JSON array returned by <c>GET /repos/{owner}/{repo}/releases</c>.</param>
  /// <returns>The releases, or an empty array when the payload is <c>null</c>.</returns>
  /// <exception cref="JsonException">The payload is not a valid releases array.</exception>
  [EditorBrowsable(EditorBrowsableState.Never)]
  public static CheckUpdatesGitHubRelease[] ParseReleases(string json) =>
    JsonSerializer.Deserialize(json, CheckUpdatesJsonSerializerContext.Default.CheckUpdatesGitHubReleaseArray) ?? [];
}

/// <summary>
/// JSON serialization context for check-updates with source generation support.
/// </summary>
[JsonSerializable(typeof(CheckUpdatesGitHubRelease))]
[JsonSerializable(typeof(CheckUpdatesGitHubRelease[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
internal partial class CheckUpdatesJsonSerializerContext : JsonSerializerContext;
