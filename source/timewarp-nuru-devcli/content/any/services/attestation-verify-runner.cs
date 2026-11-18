#region Purpose
// Process half of the ganda-audit attestation verify (kanban tasks 458-010, 592): fetch
// refs/notes/ganda-audit, resolve a commit's tree, read its note, run the pure
// AttestationVerifier.Evaluate and, when it says ReadyToVerify, the openssl Ed25519
// check. One implementation for `dev workflow` (repo-local, HEAD) and `dev release`
// (the selected release target), so the local guard and the release job agree.
#endregion
#region Design
// Moved out of tools/dev-cli/endpoints/workflow-command.attestation.cs (task 592).
// The tree is resolved from a commit-ish (`<commitish>^{tree}`), not hard-wired to
// HEAD: `dev release` may tag a commit behind HEAD (kanban-only walk, task 543), and
// a note on HEAD's tree must not satisfy that target.
//
// Never throws for an attestation-shaped failure: every branch returns a distinct
// AttestationVerifyOutcome status. Only an unresolvable `<commitish>^{tree}` throws
// InvalidOperationException (a broken checkout, not an attestation verdict).
//
// RefMissing vs NoNote: the fetch and notes-show calls match English stderr
// ("couldn't find remote ref", "no note found"); both pin LC_ALL=C and LANG=C on the
// child process so the match is locale-stable. Either verdict is fail-closed; this
// only sharpens the message. The fetch is best-effort: a repo with no origin (a test
// fixture) or no notes ref on origin still reads local notes.
//
// openssl: no pure-.NET Ed25519 verify exists in the BCL (attestation-verifier.cs
// Design region), so the signature check shells out to
// `openssl pkeyutl -verify -pubin -rawin`. A missing binary is VerifierUnavailable.
//
// keyOverride is the test seam AttestationVerifier.Evaluate already has: production
// callers omit it; tests pass a throwaway key generated with openssl.
#endregion

namespace DevCli;

/// <summary>
/// Outcome of <see cref="AttestationVerifyRunner.VerifyAsync"/>: any
/// <see cref="AttestationVerificationStatus"/> except
/// <see cref="AttestationVerificationStatus.ReadyToVerify"/>, the tree checked, and the
/// note fields parsed far enough to cite (null when no note was parsed).
/// </summary>
internal sealed record AttestationVerifyOutcome(
  AttestationVerificationStatus Status,
  string? Tree,
  string? CheckSet,
  string? Ts,
  string? Detail,
  string? KeyId = null);

/// <summary>
/// Runs the git/openssl steps of the attestation verify around the pure
/// <see cref="AttestationVerifier"/>.
/// </summary>
internal static class AttestationVerifyRunner
{
  /// <summary>
  /// Verifies the ganda-audit note on the tree of <paramref name="commitish"/> in the
  /// repository at <paramref name="repoRoot"/>.
  /// </summary>
  /// <exception cref="InvalidOperationException">The tree of <paramref name="commitish"/> cannot be resolved.</exception>
  public static async Task<AttestationVerifyOutcome> VerifyAsync
  (
    string repoRoot,
    string commitish,
    IReadOnlyDictionary<string, string>? keyOverride = null,
    CancellationToken cancellationToken = default
  )
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(repoRoot);
    ArgumentException.ThrowIfNullOrWhiteSpace(commitish);

    // (a) Best-effort forced fetch of the notes ref. Exit 128 with "couldn't find
    // remote ref" means origin has never been attested: RefMissing below, not a failure.
    CommandOutput fetchResult = await Shell.Builder("git")
      .WithArguments("fetch", "origin", $"+{AttestationVerifier.NotesRef}:{AttestationVerifier.NotesRef}")
      .WithWorkingDirectory(repoRoot)
      .WithEnvironmentVariable("LC_ALL", "C")
      .WithEnvironmentVariable("LANG", "C")
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    bool remoteNotesRefMissing = fetchResult.ExitCode == 128
      && fetchResult.Stderr.Contains("couldn't find remote ref", StringComparison.OrdinalIgnoreCase);

    // (b) The note is keyed by tree, not commit.
    CommandOutput treeResult = await Shell.Builder("git")
      .WithArguments("rev-parse", $"{commitish}^{{tree}}")
      .WithWorkingDirectory(repoRoot)
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    if (treeResult.ExitCode != 0)
    {
      throw new InvalidOperationException($"Could not resolve the tree of {commitish} for attestation verify: {treeResult.Stderr.Trim()}");
    }

    string treeSha = treeResult.Stdout.Trim();

    // (c) Read the note for this tree.
    CommandOutput noteResult = await Shell.Builder("git")
      .WithArguments("notes", $"--ref={AttestationVerifier.NotesRefShort}", "show", treeSha)
      .WithWorkingDirectory(repoRoot)
      .WithEnvironmentVariable("LC_ALL", "C")
      .WithEnvironmentVariable("LANG", "C")
      .WithNoValidation()
      .CaptureAsync(cancellationToken)
      .ConfigureAwait(false);

