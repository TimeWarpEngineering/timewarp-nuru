# Round 2 — general
**Date:** 2026-09-21
**Scope reviewed:** commit db9c6a3a vs origin/master (docs Layer 3 attestation + rotation + waiver)

## Summary

Commit `db9c6a3a` is documentation and kitchen close-out: it restates convention Layer 3 as sign-locally / verify-in-CI attestation, records the `attestation.mode: off` waiver, writes the public-half `KnownKeys` rotation procedure, and checks off shipped signer/verifier items without silently closing remaining operator work. Falsifiable claims match the shipped verifier and policy (`off`/`warn`/`require`, PR/merge default `warn`, release default `require`, CLI `--attestation` override). Risk is low; this diff does not regress the verifier half.

## Issues
