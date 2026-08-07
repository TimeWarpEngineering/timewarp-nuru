#region Purpose
// Pure verifier for ganda-audit attestation notes (kanban task 458-010,
// public/DevCli half of the attestation contract). Ganda (private, operator
// machines) signs evidence that its audit passed and pushes it to
// refs/notes/ganda-audit; this file lets ANY public repo's CI verify that
// evidence without ever running ganda's private audit itself. No process
// execution here — git/openssl orchestration lives in the caller
// (tools/dev-cli/endpoints/workflow-command.cs) so this logic stays testable
// as pure functions.
#endregion
#region Design
// Frozen verifier contract (v1) — mirrors ganda's
// source/timewarp-ganda/services/audit/attestation/{attestation-payload.cs,
// attestation-note.cs} byte-for-byte (documentation/developer/attestation.md
// in the ganda repo is the canonical spec):
//
//   - Notes ref: refs/notes/ganda-audit, keyed by TREE sha (not commit sha) —
//     `git notes --ref=ganda-audit show <tree>`.
//   - Note body: compact single-line JSON
//     {"v":1,"alg":"ed25519","tree":...,"check_set":...,"ts":...,"key_id":...,"sig":...}
//     — field names are frozen; AttestationNoteDto's JsonPropertyName
//     attributes must never change without a new schema version.
//   - Canonical signed payload: UTF-8 bytes of
//     "v1\ned25519\n{tree}\n{check_set}\n{ts}\n{key_id}" — NO trailing
//     newline after key_id. Verifiers must rebuild this exactly; any
//     whitespace/newline drift breaks every signature.
//   - ts: UTC ISO-8601, second precision, trailing 'Z' (informational only
//     for the verifier — it is signed-over opaque text, not re-validated as
//     a timestamp here).
//   - sig: Ed25519 signature (64 raw bytes) encoded as UNPADDED base64url
//     (RFC 4648 §5 alphabet, '-'/'_' in place of '+'/'/', trailing '='
//     stripped). DecodeSignature reverses this and rejects anything that
//     does not decode to exactly 64 bytes.
//
// Key registry + rotation:
//   KnownKeys maps key_id -> lowercase hex of the 32-byte raw Ed25519 public
//   key. It is baked into this package's content and updated by an
//   additive DevCli package bump when ganda rotates (documentation/
//   developer/attestation.md "Rotation procedure" in the ganda repo): the
//   new key_id is added here BEFORE ganda starts signing with it, and the
//   old key_id is kept for a grace window so in-flight PRs with
//   already-signed notes still verify. A key_id absent from the registry
//   (or from the `keyOverride` map, see below) evaluates to UnknownKey,
//   never a silent pass.
//
//   `tw-audit-1` is the org's post-rotation (2026-08-08) production key.
//   NEVER read, copy, or sign with the private half
//   (~/.timewarp/ganda/keys/) from this repo or its tests — that key lives
//   only on operator machines; DevCli only ever holds public material.
//
// BuildPublicKeyPem: ganda's key-show PEM is a standard PKCS#8
// SubjectPublicKeyInfo wrapping the raw 32-byte Ed25519 key behind the
// fixed 12-byte SPKI prefix for the Ed25519 OID (RFC 8410) —
// 302a300506032b6570032100 in hex. Concatenating that prefix with the raw
// key and PEM-wrapping (base64, 64-char lines, BEGIN/END PUBLIC KEY)
// reproduces byte-for-byte what OpenSSL (and ganda's BouncyCastle writer)
// emit for the same key, without pulling in a crypto library just to
// synthesize a well-known constant structure.
//
// keyOverride is the TEST SEAM: production code always evaluates against
// KnownKeys (call Evaluate with keyOverride omitted/null). Tests build a
// THROWAWAY Ed25519 keypair via `openssl genpkey` and pass
// `new Dictionary<string,string> { ["tw-test-1"] = throwawayHex }` as
// keyOverride so the full evaluation path (including the "is this key_id
// known" branch) runs without ever touching the production key or registry.
//
// Evaluate() checks in a fixed order — the FIRST failing check wins, so a
// note that is simultaneously malformed AND unknown-key always reports
// ParseFailure, never UnknownKey (the more specific verdict about *why* it
// is unusable, before deciding whether its claimed signer is trusted):
//   1. null/blank note JSON                          -> NoNote
//   2. parse failure / v != 1 / alg != "ed25519" /
//      missing-or-blank tree|check_set|ts|key_id|sig /
//      sig does not decode to a 64-byte signature     -> ParseFailure
//   3. key_id not present in (keyOverride ?? KnownKeys) -> UnknownKey
//   4. note.tree != actualTreeSha (Ordinal)             -> TreeMismatch
//   5. otherwise                                        -> ReadyToVerify
//      (canonical bytes + signature bytes + PEM populated; the actual
//      Ed25519 verification is intentionally NOT done here — no pure-.NET
//      Ed25519 verify exists in the BCL, and pulling in a crypto NuGet
//      package was rejected for this source-only package's posture. The
//      caller shells out to `openssl pkeyutl -verify -rawin` with these
//      three byte blobs and maps exit 0/nonzero to Valid/BadSignature.)
//
// Valid, RefMissing, BadSignature, and VerifierUnavailable are NEVER
// produced by Evaluate — they are orchestration-layer outcomes recorded by
// the caller once it has (a) established whether the notes ref exists at
// all remotely (RefMissing) and (b) actually shelled out to openssl
// (Valid/BadSignature/VerifierUnavailable). They exist on the same enum so
// callers can carry one outcome value end-to-end without a second type.
#endregion