    string? noteJson = noteResult.ExitCode == 0 ? noteResult.Stdout.Trim() : null;

    if (string.IsNullOrWhiteSpace(noteJson))
    {
      bool noLocalNote = noteResult.ExitCode == 1
        && noteResult.Stderr.Contains("no note found", StringComparison.OrdinalIgnoreCase);

      // RefMissing only when BOTH the remote ref and a local note are absent.
      AttestationVerificationStatus status = remoteNotesRefMissing && noLocalNote
        ? AttestationVerificationStatus.RefMissing
        : AttestationVerificationStatus.NoNote;

      return new AttestationVerifyOutcome(status, treeSha, null, null, null);
    }

    // (d) Pure evaluation: parse, schema/field checks, key lookup, tree match.
    AttestationEvaluation evaluation = AttestationVerifier.Evaluate(noteJson, treeSha, keyOverride);

    if (evaluation.Status != AttestationVerificationStatus.ReadyToVerify)
    {
      return new AttestationVerifyOutcome(evaluation.Status, treeSha, evaluation.Note?.CheckSet, evaluation.Note?.Ts, evaluation.Detail, evaluation.Note?.KeyId);
    }

    // (e) Ed25519 verify via openssl.
    DirectoryInfo tempDir = Directory.CreateTempSubdirectory("dev-attest-");
    try
    {
      string payloadPath = Path.Combine(tempDir.FullName, "payload.bin");
      string sigPath = Path.Combine(tempDir.FullName, "sig.bin");
      string pemPath = Path.Combine(tempDir.FullName, "pub.pem");

      await File.WriteAllBytesAsync(payloadPath, evaluation.CanonicalBytes!, cancellationToken).ConfigureAwait(false);
      await File.WriteAllBytesAsync(sigPath, evaluation.SignatureBytes!, cancellationToken).ConfigureAwait(false);
      await File.WriteAllTextAsync(pemPath, evaluation.PublicKeyPem, cancellationToken).ConfigureAwait(false);

      CommandOutput verifyResult;
      try
      {
        verifyResult = await Shell.Builder("openssl")
          .WithArguments("pkeyutl", "-verify", "-pubin", "-inkey", pemPath, "-rawin", "-in", payloadPath, "-sigfile", sigPath)
          .WithNoValidation()
          .CaptureAsync(cancellationToken)
          .ConfigureAwait(false);
      }
      catch (System.ComponentModel.Win32Exception)
      {
        return new AttestationVerifyOutcome(
          AttestationVerificationStatus.VerifierUnavailable,
          treeSha,
          evaluation.Note!.CheckSet,
          evaluation.Note.Ts,
          "openssl could not be launched",
          evaluation.Note.KeyId);
      }

      AttestationVerificationStatus finalStatus = verifyResult.ExitCode == 0
        ? AttestationVerificationStatus.Valid
        : AttestationVerificationStatus.BadSignature;

      return new AttestationVerifyOutcome(
        finalStatus,
        treeSha,
        evaluation.Note!.CheckSet,
        evaluation.Note.Ts,
        finalStatus == AttestationVerificationStatus.BadSignature ? verifyResult.Stderr.Trim() : null,
        evaluation.Note.KeyId);
    }
    finally
    {
      tempDir.Delete(recursive: true);
    }
  }

  /// <summary>
  /// The one-line operator message for <paramref name="outcome"/>, used by the
  /// `dev workflow` PR/merge and release policy steps (task 458-010 guidance text).
  /// </summary>
  public static string Describe(AttestationVerifyOutcome outcome)
  {
    ArgumentNullException.ThrowIfNull(outcome);

    string shortTree = ShortSha(outcome.Tree);

    return outcome.Status switch
    {
      AttestationVerificationStatus.Valid =>
        $"Attestation valid: check_set {ShortSha(outcome.CheckSet)} ts {outcome.Ts}",

      AttestationVerificationStatus.NoNote or AttestationVerificationStatus.RefMissing =>
        $"tree {shortTree} is unattested — pull master locally so ganda can attest (ganda repo attest)",

      AttestationVerificationStatus.UnknownKey =>
        $"tree {shortTree} attestation uses an unrecognized key_id — update TimeWarp.Nuru.DevCli ({outcome.Detail})",

      AttestationVerificationStatus.VerifierUnavailable =>
        "openssl not found — install openssl",

      AttestationVerificationStatus.TreeMismatch or AttestationVerificationStatus.BadSignature =>
        $"tree {shortTree} attestation invalid (possible tampering) — re-attest via ganda",

      AttestationVerificationStatus.ParseFailure =>
        $"tree {shortTree} attestation note could not be parsed ({outcome.Detail})",

      _ => $"tree {shortTree} attestation verification returned unexpected status '{outcome.Status}'"
    };
  }

  private static string ShortSha(string? sha)
  {
    if (string.IsNullOrEmpty(sha))
    {
      return "(unknown)";
    }

    return sha.Length > 7 ? sha[..7] : sha;
  }
}
