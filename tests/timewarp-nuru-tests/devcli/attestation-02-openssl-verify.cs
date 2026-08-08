#!/usr/bin/env -S dotnet --

#if !JARIBU_MULTI
return await RunAllTests();
#endif

namespace TimeWarp.Nuru.Tests.DevCli
{

using global::DevCli;

/// <summary>
/// End-to-end proof of the openssl verify loop (kanban task 458-010) using a
/// THROWAWAY Ed25519 keypair generated fresh in this test via
/// <c>openssl genpkey</c>. This NEVER reads, copies, or signs with anything
/// under <c>~/.timewarp/ganda/keys/</c> — that path is the production trust
/// root and is out of bounds for tests (prefs SSOT rule; the production key
/// was rotated 2026-08-08 after an earlier near-miss). The throwaway public
/// key is injected via AttestationVerifier.Evaluate's `keyOverride`
/// parameter — the test seam documented in attestation-verifier.cs's Design
/// region — never via AttestationVerifier.KnownKeys.
///
/// This reproduces, byte for byte, the exact command shape
/// workflow-command.cs's VerifyAttestationAsync runs against a real ganda
/// note: `openssl pkeyutl -verify -pubin -inkey &lt;pem&gt; -rawin -in
/// &lt;payload&gt; -sigfile &lt;sig&gt;`.
/// </summary>
[TestTag("DevCli")]
public class AttestationOpenSslVerifyTests
{
  [ModuleInitializer]
  internal static void Register() => RegisterTests<AttestationOpenSslVerifyTests>();

  private const string FakeTree = "b2eea2c9acdd5f1a0cd3f1a07af36ed1658409b1";
  private const string FakeCheckSet = "deadbeefcafef00ddeadbeefcafef00ddeadbeefcafef00ddeadbeefcafef00d";
  private const string FakeTs = "2026-08-08T12:00:00Z";
  private const string ThrowawayKeyId = "tw-test-1";

  public static async Task Full_loop_generates_signs_evaluates_and_verifies_with_a_throwaway_key()
  {
    DirectoryInfo workDir = Directory.CreateTempSubdirectory("attest-openssl-test-");
    try
    {
      string privPemPath = Path.Combine(workDir.FullName, "throwaway-priv.pem");
      string pubDerPath = Path.Combine(workDir.FullName, "throwaway-pub.der");

      // --- Generate a THROWAWAY keypair (never the production key) ---
      CommandOutput genResult = await Shell.Builder("openssl")
        .WithArguments("genpkey", "-algorithm", "ed25519", "-out", privPemPath)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);
      genResult.ExitCode.ShouldBe(0, genResult.Stderr);

      CommandOutput pubDerResult = await Shell.Builder("openssl")
        .WithArguments("pkey", "-in", privPemPath, "-pubout", "-outform", "DER", "-out", pubDerPath)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);
      pubDerResult.ExitCode.ShouldBe(0, pubDerResult.Stderr);

      // SPKI DER for Ed25519 is a fixed 12-byte prefix + the 32-byte raw
      // public key (RFC 8410) — the last 32 bytes of the DER are the raw
      // key, matching AttestationVerifier.BuildPublicKeyPem's own prefix
      // (verified against real `openssl pkey -pubout` output).
      byte[] pubDer = await File.ReadAllBytesAsync(pubDerPath);
      pubDer.Length.ShouldBe(44);
      byte[] throwawayRawKey = pubDer[^32..];
      string throwawayHex = Convert.ToHexString(throwawayRawKey).ToLowerInvariant();

      Dictionary<string, string> keyOverride = new(StringComparer.Ordinal) { [ThrowawayKeyId] = throwawayHex };

      // --- Build and sign the canonical payload for a fake tree ---
      byte[] canonicalBytes = AttestationVerifier.BuildCanonicalBytes(FakeTree, FakeCheckSet, FakeTs, ThrowawayKeyId);
      string payloadPath = Path.Combine(workDir.FullName, "payload.bin");
      await File.WriteAllBytesAsync(payloadPath, canonicalBytes);

