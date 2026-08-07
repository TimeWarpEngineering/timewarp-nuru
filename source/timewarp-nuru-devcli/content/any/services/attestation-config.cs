#region Purpose
// Configuration model for the ganda-audit attestation verify step
// (kanban task 458-010). Deserialized from .timewarp/dev.jsonc under the
// "attestation" key.
#endregion
#region Design
// Mode has exactly two meaningful values, both case-insensitive:
//   - "warn" (default, including when Mode is null/absent/blank/unrecognized):
//     `dev workflow --mode pr` prints a loud advisory when the tree is not
//     Valid but does not fail the pipeline. Nothing is enforced org-wide
//     until repos opt in.
//   - "require": a non-Valid outcome fails `dev workflow --mode pr`
//     (Environment.ExitCode = 1, pipeline aborted).
// Release mode ignores this setting entirely — the attestation gate is
// ALWAYS a hard, unconditional check in release mode regardless of config
// (see workflow-command.cs RunReleaseWorkflowAsync), because a release
// with no verifiable audit evidence must never ship, config or no config.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration for the attestation verify step in PR/merge mode.
/// </summary>
public sealed class AttestationConfig
{
  /// <summary>
  /// "warn" (default) or "require". Only consulted in PR/merge mode —
  /// release mode always hard-gates regardless of this value.
  /// </summary>
  public string? Mode { get; set; }
}
