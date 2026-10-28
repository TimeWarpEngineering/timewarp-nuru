# Split CI into parallel jobs to cut wall time

## Description

After the path filters land, split the single CI build into parallel jobs to cut wall time from about 11.4 minutes to about 7.5 minutes. That figure is Akili's estimate from the eval and has not been measured.

Source: [CI speed eval (timewarp-intel PR #74)](https://github.com/TimeWarpEngineering/timewarp-intel/pull/74), file `evaluations/2026-10-25-ci-speed-montemagno.md`.

## Depends on

- 537

## Requirements

- Break `.github/workflows/workflow.yml` into parallel jobs (for example build and pack, test groups, analyzer and generator tests) along the slowest independent stages, with shared artifacts or caches where needed.
- Required status checks are updated so branch protection still gates merges, and the path-filter skip from the dependency still satisfies them.
- Measure before and after wall time and total runner-minutes over several runs and record them in Results. Parallel jobs can raise total runner-minutes, so report both.
- Use the newest current releases of actions and tools, not LTS lines.

## Goal (implementer restatement)

Cut CI wall time by running the independent stages of the single `ci` job as parallel jobs.

Acceptance criteria:

- `workflow.yml` runs build (pack plus package gates), sample verification, and the two test phases (multi-mode assembly, standalone files) as separate parallel jobs.
- A final job named `ci` needs every parallel job, fails when any of them did not succeed, and is skipped exactly when `changes` says `code=false`. The required check name stays `ci`; no branch protection or settings edit.
- The green-master `Packages-<run_number>` artifact keeps its name and contents (the nupkgs from the Release build that passed the layout and configuration gates). It is uploaded only after every job passed, by the run of that commit, so `dev release` locate-run and download-artifact still work. Release and probe modes still run.
- Local `dev workflow` with no stage still runs the whole pipeline, including the package integrity gate.
- Newest action releases.
- Results record before and after wall time and runner-minutes over several runs.

## Checklist

- [x] Profile the current run to find the slow independent stages
- [x] Split into parallel jobs
- [x] Update required checks (no edit needed: the aggregate job is still `ci`)
- [x] Record before and after wall time and runner-minutes
- [ ] Open a PR with run links as proof (host open-pr node; run links are in Results)

## Review

- Round 1: Done

## Notes

- Filed 2026-10-25 at Steven's request, relayed by Amina. File only, do not launch.
- Keep a final job named `ci` that needs all the parallel jobs (and is skipped exactly when 537's `changes` job says skip), so the required check name stays `ci` and branch protection needs no edit. Do not change branch protection or repo settings yourself; if a settings change is unavoidable, stop and write the exact change in Results for Steven.
- The release pipeline (`locate-run`, `download-artifact`, `verify`, `push`) promotes the `Packages-*` artifact from the CI run of the tagged commit. Keep that artifact name and contents, and keep it uploaded by the run of that commit, or releases break. Check with `dev release --dry-run` logic and the release jobs in `workflow.yml`.
- 537 (PR #365) merged 2026-10-28: master non-code pushes start no run; PRs run a `changes` job and skip `ci` when every path is non-code.
- Launched 2026-10-28 at Steven's request. why: cockpit 7dce871f, Steven "yes" to launching 538, 2026-10-28

## Results

CI wall time fell from a 12.7 min median to 5.4 min. Runner time rose from 12.6 to 16.2 min (median).

### Profile (before)

From run [40567323828](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40567323828) (4 vCPU), one `ci` job, 14.4 min:

| Stage | Time |
|---|---|
| Checkout and setup .NET | about 20s |
| Install zsh (apt) | 99s here. Usually 10 to 20s, but the mirror sometimes serves 5 MB at 60 kB/s: 90s, 93s, 4m58s, 4m26s in the last 30 runs |
| Compile `dev.cs` | about 49s |
| Build and pack | 47s |
| Verify samples | 191s |
| Test: multi-mode compile | about 138s |
| Test: multi-mode run | 145s |
| Test: standalone phase | 149s |

Build, samples, and the two test phases do not depend on each other. Samples and tests build what they need themselves.

### What changed

- `workflow.yml`: a `stage` matrix job runs `build`, `samples`, `test-multi` and `test-standalone` in parallel (`fail-fast: false`, so every stage reports).
- `ci` needs `changes` and `stage`. It fails unless `needs.stage.result == success` and is skipped exactly when `changes` says `code=false`. It uses `always()`, not `!cancelled()`, so a cancelled run fails the required check instead of skipping it. On green master it downloads the build stage's `staged-packages` artifact, uploads it as `Packages-<run_number>` (same name, same nupkgs, 7 days), runs the keep-last-two prune, and deletes `staged-packages`.
- `staged-packages` deliberately does not start with `Packages-`, because `CiRunPromotion.SelectPackagesArtifact` takes any `Packages-*` artifact. Failed runs keep it for 3 days so `gh run rerun --failed` still works.
- Release events, break-glass dispatch, and the probe moved to a `release` job. Its steps, conditions and commands are the old ones, minus zsh and the upload. Its permissions are `actions: read` and `id-token: write`.
- Setup .NET and its cache config moved to the composite action `.github/actions/setup-dotnet`. The test-multi job caches the zsh debs (`actions/cache@v6`) and falls back to apt. A cache hit installs in 2 to 4s.
- `dev workflow --stage all|build|samples|test-multi|test-standalone`. With no `--stage`, local runs still run the full pipeline, including the task 503 integrity gate. The build stage keeps the layout and Release-configuration gates. In CI the separate runners do the integrity gate's job, because samples and tests cannot touch the build job's bytes.
- `dev test --phase all|multi|standalone`.
- The standalone list and loop moved to `tests/ci-standalone/run-standalone-tests.cs`, outside `tests/ci-tests`. Its job therefore skips the 140s multi-mode compile. `run-ci-tests.cs` still runs it after the multi phase unless `NURU_CI_PHASE=multi`. `.agent/local/nuru-specific.md` points at the new path.
- Action versions are the newest releases (checked 2026-10-28): checkout v7 (7.0.1), setup-dotnet v6 (6.0.0), upload-artifact v7 (7.0.2), download-artifact v8 (8.0.2), cache v6 (6.1.0), nuget/login v1 (1.2.0).
- Required checks: branch protection requires `ci` (app 15368, GitHub Actions), and the aggregate job keeps that name. No settings change is needed and none was made.

### Before and after

Before: the last 20 green non-release runs on master and PRs, 2026-10-26 to 2026-10-27. After: 3 `workflow_dispatch` (merge) runs of this branch. Runner time is the sum of job durations, including `changes` and `ci`. Billed counts each job rounded up to whole minutes; the repo is public, so this is a proxy.

| | Wall median | Wall range | Runner median | Billed median |
|---|---|---|---|---|
| Before (n=20) | 12.7 min | 9.9 to 14.7 | 12.6 min | 13 min |
| After (n=3) | 5.5 min | 5.4 to 5.5 | 16.2 min | 21 min |

After runs: [40611642444](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40611642444) (5.47 min, zsh cache miss), [40612260218](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40612260218) (5.37 min), [40612266762](https://github.com/TimeWarpEngineering/timewarp-nuru/actions/runs/40612266762) (5.48 min).

Job times in 40612266762: build 1.5 min, test-standalone 4.3 min, samples 5.1 min, test-multi 5.2 min, `ci` 3s. Samples and test-multi now tie for the critical path. The extra runner time is mostly each job's own checkout, setup and `dev.cs` compile (about 1 min per job).

### Not verifiable before merge

- The green-master upload path (`staged-packages` to `Packages-<run_number>`, prune, staged delete) runs only on `refs/heads/master`. Branch dispatch runs skip it.
- After merge, the first master push run should list `Packages-<n>` and no `staged-packages` (`gh api repos/TimeWarpEngineering/timewarp-nuru/actions/runs/<id>/artifacts --jq '.artifacts[].name'`), and `dev release --dry-run` should find that run.
- The non-code PR skip is unchanged (`changes` job untouched); the new jobs use the same `code != 'false'` gate.

### Boy-scout

`ganda repo audit` passed with no fixes needed.

### How to validate

Smoke:

```bash
dotnet run --file tools/dev-cli/dev.cs -- workflow --stage build
dotnet run --file tools/dev-cli/dev.cs -- test --phase standalone
dotnet run --file tools/dev-cli/dev.cs -- test --phase multi
actionlint .github/workflows/workflow.yml   # rhysd/actionlint 1.7.12
gh run view 40612266762 --json jobs -q '.jobs[] | [.name,.conclusion] | @tsv'
```

Expect:

- The build stage prints `Step 3/3`, `Package layout verified`, `Package configuration verified` and `Pipeline SUCCEEDED`.
- The standalone phase prints `Standalone tests: 41 files` and `Standalone finished ... (0 failed)` and exits 0.
- The multi phase prints `Multi-mode execute finished` and `NURU_CI_PHASE=multi: standalone phase skipped.` and exits 0.
- `dev test` with no phase runs both phases. Locally: 4458 tests, 0 failed.
- actionlint prints nothing and exits 0.
- The run shows build, samples, test-multi, test-standalone and ci all `success`, and release `skipped`.

### Review

Round 1, effort 3, roster general. Counts: bug 0 open / 0 fixed / 0 wontfix, suggestion 0/0/0, nit 0/0/0. Disposition: **clean**. No wontfix and no escalation. The implementer restatement matches the task and the Notes.

Paths: `review/review-framework.md`, `review/round-1/merged.md`, `review/disposition.md`.

## Session

- Implement oracle: implemented by implementer-claude (claude, model claude-opus-5-5), session not reported, max-turns 500, Done — 2026-10-28T01:06:52Z
- Review oracle: general (grok, model Grok 4.7), session not reported, max-turns uncapped (review budget 200), clean — 2026-10-28
- Review oracle: review by reviewer-cursor (cursor, model grok-4.7-high), session not reported, max-turns uncapped (no --max-turns flag; bound 200) — 2026-10-28T01:22:11Z
