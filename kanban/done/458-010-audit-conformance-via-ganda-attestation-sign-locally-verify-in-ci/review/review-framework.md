# Review framework — task 458-010

**Date:** 2026-09-21
**Host task:** kanban/in-progress/458-010-audit-conformance-via-ganda-attestation-sign-locally-verify-in-ci/
**Diff scope:** branch `task/458-010-audit-conformance-via-ganda-attestation-sign-local` vs `origin/master` — commit `db9c6a3a` (`docs: record Layer 3 as attestation and the public-side key rotation`). Five files: parent `review/convention.md`, parent `review/repo-matrix.md`, `source/timewarp-nuru-devcli/readme.md`, this `task.md`, `.gitignore`.
**Plan / brief:** Remaining Nuru-side 458-010 product work: fold the shipped sign-locally / verify-in-CI model into the canonical convention, write the public-side key-rotation procedure, record the `attestation.mode: off` waiver, close shipped checklist items. Signer (ganda 199) and verifier (`b7f0cf34` + `2e9b65f2` + 458-011) are already implemented; this pass is documentation + kitchen close-out, not a new verifier.
**Effort:** 1 (general only)
**Reviewer roster:** general
**Session IDs:** grok review oracle (2026-09-21)

## Prior rounds (frozen)

- `review/round-1/merged.md` and the previous `review/disposition.md` cover the **verifier half** (commits `b7f0cf34` / `2e9b65f2`): 2 rounds, 1 MED + 2 LOW fixed, 1 INFO wontfix, outcome **accepted-exceptions**. Do not re-open those IDs unless this diff regresses them.
- This review is **round 2** of the host task, scoped to `db9c6a3a`.

## Ground rules

- Reviewers are read-only on product code; they write only under `review/round-2/`
- Severity: bug | suggestion | nit — Status starts as open
- Do not invent issues to fill space; zero issues is a valid outcome
- Address the diff and surrounding call sites; re-verify falsifiable claims against the repo
- Prior rounds are immutable; new work goes in `round-2/`
- Do not touch `~/.timewarp/ganda/keys/`
- Confirm Results smoke commands actually match the files; confirm `tw-audit-1` hex in the DevCli readme equals `AttestationVerifier.KnownKeys`
- Confirm convention Layer 3 matches shipped verifier/policy (mode defaults, waiver, rotation, event matrix) and that remaining operator work (org-wide `ci` required checks, 7 audit partials, warn→require) is not silently marked done
