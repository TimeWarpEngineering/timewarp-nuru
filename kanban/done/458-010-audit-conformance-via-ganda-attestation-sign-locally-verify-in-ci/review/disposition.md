# Disposition — task 458-010

**Date:** 2026-09-21
**Outcome:** accepted-exceptions
**Rounds:** 2
**Final open count:** 0

## Summary

Round 2 (effort 1, general) reviewed the remaining Nuru-side product work in commit `db9c6a3a`: Layer 3 restated as sign-locally / verify-in-CI attestation, the `attestation.mode: off` waiver, and the public-half `KnownKeys` rotation procedure. Zero new findings. Falsifiable Results smokes match the files and the shipped verifier (30/30, 1/1, 18/18; PR warn advisory continues). Remaining operator work (org-wide `ci` required checks, 7 audit-fix partials, warn→require) stays recorded as open.

Round 1 (verifier half, frozen) remains the source of the task-level exception: M4 diagnostic-order **wontfix**. M1–M3 are **fixed**. No sibling apply-review task; fix loop stayed on this id.

## Exception log (if accepted-exceptions)

| ID | Severity | Rationale | Decided by |
|----|----------|-----------|------------|
| M4 | nit (INFO) | Evaluate checks UnknownKey before TreeMismatch (spec lists tree first). Diagnostic ordering only; neither order can yield Valid on a false condition. Deliberate first-failing-check design, documented in the verifier Design region. | orchestrator (verifier-half review, 2026-08-08) |

## Escalations

- None this round.
- Ganda-side decoder tightening remains timewarp-ganda kanban 200 (signer already emits spec-conformant output).

## Prior verifier-half note (2026-08-08)

Round 1 files: `review/round-1/merged.md`. Roster then: 1 × general-purpose (sonnet) adversarial reviewer; 2 internal verifier rounds (find → fix+verify) collapsed into that folder. Commits `b7f0cf34` + `2e9b65f2`. CI then: 1561 total / 1554 passed / 7 skipped / 0 failed.
