#!/usr/bin/dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using System.Text;
using System.Text.Json;
using global::DevCli;

/// <summary>
/// Pure verifier matrix for AttestationVerifier (kanban task 458-010, DevCli
/// half of the ganda-audit attestation contract). Everything here is pure —
/// no git, no openssl, no filesystem — matching AttestationVerifier's own
/// "no process execution" design (attestation-verifier.cs Purpose region).
/// Cross-process (openssl) coverage lives in
/// attestation-02-openssl-verify.cs, using a THROWAWAY keypair generated
/// in-test — this file never touches signing at all, only the pure
/// evaluation logic, so even the throwaway-key rule does not apply here.
/// </summary>
[TestTag("DevCli")]
public class AttestationVerifierTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AttestationVerifierTests>();

  private const string Tree = "b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1";
  private const string OtherTree = "0000000000000000000000000000000000000f";
  private const string CheckSet = "deadbeefcafef00ddeadbeefcafef00ddeadbeefcafef00ddeadbeefcafef00d";
  private const string Ts = "2026-08-08T12:00:00Z";

  // --- BuildCanonicalBytes: frozen v1 wire format ---

  public static async Task Canonical_bytes_match_the_frozen_v1_format_exactly()
  {
    byte[] bytes = AttestationVerifier.BuildCanonicalBytes(Tree, CheckSet, Ts, "tw-audit-1");

    string expected = $"v1\ned25519\n{Tree}\n{CheckSet}\n{Ts}\ntw-audit-1";
    Encoding.UTF8.GetString(bytes).ShouldBe(expected);

    await Task.CompletedTask;
  }

  public static async Task Canonical_bytes_have_no_trailing_newline_after_key_id()
  {
    byte[] bytes = AttestationVerifier.BuildCanonicalBytes(Tree, CheckSet, Ts, "tw-audit-1");

    // Last byte must be the final char of "tw-audit-1" ('1'), never '\n'.
    bytes[^1].ShouldBe((byte)'1');
    Encoding.UTF8.GetString(bytes).EndsWith('\n').ShouldBeFalse();

    await Task.CompletedTask;
  }

  public static async Task Canonical_bytes_throw_on_null_or_whitespace_arguments()
  {
    Should.Throw<ArgumentException>(() => AttestationVerifier.BuildCanonicalBytes(null!, CheckSet, Ts, "k"));
    Should.Throw<ArgumentException>(() => AttestationVerifier.BuildCanonicalBytes(Tree, "", Ts, "k"));
    Should.Throw<ArgumentException>(() => AttestationVerifier.BuildCanonicalBytes(Tree, CheckSet, "   ", "k"));
    Should.Throw<ArgumentException>(() => AttestationVerifier.BuildCanonicalBytes(Tree, CheckSet, Ts, null!));

    await Task.CompletedTask;
  }

  // --- TryParseNote: source-gen context round-trip ---

  public static async Task TryParseNote_parses_a_compact_note_body_through_the_source_gen_context()
  {
    string json = $$"""{"v":1,"alg":"ed25519","tree":"{{Tree}}","check_set":"{{CheckSet}}","ts":"{{Ts}}","key_id":"tw-audit-1","sig":"c2ln"}""";

    AttestationNoteDto? note = AttestationVerifier.TryParseNote(json);

    note.ShouldNotBeNull();
    note.V.ShouldBe(1);
    note.Alg.ShouldBe("ed25519");
    note.Tree.ShouldBe(Tree);
    note.CheckSet.ShouldBe(CheckSet);
    note.Ts.ShouldBe(Ts);
    note.KeyId.ShouldBe("tw-audit-1");
    note.Sig.ShouldBe("c2ln");

    // Same context instance the runtime deserialize path uses — proves this
    // type is actually wired into DevCliJsonContext, not just structurally
    // compatible with System.Text.Json's reflection fallback.
    JsonSerializer.Deserialize(json, DevCliJsonContext.Default.AttestationNoteDto).ShouldNotBeNull();

    await Task.CompletedTask;
  }

  public static async Task TryParseNote_returns_null_for_null_or_blank_input()
  {
    AttestationVerifier.TryParseNote(null).ShouldBeNull();
    AttestationVerifier.TryParseNote("").ShouldBeNull();
    AttestationVerifier.TryParseNote("   ").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task TryParseNote_returns_null_for_malformed_json()
  {
    AttestationVerifier.TryParseNote("{not valid json").ShouldBeNull();
    AttestationVerifier.TryParseNote("[1,2,3]").ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- DecodeSignature: unpadded base64url round-trip ---

  public static async Task DecodeSignature_round_trips_a_64_byte_signature()
  {
    byte[] original = new byte[64];
    for (int i = 0; i < original.Length; i++)
    {
      original[i] = (byte)(i * 4 + 1);
    }

    string unpaddedBase64Url = EncodeUnpaddedBase64Url(original);

    byte[]? decoded = AttestationVerifier.DecodeSignature(unpaddedBase64Url);

    decoded.ShouldNotBeNull();
    decoded.ShouldBe(original);

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_for_a_63_byte_signature()
  {
    byte[] tooShort = new byte[63];
    string encoded = EncodeUnpaddedBase64Url(tooShort);

    AttestationVerifier.DecodeSignature(encoded).ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_for_padded_base64()
  {
    // "abc=" is syntactically valid base64 (decodes to 2 bytes) but is
    // PADDED, not the unpadded base64url this contract requires; either
    // way it is nowhere near 64 bytes, so it must be rejected.
    AttestationVerifier.DecodeSignature("abc=").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_for_non_base64_garbage()
  {
    AttestationVerifier.DecodeSignature("not!!!base64###").ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- BuildPublicKeyPem: SPKI derivation shape (not ganda's exact byte-output) ---

  public static async Task BuildPublicKeyPem_wraps_the_spki_der_derived_from_hex()
  {
    string hex = AttestationVerifier.KnownKeys["tw-audit-1"];

    string pem = AttestationVerifier.BuildPublicKeyPem(hex);

    pem.ShouldStartWith("-----BEGIN PUBLIC KEY-----\n");
    pem.ShouldEndWith("-----END PUBLIC KEY-----\n");

    // Recompute the expected DER independently (prefix + raw key) and
    // confirm the PEM body base64-decodes back to exactly that — this
    // pins the SPKI derivation without asserting ganda's own PEM
    // line-wrap/whitespace conventions.
    byte[] expectedPrefix = Convert.FromHexString("302a300506032b6570032100");
    byte[] expectedRawKey = Convert.FromHexString(hex);
    byte[] expectedDer = [.. expectedPrefix, .. expectedRawKey];

    string body = pem
      .Replace("-----BEGIN PUBLIC KEY-----", string.Empty)
      .Replace("-----END PUBLIC KEY-----", string.Empty)
      .Replace("\n", string.Empty)
      .Replace("\r", string.Empty);

    byte[] decodedDer = Convert.FromBase64String(body);
    decodedDer.ShouldBe(expectedDer);

    await Task.CompletedTask;
  }

  public static async Task BuildPublicKeyPem_throws_when_hex_does_not_decode_to_32_bytes()
  {
    Should.Throw<ArgumentException>(() => AttestationVerifier.BuildPublicKeyPem("aabb"));

    await Task.CompletedTask;
  }

  // --- Evaluate: NoNote ---

  public static async Task Evaluate_null_note_json_yields_NoNote()
  {
    AttestationEvaluation result = AttestationVerifier.Evaluate(null, Tree);
    result.Status.ShouldBe(AttestationVerificationStatus.NoNote);
    result.Note.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Evaluate_blank_note_json_yields_NoNote()
  {
    AttestationVerifier.Evaluate("", Tree).Status.ShouldBe(AttestationVerificationStatus.NoNote);
    AttestationVerifier.Evaluate("   ", Tree).Status.ShouldBe(AttestationVerificationStatus.NoNote);

    await Task.CompletedTask;
  }

  // --- Evaluate: ParseFailure ---

  public static async Task Evaluate_malformed_json_yields_ParseFailure()
  {
    AttestationEvaluation result = AttestationVerifier.Evaluate("{not valid json", Tree);
    result.Status.ShouldBe(AttestationVerificationStatus.ParseFailure);
    result.Note.ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task Evaluate_unsupported_schema_version_yields_ParseFailure()
  {
    string json = ValidNoteJson(v: 2);
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ParseFailure);

    await Task.CompletedTask;
  }

  public static async Task Evaluate_unsupported_algorithm_yields_ParseFailure()
  {
    string json = ValidNoteJson(alg: "rsa");
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ParseFailure);

    await Task.CompletedTask;
  }

  public static async Task Evaluate_missing_sig_field_yields_ParseFailure()
  {
    string json = $$"""{"v":1,"alg":"ed25519","tree":"{{Tree}}","check_set":"{{CheckSet}}","ts":"{{Ts}}","key_id":"tw-audit-1"}""";
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ParseFailure);

    await Task.CompletedTask;
  }

  public static async Task Evaluate_padded_base64_sig_yields_ParseFailure()
  {
    string json = ValidNoteJson(sig: "abc=");
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ParseFailure);

    await Task.CompletedTask;
  }

  public static async Task Evaluate_63_byte_sig_yields_ParseFailure()
  {
    string shortSig = EncodeUnpaddedBase64Url(new byte[63]);
    string json = ValidNoteJson(sig: shortSig);
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ParseFailure);

    await Task.CompletedTask;
  }

  // --- Evaluate: UnknownKey ---

  public static async Task Evaluate_unknown_key_id_yields_UnknownKey()
  {
    string json = ValidNoteJson(keyId: "tw-audit-999");
    AttestationEvaluation result = AttestationVerifier.Evaluate(json, Tree);

    result.Status.ShouldBe(AttestationVerificationStatus.UnknownKey);
    result.Detail.ShouldNotBeNull();
    result.Detail.ShouldContain("tw-audit-999");

    await Task.CompletedTask;
  }

  // --- Evaluate: TreeMismatch ---

  public static async Task Evaluate_tree_mismatch_yields_TreeMismatch()
  {
    // key_id "tw-audit-1" IS known (it's registry data, not a live signing
    // key — see attestation-verifier.cs Design region), so this exercises
    // the tree-comparison branch specifically, past the key-lookup branch.
    string json = ValidNoteJson(tree: OtherTree, keyId: "tw-audit-1");
    AttestationEvaluation result = AttestationVerifier.Evaluate(json, Tree);

    result.Status.ShouldBe(AttestationVerificationStatus.TreeMismatch);

    await Task.CompletedTask;
  }

  // --- Evaluate: ReadyToVerify (the test seam — keyOverride) ---

  public static async Task Evaluate_ready_to_verify_with_key_override()
  {
    const string throwawayKeyId = "tw-test-1";
    // 32 arbitrary bytes standing in for a public key — Evaluate only
    // needs 32 bytes of hex to synthesize a PEM; it never verifies the
    // signature itself (that is the caller's openssl step).
    string throwawayHex = Convert.ToHexString([.. Enumerable.Range(0, 32).Select(i => (byte)i)]).ToLowerInvariant();
    Dictionary<string, string> keyOverride = new(StringComparer.Ordinal) { [throwawayKeyId] = throwawayHex };

    string json = ValidNoteJson(tree: Tree, keyId: throwawayKeyId);

    AttestationEvaluation result = AttestationVerifier.Evaluate(json, Tree, keyOverride);

    result.Status.ShouldBe(AttestationVerificationStatus.ReadyToVerify);
    result.Note.ShouldNotBeNull();
    result.CanonicalBytes.ShouldNotBeNull();
    result.SignatureBytes.ShouldNotBeNull();
    result.SignatureBytes!.Length.ShouldBe(64);
    result.PublicKeyPem.ShouldNotBeNull();
    result.PublicKeyPem.ShouldContain("BEGIN PUBLIC KEY");

    // Rebuilding canonical bytes independently must match what Evaluate derived.
    byte[] expectedCanonical = AttestationVerifier.BuildCanonicalBytes(Tree, CheckSet, Ts, throwawayKeyId);
    result.CanonicalBytes.ShouldBe(expectedCanonical);

    await Task.CompletedTask;
  }

  public static async Task Evaluate_keyOverride_absent_falls_back_to_KnownKeys()
  {
    // Same note, no override — resolves against the production registry
    // (data only: a public hex, never a signing operation).
    string json = ValidNoteJson(tree: Tree, keyId: "tw-audit-1");
    AttestationVerifier.Evaluate(json, Tree).Status.ShouldBe(AttestationVerificationStatus.ReadyToVerify);

    await Task.CompletedTask;
  }

  private static string ValidNoteJson(
    int v = 1,
    string alg = "ed25519",
    string tree = Tree,
    string checkSet = CheckSet,
    string ts = Ts,
    string keyId = "tw-audit-1",
    string? sig = null)
  {
    string sigValue = sig ?? EncodeUnpaddedBase64Url(new byte[64]);
    return $$"""{"v":{{v}},"alg":"{{alg}}","tree":"{{tree}}","check_set":"{{checkSet}}","ts":"{{ts}}","key_id":"{{keyId}}","sig":"{{sigValue}}"}""";
  }

  private static string EncodeUnpaddedBase64Url(byte[] bytes) =>
    Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

} // namespace TimeWarp.Nuru.Tests.DevCli
