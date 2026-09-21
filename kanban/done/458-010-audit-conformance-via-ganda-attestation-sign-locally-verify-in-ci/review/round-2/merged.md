# Round 2 — merged findings
**Date:** 2026-09-21
**Sources:** general

## Counts

| Severity | open | fixed | wontfix |
|----------|------|-------|---------|
| bug | 0 | 0 | 0 |
| suggestion | 0 | 0 | 0 |
| nit | 0 | 0 | 0 |

## Issues

None. Round 2 reviewed commit `db9c6a3a` (Layer 3 restated as attestation, public-half rotation, `off` waiver, kitchen close-out). Zero issues.

## Resolved prior (round 1 — verifier half, frozen)

| ID | Severity | Status | Notes |
|----|----------|--------|-------|
| M1 | bug (MED) | fixed | `DecodeSignature` unpadded base64url only (`2e9b65f2`) |
| M2 | suggestion (LOW) | fixed | git stderr locale-pinned via Amuru |
| M3 | suggestion (LOW) | fixed | unrecognized `attestation.mode` warns |
| M4 | nit (INFO) | wontfix | UnknownKey-before-TreeMismatch diagnostic order; first-failing-check. Decider: orchestrator (verifier-half review) |

## Duplicates / conflicts

- None. Single general reviewer; no overlap with new round-2 findings.

## Verification (this round)

- Smoke 1: `**Attestation.**` at `convention.md:81`
- Smoke 2: waiver `"mode": "off"` at `convention.md:102`; `#### Key rotation (public half)` at `readme.md:184`
- `tw-audit-1` hex in readme equals `AttestationVerifier.KnownKeys` (`ea6d9ea94f07d0ffe4d46fa7021115f2d5130b715fced113c8742e1d3be94681`)
- attestation-01 **30/30**; attestation-02 **1/1** (throwaway key); attestation-03 **18/18**
- Smoke 4: `dev workflow --mode pr --attestation warn` Step 1 advisory contains `pull master locally so ganda can attest`; pipeline did not abort (killed after Step 2 clean so the review would not run a full CI pack)
- Remaining operator work still open: org-wide `ci` required checks, 7 audit-fix partials, PR warn→require; one-off `ganda repo audit --fix` checklist item remains unchecked with wave notes
