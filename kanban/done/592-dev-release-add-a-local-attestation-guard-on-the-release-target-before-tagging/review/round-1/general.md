# Round 1 — general
**Date:** 2026-11-18
**Scope reviewed:** branch `task/592-dev-release-add-a-local-attestation-guard-on-the` vs `master` (feature commit `997fd666`), plus the kitchen `## Goal (implementer restatement)`, `## Results`, and `### How to validate`.

## Summary

`dev release` now runs a step-9 attestation guard on the selected release target, before the dry-run stop and before the tag. The git-and-openssl verify moved from `workflow-command.attestation.cs` into `AttestationVerifyRunner`; `dev workflow` calls it with `HEAD`, and the release guard calls it with the target sha. Mode resolution uses the same release default as the `release: published` job (`require` when unset or unrecognized). The pass line, refusal block, and remedy match the task. The new tests sign a throwaway key and cover pass, require, warn, off, and a walked-back target whose HEAD note must not count. The recorded build, audit, 7/7 guard tests, and the two dry-run transcripts cover that change. No issues.

## Issues

<!-- None. -->
