# CI path filters: skip the full build for kanban-only and non-code changes

## Description

Most pushes to master change only `kanban/` or other non-code assets, yet every one runs the full CI build (about 11 minutes). Skip the build for those changes, cancel superseded PR runs, and measure how much the caches help.

Source: [CI speed eval (timewarp-intel PR #74)](https://github.com/TimeWarpEngineering/timewarp-intel/pull/74), file `evaluations/2026-10-25-ci-speed-montemagno.md`. The eval found that 31 of the last 40 master pushes were kanban-only, which would save about 390 runner-minutes per 40 pushes.

## Goal (implementer restatement)

Stop paying ~11 runner-minutes for pushes and PRs that change nothing the build reads (board files,
docs, agent instructions, unpacked images), without weakening anything that depends on CI.

Acceptance:

- A kanban-only (or other non-code) master push or PR does not run the build in `workflow.yml`.
- The required check `ci` still goes green on a non-code PR, so it stays mergeable.
- A superseded PR run is cancelled; master, release and dispatch runs are never cancelled.
- `release:` and `workflow_dispatch` are never path-filtered.
- `dev release` must never see a skipped-build run as a tested commit with a `Packages-*` artifact.
- Actions are on their newest majors.
- Results record NuGet cache hit vs miss timings and the minutes saved.

## Requirements

- Pushes and PRs that change only `kanban/**` or other non-code assets (for example docs-only or image assets) do not run the full build in `.github/workflows/workflow.yml`.
- Required status checks still pass when the build is skipped, so branch protection does not block kanban-only PRs or `ganda kanban publish`. Prefer a job that always runs and reports success when the build is skipped (or a paths-filter step), not a workflow-level `paths-ignore` that leaves a required check pending.
- Concurrency: `cancel-in-progress` for PR runs only. Master runs are never cancelled.
- Time the caches (NuGet and any build cache) and record hit/miss and the minutes saved in Results.
- Use the newest current releases of actions and tools, not LTS lines.

## Checklist

- [x] Define the non-code path set and confirm it with real recent pushes
- [x] Add path filtering that still satisfies required checks
- [x] Add PR-only concurrency cancel-in-progress
- [x] Time cache hit vs miss and record the result
- [ ] Prove it with a kanban-only PR (build skipped, checks green) and a code PR (full build runs) — gate proven offline against real PRs #364/#357/#363/#362 and the wiring by dispatch run 40560311385; the live code PR is this task's PR (host open-pr), the live kanban-only PR is the first one after merge (see How to validate)
- [ ] Open a PR with run links as proof — host open-pr node

## Notes

- Filed 2026-10-25 at Steven's request, relayed by Amina. File only, do not launch.
- Released safety: `dev release` (543, PR #364) tags the newest commit with a green `workflow.yml` run when only kanban-only commits follow it. A kanban-only push that no longer runs the build must not break that: the release run's `locate-run`/`download-artifact` still needs the `Packages-*` artifact of the tagged (build) commit, and a skipped-build run must not count as the "successful CI run" dev release looks for, or it would tag a commit with no artifact. Verify with `dev release --dry-run` after a kanban-only push. See also 549.
- The `release:` trigger and `workflow_dispatch` modes must never be path-filtered.
- Launched 2026-10-26 at Steven's request ("can we do 537 next?", 2026-10-28). why: cockpit 7dce871f
- Follow-up task for splitting CI into parallel jobs depends on this one.

## Results

### What changed

`.github/workflows/workflow.yml`:

- **push (master): `paths-ignore`** for `kanban/**`, `documentation/**`, `skills/**`, `.agent/**`,
  `.claude/**`, `assets/mascot/**`, `assets/timewarp-nuru-avatar.svg`, `changelog.md`. A push that
  changes only those paths gets **no run**. `readme.md` and `assets/logo.png` stay code (both are
  packed into the nupkgs).
- **pull_request: `changes` job** (always runs, ~4 s). It reads the same list from
  `on.push.paths-ignore` with `yq` (single source), lists the PR's files with the files API
  (including rename sources), and sets `code=false` only when every path matches. `ci` has
  `needs: changes` and `if: !cancelled() && needs.changes.outputs.code != 'false'`, so only an
  explicit `false` skips; a failed gate, a truncated file list (fewer than `changed_files`), or any
  non-PR event builds. `ci` is the required check (`contexts: ["ci"]`); a skipped job reports
  success, so a kanban-only PR stays mergeable.
- **concurrency**: group `CI/CD-pr-<n>` with `cancel-in-progress` for pull requests; every other
  event gets `CI/CD-run-<run_id>`, so master, release and dispatch runs are never cancelled or queued.
- **actions**: `actions/checkout` v4 → v7 (v7.0.1), `actions/setup-dotnet` v4 → v6 (v6.0.0),
  `actions/upload-artifact` v4 → v7 (v7.0.2). `nuget/login@v1` already resolves to v1.2.0 (newest).
  .NET stays `10.0.x`: 10 is the newest GA SDK (global.json `10.0.100`, `latestMinor`).
- `release:` and `workflow_dispatch` are untouched by both filters (`paths-ignore` only applies to
  push/pull_request; the gate returns `code=true` for every non-PR event).
- Boy-scout: `ganda repo audit --fix` refreshed the outdated memsearch `.githooks/pre-push.cs`
  (the only audit finding); audit now passes clean.

### Why push uses `paths-ignore` and PRs use a job

The Requirements prefer a job over `paths-ignore` because of required checks. Those gate pull
requests only. Master pushes go in by admin bypass (`enforce_admins: false`), so no check waits on
a push run. For push the job approach is unsafe: a run with `ci` skipped concludes **success**, and
`dev release` step 8 (`gh run list --commit HEAD --status success`) would accept it and tag a
commit whose run has no `Packages-*` artifact; release-mode `locate-run` would then fail after the
tag and GitHub release exist. With `paths-ignore` a kanban-only push has no run at all, so a
skipped build can never pass as tested.

**Consequence for releases (read before the next `dev release`).** The Notes assume 543 (walk back
over a kanban-only tail) has landed. It has not: PR #364 only reopened 543, and
`release-command.cs` step 8 still needs a green run at exactly HEAD. Today every master push runs,
so HEAD = `land kanban N` has a run. After this merges it will not, and `dev release` refuses
safely ("no successful CI run of workflow.yml exists for <sha>"). Remedy until 543 lands:
`gh workflow run workflow.yml --ref master -f mode=merge`, wait for green, then `dev release`
(dispatch is never filtered and uploads `Packages-*` on master). 543 removes the extra step.

### Path set, confirmed on the last 40 first-parent master commits (to `19397680`)

36 skip, 4 build. The 4 builds are the code merges #363 (`source/Directory.Build.props`),
#362, #361, #360. Every `land kanban N` and the merges of #364 and #357 skip (#357's `.gitignore`
line was already on master, so its merge diff is kanban-only; its PR gate still builds). 36 × ~11 min ≈ **6.6 runner-hours saved per 40 pushes**
(the eval estimated 31/40 and ~390 min).

### Gate proven offline with the real script and real PRs

Extracted `jobs.changes.steps[1].run` with yq 4.54.1 and ran it against the files API:

| PR | files | result |
|----|-------|--------|
| #364 (kanban only) | 1 | `code=false`, skip |
| #357 (kanban + `.gitignore`) | 2 | `code=true`, `.gitignore` |
| #363 (version bump) | 2 | `code=true`, `source/Directory.Build.props` |
| #362 (analyzer) | 28 | `code=true` |
| `EVENT_NAME=push` | — | `code=true`, never gated |

`actionlint` 1.7.12 passes on the new workflow.

### Live run on the branch

Dispatch (`mode=merge`) run [40560311385](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40560311385):
green. `changes` 4 s ("Event workflow_dispatch is never path-gated: full build."), then `ci` 11 min
with checkout v7 + setup-dotnet v6 (cache hit, 648 MB). Upload steps skipped as designed (not
master).

### Cache timing (NuGet global-packages via setup-dotnet; there is no build cache)

The runfile build cache is deliberately not cached (see the comment on the setup-dotnet step).

| run | cache | Setup .NET | Post Setup (save) | listed restores | job |
|-----|-------|-----------|-------------------|-----------------|-----|
| [39483717551](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/39483717551) (#341, key change) | miss | 16 s | 12 s | 3.5 s | 13.5 min |
| [39939473436](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/39939473436) (#352, key change) | miss | 9 s | 8 s | 4.4 s | 11.4 min |
| [40449906207](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40449906207) | hit | 14 s | 0 s | 1.6 s | 12.9 min |
| [40524210175](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40524210175) | hit | 19 s | 0 s | 1.4 s | 9.8 min |
| [40560311385](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40560311385) (this branch) | hit | 17 s | 0 s | — | 11.0 min |

A hit downloads and extracts 648 MB in ~8–10 s and saves ~2–3 s of listed project restores plus the
unlisted runfile restores (pipeline start → solution restore done: 38.6 s hit vs 47.9 s miss). A
miss adds 8–12 s to save. Net: **the cache saves well under a minute per run (~0.1–0.3 min)**,
inside run-to-run noise (9.8–13.5 min). It is not where the time goes; the path filter is. Kept
as is; the follow-up parallel-jobs task is the place to revisit it.

### How to validate

Smoke:

1. This PR (code change) — its `changes` job prints `Code path changed: .githooks/pre-push.cs`
   (or `.github/workflows/workflow.yml`) and `ci` runs the full build.
2. After merge, the host's `land kanban 537` push: `gh run list --workflow workflow.yml --commit <sha>`
   shows **no run** for it.
3. Next kanban-only PR (for example any `ganda kanban publish` / board-only PR): `changes` prints
   `All N changed paths are non-code`, `ci` shows **skipped**, the PR is mergeable.
4. Push twice to one PR branch quickly: the first run is cancelled.
5. `dev release --dry-run` from synced master with a kanban-only HEAD refuses with "no successful
   CI run"; after `gh workflow run workflow.yml --ref master -f mode=merge` goes green it passes.

Expect: steps 2–3 cost no build minutes; `ci` stays green on step 3; step 5 never accepts a commit
without a `Packages-*` artifact.

## Session

- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-27T18:41:11Z
