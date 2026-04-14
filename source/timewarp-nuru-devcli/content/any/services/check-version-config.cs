#region Purpose
// Configuration model for the check-version command.
// Deserialized from .timewarp/dev.jsonc under the "CheckVersion" key.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration for version checking.
/// </summary>
public sealed class CheckVersionConfig
{
  /// <summary>
  /// The strategy to use when checking versions.
  /// Defaults to <see cref="CheckVersionStrategy.NuGetSearch"/> if not specified.
  /// </summary>
  public CheckVersionStrategy CheckVersionStrategy { get; init; } = CheckVersionStrategy.NuGetSearch;

  /// <summary>
  /// Comma-separated NuGet package IDs to check.
  /// Only used with <see cref="CheckVersionStrategy.NuGetSearch"/>.
  /// </summary>
  public string? Packages { get; init; }
}