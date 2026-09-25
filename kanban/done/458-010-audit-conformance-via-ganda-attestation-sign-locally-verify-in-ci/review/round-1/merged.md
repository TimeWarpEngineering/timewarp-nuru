# Round 1 — merged findings, verifier half (reviewer: general-purpose/sonnet, agent ae1cd0fe0cd240e0a)

Diff: commit `b7f0cf34`. Reviewer verified live: canonical bytes byte-identical
to ganda's AttestationPayload; note JSON/ref/tree-keying identical; embedded
registry hex equals live `key-show` output; SPKI PEM derivation reproduces
key-show's PEM byte-for-byte (independent Python re-derivation); malformed
notes cannot reach openssl; TreeMismatch unbypassable; temp dirs randomized
with finally-cleanup; keyOverride unreachable from production call sites;
**zero production-key contact confirmed by grep + live throwaway-key
round-trip**; AOT registrations complete; CI 1538/0 reproduced.

| # | Sev | Finding | Status | Disposition |
|---|-----|---------|--------|-------------|
| 1 | MED | `DecodeSignature` accepts padded base64 and standard-alphabet base64 (proven live with a real 64-byte sig in 3 encodings) — spec mandates unpadded base64url only; existing "padded" tests actually test length, not padding. No forgery vector (encodings are bijective to the same bytes; Ed25519 check operates on decoded bytes) but a real wire-contract divergence. Same leniency exists in ganda's own reference decoder (shared ecosystem gap). | fix | Reject any sig containing `+`, `/`, or `=` before decoding; add real 64-byte padded/standard-alphabet fixtures. Ganda-side tightening noted for operator (separate repo). |
| 2 | LOW | RefMissing/NoNote classification matches English git stderr substrings — non-English locale or reworded git message misclassifies (fail-closed either way; message precision only) | fix | Force C locale on the git invocations if Amuru Shell supports env injection; else document the limitation in the Design region (either resolution acceptable). |
| 3 | LOW | Unrecognized `attestation.mode` (typo like "requiree") silently falls back to warn — operator believes enforcement is on | fix | Print a warning naming the unrecognized value and the fallback; keep warn behavior. |
| 4 | INFO | Evaluate checks UnknownKey before TreeMismatch (spec lists tree first) — diagnostic ordering only; neither ordering can yield Valid on a false condition | wontfix | Deliberate "first failing check wins" design, documented in Design region. Decider: orchestrator. |

Open after dispositions: 1–3 → fix (one batch); 4 → wontfix.
