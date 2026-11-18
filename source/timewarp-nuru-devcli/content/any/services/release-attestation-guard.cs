#region Purpose
// Pure verdict for the `dev release` attestation guard (kanban task 592): turns the
// effective attestation mode and the verify outcome for the release target's tree into
// pass / warn / refuse / skipped with the operator-facing lines. No process execution;
// release-command.guards.cs runs AttestationVerifyRunner and prints these lines.
#endregion
#region Design
// Mode: the same effective mode the `release: published` job uses, so a local pass
// means the job's attestation gate passes too. That job runs `dev workflow` with no
// --attestation flag, so the mode is `.timewarp/dev.jsonc` attestation.mode with the
// release default (AttestationConfigResolver.DefaultReleaseMode = require) when unset,
// and a typo also falls back to require (ResolveReleaseMode).
//
// Off: the caller does not verify at all; the verdict is Skipped with one line.
// Valid: Pass, `✓ target tree <sha12> attested (key_id=…)`.
// Anything else: Refuse under require, Warn under warn, with the same block — target
// commit, its tree, the status name, the detail when there is one, and the remedy.
//
// Remedy by status: UnknownKey → update TimeWarp.Nuru.DevCli (the key registry is
// baked into this package); VerifierUnavailable → install openssl; every other
// status (NoNote, RefMissing, ParseFailure, TreeMismatch, BadSignature) → attest the
// target's tree with `ganda repo attest` (which pushes refs/notes/ganda-audit). When
// the target is HEAD that is pull master and attest. When the target is behind HEAD
// (kanban-only commits after it, task 543), HEAD's tree is a different tree, so the
// remedy attests from a scratch branch at the target sha tracking origin/master and
// deletes the branch after — the manual recovery used for 4.0.0-beta.13 to beta.15.
#endregion

namespace DevCli;

/// <summary>What the release attestation guard decided.</summary>
internal enum ReleaseAttestationGuardKind
{
  /// <summary>attestation.mode is off: nothing verified.</summary>
  Skipped,

  /// <summary>The target tree has a valid note.</summary>
  Pass,

  /// <summary>Not attested, mode warn: print the block and continue.</summary>
  Warn,

  /// <summary>Not attested, mode require: print the block and refuse.</summary>
  Refuse
}

/// <summary>
/// Verdict of <see cref="ReleaseAttestationGuard.Check"/>: the kind and the lines to print,
/// in order.
/// </summary>
internal sealed record ReleaseAttestationGuardVerdict(ReleaseAttestationGuardKind Kind, IReadOnlyList<string> Lines);

/// <summary>Pure classifier for the `dev release` attestation guard. See the Design region.</summary>
internal static class ReleaseAttestationGuard
{
  /// <summary>
  /// The effective release-mode attestation policy for a raw <c>attestation.mode</c>
  /// value, plus a warning line when the raw value is an unrecognized non-blank string.
  /// </summary>
  public static (AttestationMode Mode, string? Warning) ResolveReleaseMode(string? rawMode)
  {
    AttestationModeResolution resolution = AttestationConfigResolver.ResolveMode(rawMode);
    AttestationMode mode = AttestationConfigResolver.EffectiveMode(resolution, AttestationConfigResolver.DefaultReleaseMode);

    string? warning = resolution.UnrecognizedValue is null
      ? null
      : $"Warning: unrecognized attestation.mode '{resolution.UnrecognizedValue}' — treating as 'require'. Valid values: off, warn, require.";

    return (mode, warning);
  }

  /// <summary>
  /// Classifies the verify outcome for the release target. <paramref name="outcome"/> may be
  /// <see langword="null"/> only when <paramref name="mode"/> is <see cref="AttestationMode.Off"/>.
  /// </summary>
  public static ReleaseAttestationGuardVerdict Check
  (
    AttestationMode mode,
    string targetSha,
    string headSha,
    AttestationVerifyOutcome? outcome
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(targetSha);
    ArgumentException.ThrowIfNullOrWhiteSpace(headSha);

    if (mode == AttestationMode.Off)
    {
      return new ReleaseAttestationGuardVerdict(ReleaseAttestationGuardKind.Skipped, ["attestation skipped (attestation.mode=off)"]);
    }

    ArgumentNullException.ThrowIfNull(outcome);

    if (outcome.Status == AttestationVerificationStatus.Valid)
    {
      return new ReleaseAttestationGuardVerdict(
        ReleaseAttestationGuardKind.Pass,
        [$"✓ target tree {Sha12(outcome.Tree)} attested (key_id={outcome.KeyId ?? "(unknown)"})"]);
    }

    bool targetIsHead = string.Equals(targetSha, headSha, StringComparison.Ordinal);
    List<string> lines = [];

    if (mode == AttestationMode.Require)
    {
      lines.Add($"release target {Sha12(targetSha)} is not attested ({outcome.Status}) — refusing before the tag.");
    }
    else
    {
      lines.Add($"release target {Sha12(targetSha)} is not attested ({outcome.Status}) — attestation.mode=warn, continuing.");
    }

    lines.Add($"  commit: {targetSha}");
    lines.Add($"  tree:   {outcome.Tree ?? "(unknown)"}");
    lines.Add($"  status: {outcome.Status}");

    if (!string.IsNullOrWhiteSpace(outcome.Detail))
    {
      lines.Add($"  detail: {outcome.Detail}");
    }

    if (mode == AttestationMode.Require)
    {
      lines.Add("  The release: published job verifies this tree with attestation.mode=require and would abort after the tag and GitHub Release exist.");
    }

    lines.AddRange(Remedy(outcome, targetSha, targetIsHead));

    ReleaseAttestationGuardKind kind = mode == AttestationMode.Require
      ? ReleaseAttestationGuardKind.Refuse
      : ReleaseAttestationGuardKind.Warn;

    return new ReleaseAttestationGuardVerdict(kind, lines);
  }

  private static IEnumerable<string> Remedy(AttestationVerifyOutcome outcome, string targetSha, bool targetIsHead)
  {
    switch (outcome.Status)
    {
      case AttestationVerificationStatus.UnknownKey:
        yield return $"  Remedy: update TimeWarp.Nuru.DevCli so its key registry knows key_id '{outcome.KeyId ?? "(unknown)"}', then rerun dev release.";
        yield break;

      case AttestationVerificationStatus.VerifierUnavailable:
        yield return "  Remedy: install openssl, then rerun dev release.";
        yield break;
    }

    if (targetIsHead)
    {
      yield return "  Remedy: HEAD is the target. Pull master, attest, rerun:";
      yield return "    git pull --ff-only";
      yield return "    ganda repo attest";
      yield return "    dev release";
      yield break;
    }

    string branch = $"attest-{Sha12(targetSha)}";
    yield return "  Remedy: the target is behind HEAD, so HEAD's tree is not the target's tree. Attest the target from a scratch branch:";
    yield return $"    git switch -c {branch} {targetSha}";
    yield return "    git branch --set-upstream-to=origin/master";
    yield return "    ganda repo attest";
    yield return $"    git switch master && git branch -D {branch}";
    yield return "    dev release";
  }

  private static string Sha12(string? sha)
  {
    if (string.IsNullOrEmpty(sha))
    {
      return "(unknown)";
    }

    return sha.Length > 12 ? sha[..12] : sha;
  }
}
