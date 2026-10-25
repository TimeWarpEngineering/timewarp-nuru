# CI path filters: skip the full build for kanban-only and non-code changes

## Description

Most pushes to master change only `kanban/` or other non-code assets, yet every one runs the full CI build (about 11 minutes). Skip the build for those changes, cancel superseded PR runs, and measure how much the caches help.

Source: [CI speed eval (timewarp-intel PR #74)](https://github.com/TimeWarpEngineering/timewarp-intel/pull/74), file `evaluations/2026-10-25-ci-speed-montemagno.md`. The eval found that 31 of the last 40 master pushes were kanban-only, which would save about 390 runner-minutes per 40 pushes.

## Requirements

- Pushes and PRs that change only `kanban/**` or other non-code assets (for example docs-only or image assets) do not run the full build in `.github/workflows/workflow.yml`.
- Required status checks still pass when the build is skipped, so branch protection does not block kanban-only PRs or `ganda kanban publish`. Prefer a job that always runs and reports success when the build is skipped (or a paths-filter step), not a workflow-level `paths-ignore` that leaves a required check pending.
- Concurrency: `cancel-in-progress` for PR runs only. Master runs are never cancelled.
- Time the caches (NuGet and any build cache) and record hit/miss and the minutes saved in Results.
- Use the newest current releases of actions and tools, not LTS lines.

## Checklist

- [ ] Define the non-code path set and confirm it with real recent pushes
- [ ] Add path filtering that still satisfies required checks
- [ ] Add PR-only concurrency cancel-in-progress
- [ ] Time cache hit vs miss and record the result
- [ ] Prove it with a kanban-only PR (build skipped, checks green) and a code PR (full build runs)
- [ ] Open a PR with run links as proof

## Notes

- Filed 2026-10-25 at Steven's request, relayed by Amina. File only, do not launch.
- Follow-up task for splitting CI into parallel jobs depends on this one.

## Results
