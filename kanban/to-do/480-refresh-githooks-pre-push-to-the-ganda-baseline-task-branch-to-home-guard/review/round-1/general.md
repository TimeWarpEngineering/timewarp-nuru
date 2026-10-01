# Round 1 — general
**Date:** 2026-10-01
**Scope reviewed:** commit 08d4f201 — `.githooks/pre-push.cs`, task.md

## Summary

The change adds the ganda-baseline guard that refuses pushing a local `refs/heads/task/*` ref to
`refs/heads/master|main`, from any HEAD, before the existing exempt-dest and home-HEAD checks.
It reuses the existing `IsHomeBranchDest` helper; raw-sha sources and branch deletions (local ref
`(delete)`) are unaffected. Verified: smoke test task→home exits 1 with the expected message,
raw sha→home exits 0, and `ganda repo audit` passes all checks. Low risk; no issues found.

## Issues

<!-- none -->
