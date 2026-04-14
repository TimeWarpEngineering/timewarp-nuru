#region Purpose
// Top-level configuration model for .timewarp/dev.jsonc.
// Provides per-repo settings for dev-cli commands.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration deserialized from <c>.timewarp/dev.jsonc</c>.
/// </summary>
public sealed class RepoConfig
{
  /// <summary>
  /// Configuration for the check-version command.
  /// </summary>
  public CheckVersionConfig? CheckVersionConfig { get; init; }
}