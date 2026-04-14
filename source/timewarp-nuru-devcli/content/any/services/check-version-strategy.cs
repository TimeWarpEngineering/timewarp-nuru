#region Purpose
// Defines the available version checking strategies for dev-cli commands.
// Replaces magic strings ("git-tag", "nuget-search") with a type-safe enum.
#endregion

namespace DevCli;

using System.Text.Json.Serialization;

/// <summary>
/// Strategy for checking whether a version has already been released.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<CheckVersionStrategy>))]
public enum CheckVersionStrategy
{
  /// <summary>
  /// Compare the current version against the latest GitHub release tag.
  /// </summary>
  [JsonStringEnumMemberName("git-tag")]
  GitTag,

  /// <summary>
  /// Search NuGet.org for the current version of one or more packages.
  /// </summary>
  [JsonStringEnumMemberName("nuget-search")]
  NuGetSearch
}