      string sigPath = Path.Combine(workDir.FullName, "sig.bin");
      CommandOutput signResult = await Shell.Builder("openssl")
        .WithArguments("pkeyutl", "-sign", "-inkey", privPemPath, "-rawin", "-in", payloadPath, "-out", sigPath)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);
      signResult.ExitCode.ShouldBe(0, signResult.Stderr);

      byte[] sigBytes = await File.ReadAllBytesAsync(sigPath);
      sigBytes.Length.ShouldBe(64);
      string sigBase64Url = Convert.ToBase64String(sigBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

      // --- Construct the note and evaluate through the SAME pure path the
      // caller uses, with the throwaway key injected via keyOverride ---
      string noteJson =
        $$"""{"v":1,"alg":"ed25519","tree":"{{FakeTree}}","check_set":"{{FakeCheckSet}}","ts":"{{FakeTs}}","key_id":"{{ThrowawayKeyId}}","sig":"{{sigBase64Url}}"}""";

      AttestationEvaluation evaluation = AttestationVerifier.Evaluate(noteJson, FakeTree, keyOverride);

      evaluation.Status.ShouldBe(AttestationVerificationStatus.ReadyToVerify);
      evaluation.CanonicalBytes.ShouldNotBeNull();
      evaluation.SignatureBytes.ShouldNotBeNull();
      evaluation.PublicKeyPem.ShouldNotBeNull();
      evaluation.CanonicalBytes.ShouldBe(canonicalBytes);
      evaluation.SignatureBytes.ShouldBe(sigBytes);

      // --- Run the EXACT command shape workflow-command.cs's
      // VerifyAttestationAsync runs, using the evaluation's own derived
      // bytes/PEM (not the raw openssl-generated files above) — this
      // proves AttestationVerifier's derived PEM is what actually gets
      // handed to openssl in production, not just structurally similar. ---
      string derivedPayloadPath = Path.Combine(workDir.FullName, "derived-payload.bin");
      string derivedSigPath = Path.Combine(workDir.FullName, "derived-sig.bin");
      string derivedPemPath = Path.Combine(workDir.FullName, "derived-pub.pem");

      await File.WriteAllBytesAsync(derivedPayloadPath, evaluation.CanonicalBytes!);
      await File.WriteAllBytesAsync(derivedSigPath, evaluation.SignatureBytes!);
      await File.WriteAllTextAsync(derivedPemPath, evaluation.PublicKeyPem);

      CommandOutput verifyResult = await Shell.Builder("openssl")
        .WithArguments("pkeyutl", "-verify", "-pubin", "-inkey", derivedPemPath, "-rawin", "-in", derivedPayloadPath, "-sigfile", derivedSigPath)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      verifyResult.ExitCode.ShouldBe(0, verifyResult.Stderr);

      // --- Flip one signature byte -> verify must fail (nonzero exit) ---
      byte[] tamperedSig = (byte[])evaluation.SignatureBytes!.Clone();
      tamperedSig[0] ^= 0xFF;
      string tamperedSigPath = Path.Combine(workDir.FullName, "tampered-sig.bin");
      await File.WriteAllBytesAsync(tamperedSigPath, tamperedSig);

      CommandOutput tamperedVerifyResult = await Shell.Builder("openssl")
        .WithArguments("pkeyutl", "-verify", "-pubin", "-inkey", derivedPemPath, "-rawin", "-in", derivedPayloadPath, "-sigfile", tamperedSigPath)
        .WithNoValidation()
        .CaptureAsync(CancellationToken.None);

      tamperedVerifyResult.ExitCode.ShouldNotBe(0);
    }
    finally
    {
      workDir.Delete(recursive: true);
    }
  }
}

} // namespace TimeWarp.Nuru.Tests.DevCli
