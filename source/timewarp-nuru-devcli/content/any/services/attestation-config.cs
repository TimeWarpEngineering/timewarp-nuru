#region Purpose
// Configuration model for the ganda-audit attestation verify step
// (kanban task 458-010). Deserialized from .timewarp/dev.jsonc under the
// "attestation" key. AttestationConfigResolver.ResolveMode is the pure,
// testable piece of mode interpretation — kept out of workflow-command.cs
// so a typo'd mode value has unit-test coverage instead of only being
// exercisable end-to-end.
#endregion
#region Design
// Mode has exactly two meaningful values, both case-insensitive:
//   - "warn" (default, including when Mode is null/absent/blank):
//     `dev workflow --mode pr` prints a loud advisory when the tree is not
//     Valid but does not fail the pipeline. Nothing is enforced org-wide
//     until repos opt in.
//   - "require": a non-Valid outcome fails `dev workflow --mode pr`
//     (Environment.ExitCode = 1, pipeline aborted).
// Release mode ignores this setting entirely — the attestation gate is
// ALWAYS a hard, unconditional check in release mode regardless of config
// (see workflow-command.cs RunReleaseWorkflowAsync), because a release
// with no verifiable audit evidence must never ship, config or no config.
//
// Unrecognized values (round-1 review Fix 3): a blank/absent Mode silently
// resolves to Warn — that is the documented default, not a mistake. But a
// NON-blank value that is neither "warn" nor "require" (a typo like
// "requiree") is a real operator error: falling back to Warn silently would
// leave the operator believing enforcement is on when it is not.
// AttestationModeResolution.UnrecognizedValue carries the original
// (trimmed) string in that case so the caller can print exactly one
// warning line naming it, while STILL falling back to Warn (fail-open on
// PR/merge-mode config parsing, not fail-closed — the hard release-mode
// gate is what actually enforces attestation; see the Purpose region
// above). UnrecognizedValue is null for every other input (blank/absent,
// "warn", "require" in any casing) — that null-ness is itself the "should
// I warn" signal, not a separate flag.
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

/// <summary>Resolved attestation mode for PR/merge mode (see attestation-config.cs Design region).</summary>
public enum AttestationMode
{
  Warn,
  Require
}

/// <summary>
/// Result of <see cref="AttestationConfigResolver.ResolveMode"/>:
/// the mode to actually use, plus (when non-null) the unrecognized raw
/// value the caller should warn about before falling back to
/// <see cref="AttestationMode.Warn"/>.
/// </summary>
public readonly record struct AttestationModeResolution(AttestationMode Mode, string? UnrecognizedValue);

/// <summary>
/// Pure resolver for <c>attestation.mode</c> — no process execution, no I/O.
/// </summary>
public static class AttestationConfigResolver
{
  /// <summary>
  /// Resolve a raw <c>attestation.mode</c> config value. Blank/absent
  /// resolves to <see cref="AttestationMode.Warn"/> with no warning;
  /// "warn"/"require" (case-insensitive) resolve exactly; anything else
  /// resolves to <see cref="AttestationMode.Warn"/> WITH
  /// <see cref="AttestationModeResolution.UnrecognizedValue"/> set to the
  /// trimmed raw value, so the caller can surface a one-line warning.
  /// </summary>
  public static AttestationModeResolution ResolveMode(string? rawMode)
  {
    if (string.IsNullOrWhiteSpace(rawMode))
    {
      return new AttestationModeResolution(AttestationMode.Warn, null);
    }

    string trimmed = rawMode.Trim();

    if (string.Equals(trimmed, "require", StringComparison.OrdinalIgnoreCase))
    {
      return new AttestationModeResolution(AttestationMode.Require, null);
    }

    if (string.Equals(trimmed, "warn", StringComparison.OrdinalIgnoreCase))
    {
      return new AttestationModeResolution(AttestationMode.Warn, null);
    }

    return new AttestationModeResolution(AttestationMode.Warn, trimmed);
  }
}