namespace DevCli;

using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Wire shape of a ganda-audit attestation note body (frozen v1 field
/// names — see the Design region above).
/// </summary>
public sealed class AttestationNoteDto
{
  [JsonPropertyName("v")]
  public int V { get; set; } = 1;

  [JsonPropertyName("alg")]
  public string Alg { get; set; } = string.Empty;

  [JsonPropertyName("tree")]
  public string Tree { get; set; } = string.Empty;

  [JsonPropertyName("check_set")]
  public string CheckSet { get; set; } = string.Empty;

  [JsonPropertyName("ts")]
  public string Ts { get; set; } = string.Empty;

  [JsonPropertyName("key_id")]
  public string KeyId { get; set; } = string.Empty;

  [JsonPropertyName("sig")]
  public string Sig { get; set; } = string.Empty;
}

/// <summary>Verdict from evaluating an attestation note (see Design region above).</summary>
public enum AttestationVerificationStatus
{
  /// <summary>Note parsed, key known, tree matches — caller must still run the Ed25519 verify.</summary>
  ReadyToVerify,

  /// <summary>Orchestration-layer only: openssl verified the signature successfully.</summary>
  Valid,

  /// <summary>Orchestration-layer only: refs/notes/ganda-audit does not exist on origin AND no local note exists either.</summary>
  RefMissing,

  /// <summary>No note body for this tree (empty/null input to Evaluate).</summary>
  NoNote,

  /// <summary>Note JSON malformed, wrong schema/algorithm, missing fields, or an undecodable signature.</summary>
  ParseFailure,

  /// <summary>Note's key_id is not in the known-key registry.</summary>
  UnknownKey,

  /// <summary>Note's tree field does not match the tree actually being verified.</summary>
  TreeMismatch,

  /// <summary>Orchestration-layer only: openssl ran and rejected the signature.</summary>
  BadSignature,

  /// <summary>Orchestration-layer only: openssl could not be launched (not installed).</summary>
  VerifierUnavailable
}

/// <summary>
/// Result of <see cref="AttestationVerifier.Evaluate"/>: the verdict plus
/// whatever was derived along the way, so the caller never has to
/// re-derive canonical bytes / PEM / parsed fields itself.
/// </summary>
[SuppressMessage
(
  "Performance",
  "CA1819:Properties should not return arrays",
  Justification = "Record type carrying raw byte payloads (canonical bytes, signature) for the caller's openssl step is intentional"
)]
public sealed record AttestationEvaluation(
  AttestationVerificationStatus Status,
  AttestationNoteDto? Note,
  byte[]? CanonicalBytes,
  byte[]? SignatureBytes,
  string? PublicKeyPem,
  string? Detail);

/// <summary>
/// Pure evaluator for ganda-audit attestation notes. No process execution —
/// see the Purpose/Design regions above for the full frozen v1 contract and
/// why the actual Ed25519 verify happens in the caller via openssl.
/// </summary>
public static class AttestationVerifier
{
  /// <summary>Notes ref short name (git prefixes <c>refs/notes/</c>).</summary>
  public const string NotesRefShort = "ganda-audit";

  /// <summary>Full notes ref.</summary>
  public const string NotesRef = "refs/notes/ganda-audit";

  private const string Ed25519SpkiPrefixHex = "302a300506032b6570032100";

  /// <summary>
  /// Production key registry: key_id -> lowercase hex of the raw 32-byte
  /// Ed25519 public key. See the Design region above for rotation
  /// procedure. Tests must never rely on this map directly — pass a
  /// throwaway <c>keyOverride</c> to <see cref="Evaluate"/> instead.
  /// </summary>
  public static readonly IReadOnlyDictionary<string, string> KnownKeys =
    new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["tw-audit-1"] = "ea6d9ea94f07d0ffe4d46fa7021115f2d5130b715fced113c8742e1d3be94681"
    };

  /// <summary>Parse a note JSON body; returns null on empty/malformed input.</summary>
  public static AttestationNoteDto? TryParseNote(string? json)
  {
    if (string.IsNullOrWhiteSpace(json))
    {
      return null;
    }

    try
    {
      return JsonSerializer.Deserialize(json.Trim(), DevCliJsonContext.Default.AttestationNoteDto);
    }
    catch (JsonException)
    {
      return null;
    }
  }

  /// <summary>
  /// Build the exact UTF-8 bytes that are signed (and must be rebuilt
  /// identically by verifiers): <c>v1\ned25519\n{tree}\n{check_set}\n{ts}\n{key_id}</c>
  /// with NO trailing newline after <paramref name="keyId"/>.
  /// </summary>
  public static byte[] BuildCanonicalBytes(string tree, string checkSet, string ts, string keyId)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(tree);
    ArgumentException.ThrowIfNullOrWhiteSpace(checkSet);
    ArgumentException.ThrowIfNullOrWhiteSpace(ts);
    ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

    string payload =
      "v1" + "\n" +
      "ed25519" + "\n" +
      tree + "\n" +
      checkSet + "\n" +
      ts + "\n" +
      keyId;

    return Encoding.UTF8.GetBytes(payload);
  }

  /// <summary>
  /// Decode an unpadded base64url signature. Returns null (never throws)
  /// on any input that is not valid base64 or that does not decode to
  /// exactly 64 bytes (the Ed25519 signature length).
  /// </summary>
  public static byte[]? DecodeSignature(string value)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(value);

    string padded = value.Replace('-', '+').Replace('_', '/');
    int remainder = padded.Length % 4;
    if (remainder == 2)
    {
      padded += "==";
    }
    else if (remainder == 3)
    {
      padded += "=";
    }

    byte[] bytes;
    try
    {
      bytes = Convert.FromBase64String(padded);
    }
    catch (FormatException)
    {
      return null;
    }

    return bytes.Length == 64 ? bytes : null;
  }

  /// <summary>
  /// Synthesize the SubjectPublicKeyInfo PEM for a raw 32-byte Ed25519
  /// public key given as lowercase or uppercase hex. Reproduces ganda's
  /// <c>ganda repo attest key-show</c> PEM output for the same key.
  /// </summary>
  public static string BuildPublicKeyPem(string hex)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(hex);

    byte[] rawKey = Convert.FromHexString(hex);
    if (rawKey.Length != 32)
    {
      throw new ArgumentException("Ed25519 public key hex must decode to exactly 32 bytes.", nameof(hex));
    }

    byte[] prefix = Convert.FromHexString(Ed25519SpkiPrefixHex);
    byte[] der = new byte[prefix.Length + rawKey.Length];
    prefix.CopyTo(der, 0);
    rawKey.CopyTo(der, prefix.Length);

    string base64 = Convert.ToBase64String(der);

    StringBuilder pem = new();
    pem.Append("-----BEGIN PUBLIC KEY-----\n");
    for (int i = 0; i < base64.Length; i += 64)
    {
      int length = Math.Min(64, base64.Length - i);
      pem.Append(base64, i, length);
      pem.Append('\n');
    }

    pem.Append("-----END PUBLIC KEY-----\n");
    return pem.ToString();
  }

  /// <summary>
  /// Evaluate a note against the actual tree being verified. See the
  /// Design region above for the exact check order. Pure — never touches
  /// git, openssl, or the filesystem.
  /// </summary>
  public static AttestationEvaluation Evaluate(
    string? noteJson,
    string actualTreeSha,
    IReadOnlyDictionary<string, string>? keyOverride = null)
  {
    ArgumentException.ThrowIfNullOrWhiteSpace(actualTreeSha);

    if (string.IsNullOrWhiteSpace(noteJson))
    {
      return new AttestationEvaluation(AttestationVerificationStatus.NoNote, null, null, null, null, "no attestation note found for this tree");
    }

    AttestationNoteDto? note = TryParseNote(noteJson);
    if (note is null)
    {
      return new AttestationEvaluation(AttestationVerificationStatus.ParseFailure, null, null, null, null, "note body is not valid JSON");
    }

    if (note.V != 1)
    {
      return ParseFailure(note, $"unsupported schema version '{note.V}' (expected 1)");
    }

    if (!string.Equals(note.Alg, "ed25519", StringComparison.Ordinal))
    {
      return ParseFailure(note, $"unsupported algorithm '{note.Alg}' (expected 'ed25519')");
    }

    if (string.IsNullOrWhiteSpace(note.Tree))
    {
      return ParseFailure(note, "missing or blank 'tree' field");
    }

    if (string.IsNullOrWhiteSpace(note.CheckSet))
    {
      return ParseFailure(note, "missing or blank 'check_set' field");
    }

    if (string.IsNullOrWhiteSpace(note.Ts))
    {
      return ParseFailure(note, "missing or blank 'ts' field");
    }

    if (string.IsNullOrWhiteSpace(note.KeyId))
    {
      return ParseFailure(note, "missing or blank 'key_id' field");
    }

    if (string.IsNullOrWhiteSpace(note.Sig))
    {
      return ParseFailure(note, "missing or blank 'sig' field");
    }

    byte[]? signatureBytes = DecodeSignature(note.Sig);
    if (signatureBytes is null)
    {
      return ParseFailure(note, "'sig' is not a valid 64-byte unpadded base64url signature");
    }

    IReadOnlyDictionary<string, string> keys = keyOverride ?? KnownKeys;
    if (!keys.TryGetValue(note.KeyId, out string? publicKeyHex))
    {
      return new AttestationEvaluation(
        AttestationVerificationStatus.UnknownKey,
        note,
        null,
        null,
        null,
        $"key_id '{note.KeyId}' is not in the known-key registry — update TimeWarp.Nuru.DevCli");
    }

    if (!string.Equals(note.Tree, actualTreeSha, StringComparison.Ordinal))
    {
      return new AttestationEvaluation(
        AttestationVerificationStatus.TreeMismatch,
        note,
        null,
        null,
        null,
        $"note tree '{note.Tree}' does not match actual tree '{actualTreeSha}'");
    }

    byte[] canonicalBytes = BuildCanonicalBytes(note.Tree, note.CheckSet, note.Ts, note.KeyId);
    string publicKeyPem = BuildPublicKeyPem(publicKeyHex);

    return new AttestationEvaluation(
      AttestationVerificationStatus.ReadyToVerify,
      note,
      canonicalBytes,
      signatureBytes,
      publicKeyPem,
      null);
  }

  private static AttestationEvaluation ParseFailure(AttestationNoteDto note, string detail) =>
    new(AttestationVerificationStatus.ParseFailure, note, null, null, null, detail);
}
