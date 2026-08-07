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
  /// Comma-separated NuGet package IDs to check.
  /// </summary>
  public string? Packages { get; init; }
}