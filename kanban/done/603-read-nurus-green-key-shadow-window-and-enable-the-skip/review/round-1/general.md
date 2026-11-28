# Round 1 — general
**Date:** 2026-11-28
**Scope reviewed:** branch vs master, as in `review/review-framework.md`

## Summary

The change turns `GreenKeyShadow.Enabled` off and wires the workflow to act on `green_key_run`. A PR skip runs no stage, attests in `ci`, and leaves `ci` green. A master skip carries a live `Packages-*`, checks it against the packable set, and republishes it as this run's `Packages-<run_number>`. Pack-only and full runs build. The dev-cli side is sound: `PipelineFor`, `FormatActed`, the carry verification, and the rule that only a full success stages a marker all match the restatement. Two problems remain. The pack-only gate puts `matrix.stage` in a job-level `if`, and GitHub does not allow `matrix` there, so the workflow file is invalid. Separately, the new `green-key` dependency adds wall time to every push, and the minutes-saved rule does not count that time.

The restatement matches the task's Requirements and Steven's 2026-11-28 decision. No misread.

## Issues

### Issue 1 — Severity: bug
- File: .github/workflows/workflow.yml:172
- Description: The `stage` job's job-level `if` now ends with `... != 'pack-only' || matrix.stage == 'build'`. GitHub Actions does not offer the `matrix` context in `jobs.<job_id>.if`; only `github`, `needs`, `vars`, and `inputs` are available there, because the `if` runs before the matrix expands. GitHub rejects the workflow file with `Unrecognized named-value: 'matrix'`, so every event runs nothing. The required `ci` check is never reported, PRs cannot merge, and master pushes produce no `Packages-*`. The Results "Residual risk" paragraph assumes some legs would be skipped and one would run, which cannot happen. The recorded gates (dev build, the rules runfile, `workflow --help`, audit) never parse the workflow. The test `Workflow_skips_stages_and_republishes_carried_packages` pins the invalid expression (`ShouldContain("!= 'pack-only' || matrix.stage == 'build'")`). Verified by reading; `actionlint` is not installed on this machine, so no targeted command was run.
- Suggestion: Remove `matrix.stage` from the job-level `if` and pick the legs in `strategy.matrix`, where `needs` is allowed. For example: `stage: ${{ fromJSON((needs.changes.outputs.green_key_run || needs['green-key'].outputs.green_key_run) == 'pack-only' && '["build"]' || '["build","samples","test-multi","test-standalone"]') }}`. Keep the `!= 'skip'` gate in the job `if`. A pack-only run then has a single build leg, so `needs.stage.result` is plain success or failure, and the residual risk in Results goes away. Update the test pin, and record a workflow-parse check (for example actionlint, or a pushed run) in `### How to validate`.
- Status: open

### Issue 2 — Severity: suggestion
- File: .github/workflows/workflow.yml:165 (`needs: [changes, green-key]`); task.md `## Results` subtraction rule
- Description: On a push, the stage matrix now waits for the `green-key` job: checkout, setup-dotnet, a `dev.cs` compile, and `gh api` lookups. Before this change, the matrix started without waiting for it. Every push, hit or miss, now pays that job's wall time on the critical path. The Results subtraction rule names only `ci-finish-seconds` (about 26 s). The task says to subtract the ci time the ledger adds, so the minutes-saved tally will overstate the saving.
- Suggestion: In the Results tally rule, also count the `green-key` job duration as cost on push runs. A tally then reads saved minutes as (full-run wall − skipped-run wall), summed over hits, minus the added green-key wall summed over all push runs. A log line like `ci-finish-seconds` from the observe step would make that easy.
- Status: open
