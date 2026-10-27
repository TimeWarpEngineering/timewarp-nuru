# Round 1 — general
**Date:** 2026-10-28
**Scope reviewed:** `master...HEAD` (merge-base `cafd5fcf`) — `.github/workflows/workflow.yml`, `.githooks/pre-push.cs`, task.md Results against the original task and `## Goal (implementer restatement)`

## Summary

Push to `master` uses `paths-ignore`, so a change that only touches the non-code list creates no `workflow.yml` run. Pull requests always run a `changes` job and skip `ci` only when that job sets `code=false`, so the required `ci` check is still reported. `release` and `workflow_dispatch` are outside both filters. Concurrency cancels in-progress runs only for `pull_request`; every other event gets its own `run_id` group. Checkout, setup-dotnet, and upload-artifact move to current majors. The pre-push hook returns success on an empty dest list and still refuses a listed home-branch update and a `task/*` push onto home.

The restatement matches the task. It adds the Notes constraints (release and dispatch stay ungated; `dev release` must not treat a skipped build as a tested commit) and treats packed `readme.md` and `assets/logo.png` as code. Both files are packed (`source/timewarp-nuru/timewarp-nuru.csproj`, `source/Directory.Build.props`). That is the right reading of "nothing the build reads," so there is no misread.

Re-checked the last 40 first-parent commits ending at `19397680`: the four builds are the merges of #363, #362, #361, and #360. #364 changes one kanban task file. #357's merge tree equals its first parent, so that push has no file delta; its PR still builds because `.gitignore` is outside the list. `CiRunPromotion.OrderCandidateRuns` drops `pull_request` runs, and `dev release` step 8 asks `gh run list` for `--status success` before that filter, so a skipped PR build cannot pass as the tested commit. A kanban-only push has no run to pass either. Bash `[[ == ]]` matches the eight ignore patterns, including dot-directories. Low risk. No issues.

## Issues

<!-- none -->
