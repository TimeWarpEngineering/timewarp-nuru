#region Purpose
// Configuration model for the ganda-audit attestation verify step
// (kanban tasks 458-010, 458-011). Deserialized from .timewarp/dev.jsonc under
// the "attestation" key. AttestationConfigResolver is the pure, testable piece
// of mode interpretation — kept out of workflow-command.cs so a typo'd mode
// value has unit-test coverage instead of only being exercisable end-to-end.
// ResolveMode never applies a context default; EffectiveMode does, so PR/merge
// and release can share one resolver with different unset defaults.
#endregion
#region Design
// Mode has three meaningful values, all case-insensitive, one key for every
// pipeline mode (no separate prMode/releaseMode):
//   - "off": skip verify entirely (PR and release). One log line; no advisory
//     spam. For portable/no-ganda consumers and break-glass CLI override.
//   - "warn": run verify; non-Valid prints a loud advisory but never aborts
//     (PR may RepeatAdvisory before SUCCEEDED; release logs once).
//   - "require": run verify; non-Valid aborts the pipeline.
//
// Context-sensitive defaults when mode is blank/absent (TimeWarp-first):
//   - PR/merge: DefaultPrMode = Warn (nothing enforced org-wide until opt-in)
//   - release:  DefaultReleaseMode = Require (safe default for TimeWarp repos
//     that never set the key — preserves pre-458-011 release hard-gate
//     behavior without every repo editing jsonc)
// Blank is NOT the same as explicit "warn": release blank → Require, while
// explicit "warn" → advisory on release too. That is why ResolveMode returns
// Mode = null for blank/absent and callers pass whenUnset to EffectiveMode.
//
// Unrecognized non-blank values (typos like "requiree"): Mode stays null so
// EffectiveMode still applies the context default — PR fail-open to Warn,
// release fail-closed to Require. UnrecognizedValue carries the trimmed raw
// string so the caller prints one warning naming it. Never silently Off
// (would disable the release gate by accident). Valid values message lists
// off, warn, require.
//
// CLI --attestation off|warn|require overrides config for that run only
// (precedence: CLI > config > context default). Empty CLI still overrides
// config (is not null check) and then resolves as blank → context default.
//
// Verify I/O (git notes, openssl) stays in workflow-command.cs; this file is
// pure mode policy only — no change to ganda signing or note format.
#endregion

namespace DevCli;

/// <summary>
/// Per-repo configuration for the attestation verify step.
/// </summary>
public sealed class AttestationConfig
{
  /// <summary>
  /// <c>off</c>, <c>warn</c>, or <c>require</c>. Blank/absent uses a
  /// context-sensitive default (PR/merge: warn; release: require). See
  /// <see cref="AttestationConfigResolver"/>.
  /// </summary>
  public string? Mode { get; set; }
}

/// <summary>
/// Resolved attestation enforcement level for a pipeline run.
/// </summary>
public enum AttestationMode
{
  /// <summary>Skip attestation verify entirely.</summary>
  Off,

  /// <summary>Verify; non-Valid is advisory only (no abort).</summary>
  Warn,

  /// <summary>Verify; non-Valid aborts the pipeline.</summary>
  Require
}

/// <summary>
/// Result of <see cref="AttestationConfigResolver.ResolveMode"/>:
/// <see cref="Mode"/> is null when the raw value is blank/absent or
/// unrecognized; <see cref="UnrecognizedValue"/> is non-null only for an
/// unrecognized non-blank raw value (callers should warn, then apply
/// <see cref="AttestationConfigResolver.EffectiveMode"/>).
/// </summary>
public readonly record struct AttestationModeResolution(
  AttestationMode? Mode,
  string? UnrecognizedValue);

/// <summary>
/// Pure resolver for <c>attestation.mode</c> — no process execution, no I/O.
/// </summary>
public static class AttestationConfigResolver
{
  /// <summary>Default when mode is unset in PR/merge pipeline mode.</summary>
  public const AttestationMode DefaultPrMode = AttestationMode.Warn;

  /// <summary>Default when mode is unset in release pipeline mode.</summary>
  public const AttestationMode DefaultReleaseMode = AttestationMode.Require;

  /// <summary>
  /// Resolve a raw <c>attestation.mode</c> (or CLI override) value.
  /// Blank/absent → <c>(null, null)</c>;
  /// <c>off</c>/<c>warn</c>/<c>require</c> (case-insensitive, trimmed) →
  /// <c>(enum, null)</c>;
  /// any other non-blank value → <c>(null, trimmed)</c> so the caller can
  /// surface a typo warning and apply a context default via
  /// <see cref="EffectiveMode"/> (never silently <see cref="AttestationMode.Off"/>).
  /// </summary>
  public static AttestationModeResolution ResolveMode(string? rawMode)
  {
    if (string.IsNullOrWhiteSpace(rawMode))
    {
      return new AttestationModeResolution(null, null);
    }

    string trimmed = rawMode.Trim();

    if (string.Equals(trimmed, "off", StringComparison.OrdinalIgnoreCase))
    {
      return new AttestationModeResolution(AttestationMode.Off, null);
    }

    if (string.Equals(trimmed, "warn", StringComparison.OrdinalIgnoreCase))
    {
      return new AttestationModeResolution(AttestationMode.Warn, null);
    }

    if (string.Equals(trimmed, "require", StringComparison.OrdinalIgnoreCase))
    {
      return new AttestationModeResolution(AttestationMode.Require, null);
    }

    return new AttestationModeResolution(null, trimmed);
  }

  /// <summary>
  /// Apply a context-sensitive default when <see cref="AttestationModeResolution.Mode"/>
  /// is null (blank/absent or unrecognized raw input).
  /// </summary>
  public static AttestationMode EffectiveMode(
    AttestationModeResolution resolution,
    AttestationMode whenUnset)
    => resolution.Mode ?? whenUnset;
}
