# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** branch `task/538-split-ci-into-parallel-jobs-to-cut-wall-time` vs merge-base `46dabc6de2d2e6b4f67086da7eb74a0c348ab602`. Product diff only (workflow, setup-dotnet composite action, dev-cli workflow and test commands, CI test runners, nuru-specific path note).

## Summary

The single `ci` job is now four parallel stages (`build`, `samples`, `test-multi`, `test-standalone`) plus a final job still named `ci`. That job fails unless `needs.stage.result` is `success`, and it is skipped on the same `code=false` gate as before, so a non-code pull request still satisfies the required check without a settings change. Package promotion still publishes `Packages-<run_number>` only from the build stage's verified nupkgs, and only after the aggregate job's require step has passed: `staged-packages` does not match `CiRunPromotion.SelectPackagesArtifact`, and the download and re-upload steps use the same `artifacts/packages/*.nupkg` glob the previous green-master upload used. Local `dev workflow` with no `--stage` still runs attestation, clean, build, samples, both test phases, and the task 503 integrity gate. The implementer restatement matches the task and the Notes (keep the check name `ci`, do not edit branch protection, keep the `Packages-*` artifact). No misread.

## Issues

No issues.
