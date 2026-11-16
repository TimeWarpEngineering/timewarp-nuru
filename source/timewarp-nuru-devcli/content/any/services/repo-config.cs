#region Purpose
// Top-level configuration model for .timewarp/dev.jsonc.
// Provides per-repo settings for dev-cli commands.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration deserialized from <c>.timewarp/dev.jsonc</c>.
/// </summary>
internal sealed class RepoConfig
{
  /// <summary>
  /// Configuration for the check-version command.
  /// </summary>
  public CheckVersionConfig? CheckVersionConfig { get; init; }

  /// <summary>
  /// Configuration for the ganda-audit attestation verify step (kanban task 458-010).
  /// </summary>
  public AttestationConfig? Attestation { get; set; }

  /// <summary>
  /// Repo additions to the pull request build rule (kanban task 595). See <see cref="PullRequestBuildClassifier"/>.
  /// </summary>
  public PullRequestBuildConfig? PullRequestBuild { get; init; }
}
