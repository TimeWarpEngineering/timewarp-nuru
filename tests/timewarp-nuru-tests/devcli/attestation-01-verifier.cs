#!/usr/bin/env -S dotnet --

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
    byte[] original = MakeSampleSignatureBytes();
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

  // Short, deliberately-not-64-bytes smoke check that a lone padding
  // character is rejected — kept for backward compatibility with round-1,
  // but this is a QUICK sanity check, not the rigorous coverage: the tests
  // below prove strictness against REAL 64-byte-signature encodings in
  // both disallowed alphabets (round-1 review Fix 1).
  public static async Task DecodeSignature_returns_null_for_a_string_containing_a_padding_character()
  {
    AttestationVerifier.DecodeSignature("abc=").ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_for_non_base64_garbage()
  {
    AttestationVerifier.DecodeSignature("not!!!base64###").ShouldBeNull();

    await Task.CompletedTask;
  }

  // --- DecodeSignature: strict unpadded-base64url-only (round-1 review Fix 1) ---
  //
  // These use the SAME 64 real signature bytes as the happy-path round-trip
  // test above, re-encoded in two DIFFERENT (disallowed) base64 dialects —
  // proving DecodeSignature rejects them on ALPHABET/PADDING grounds, not
  // merely because they happen not to decode to 64 bytes (they decode to
  // the identical 64 bytes; the encodings are bijective).

  public static async Task DecodeSignature_returns_null_for_standard_alphabet_padded_encoding_of_a_real_64_byte_signature()
  {
    byte[] original = MakeSampleSignatureBytes();
    string standardPaddedBase64 = Convert.ToBase64String(original);

    // Sanity on the fixture itself: this really does contain a
    // disallowed character (padding, and — for these particular bytes —
    // a standard-alphabet '/' too), not an accidental no-op re-encoding.
    standardPaddedBase64.ShouldContain("=");
    standardPaddedBase64.ShouldContain("/");

    AttestationVerifier.DecodeSignature(standardPaddedBase64).ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_for_base64url_alphabet_with_padding_of_a_real_64_byte_signature()
  {
    byte[] original = MakeSampleSignatureBytes();
    string base64UrlWithPadding = Convert.ToBase64String(original).Replace('+', '-').Replace('/', '_');

    // Sanity: url-safe alphabet ('-'/'_'), but STILL carries '=' padding —
    // the one thing wrong with it relative to the frozen contract.
    base64UrlWithPadding.ShouldContain("=");
    base64UrlWithPadding.ShouldNotContain("+");
    base64UrlWithPadding.ShouldNotContain("/");

    AttestationVerifier.DecodeSignature(base64UrlWithPadding).ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_when_input_contains_plus()
  {
    string valid = EncodeUnpaddedBase64Url(MakeSampleSignatureBytes());
    string withPlus = "+" + valid[1..];

    AttestationVerifier.DecodeSignature(withPlus).ShouldBeNull();

    await Task.CompletedTask;
  }

  public static async Task DecodeSignature_returns_null_when_input_contains_slash()
  {
    string valid = EncodeUnpaddedBase64Url(MakeSampleSignatureBytes());
    string withSlash = "/" + valid[1..];

    AttestationVerifier.DecodeSignature(withSlash).ShouldBeNull();

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

  public static async Task Evaluate_sig_containing_a_padding_character_yields_ParseFailure()
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

  // Both use the SAME real 64-byte signature as the ReadyToVerify happy
  // path below, just encoded in a disallowed dialect — Evaluate must
  // reject them at the ParseFailure stage (via DecodeSignature) and name
  // 'sig' as the offending field, never let a bijective re-encoding of a
  // genuinely valid signature slip through (round-1 review Fix 1).

  public static async Task Evaluate_standard_alphabet_padded_sig_yields_ParseFailure_naming_sig()
  {
    string standardPaddedBase64 = Convert.ToBase64String(MakeSampleSignatureBytes());
    string json = ValidNoteJson(sig: standardPaddedBase64);

    AttestationEvaluation result = AttestationVerifier.Evaluate(json, Tree);

    result.Status.ShouldBe(AttestationVerificationStatus.ParseFailure);
    result.Detail.ShouldNotBeNull();
    result.Detail.ShouldContain("sig");

    await Task.CompletedTask;
  }

  public static async Task Evaluate_base64url_alphabet_with_padding_sig_yields_ParseFailure_naming_sig()
  {
    string base64UrlWithPadding = Convert.ToBase64String(MakeSampleSignatureBytes()).Replace('+', '-').Replace('/', '_');
    string json = ValidNoteJson(sig: base64UrlWithPadding);

    AttestationEvaluation result = AttestationVerifier.Evaluate(json, Tree);

    result.Status.ShouldBe(AttestationVerificationStatus.ParseFailure);
    result.Detail.ShouldNotBeNull();
    result.Detail.ShouldContain("sig");

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

  // Deterministic 64-byte fixture reused across the happy-path round-trip
  // test and the strict-decoding rejection tests, so the latter genuinely
  // prove "same bytes, disallowed encoding" rather than an unrelated fixture.
  private static byte[] MakeSampleSignatureBytes()
  {
    byte[] bytes = new byte[64];
    for (int i = 0; i < bytes.Length; i++)
    {
      bytes[i] = (byte)(i * 4 + 1);
    }

    return bytes;
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
