# Disposition — 458-010 verifier half

**Outcome: accepted-exceptions**

- Rounds: 2 (find → fix+verify). Roster: 1 × general-purpose (sonnet)
  adversarial reviewer (ae1cd0fe0cd240e0a); implementer aa60d7aaee467afff;
  effort 1, security-elevated scrutiny.
- Findings: 4 — 1 MED resolved (decoder accepted padded/standard base64;
  proven live in 3 encodings of a real 64-byte sig; now rejects any char
  outside [A-Za-z0-9_-] pre-decode, with bijective real-signature fixtures
  that self-verify they aren't no-ops), 2 LOW resolved (git stderr matching
  locale-pinned via Amuru WithEnvironmentVariable on both call sites
  [API existence decompile-verified]; unrecognized attestation.mode warns
  once via pure AttestationConfigResolver), 1 INFO **wontfix**
  (UnknownKey-before-TreeMismatch diagnostic ordering — neither order can
  yield Valid on a false condition; deliberate first-failing-check design).
- Reviewer positively verified: canonical bytes byte-identical to ganda's
  signer; registry hex equals live key-show output (post-rotation key); SPKI
  PEM derivation reproduces key-show byte-for-byte (independent Python
  re-derivation); malformed notes cannot reach openssl; keyOverride seam
  unreachable from production; **zero production-key contact** (grep + live
  throwaway-key cryptographic round-trip through production code paths).
- **Accepted exceptions:** the INFO wontfix; and the shared-ecosystem decoder
  leniency on the ganda side is tracked separately as timewarp-ganda
  kanban 200 (signer already emits spec-conformant output; only its accept
  path is lenient).
- Commits: implementation `b7f0cf34`, round-1 fixes `2e9b65f2`.
- CI: 1561 total / 1554 passed / 7 skipped / 0 failed (reviewer reproduced
  both rounds; 41 attestation tests green in multi-mode).
