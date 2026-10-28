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

- [ ] Profile the current run to find the slow independent stages
- [ ] Split into parallel jobs
- [ ] Update required checks
- [ ] Record before and after wall time and runner-minutes
- [ ] Open a PR with run links as proof

## Notes

- Filed 2026-10-25 at Steven's request, relayed by Amina. File only, do not launch.
- Keep a final job named `ci` that needs all the parallel jobs (and is skipped exactly when 537's `changes` job says skip), so the required check name stays `ci` and branch protection needs no edit. Do not change branch protection or repo settings yourself; if a settings change is unavoidable, stop and write the exact change in Results for Steven.
- The release pipeline (`locate-run`, `download-artifact`, `verify`, `push`) promotes the `Packages-*` artifact from the CI run of the tagged commit. Keep that artifact name and contents, and keep it uploaded by the run of that commit, or releases break. Check with `dev release --dry-run` logic and the release jobs in `workflow.yml`.
- 537 (PR #365) merged 2026-10-28: master non-code pushes start no run; PRs run a `changes` job and skip `ci` when every path is non-code.
- Launched 2026-10-28 at Steven's request. why: cockpit 7dce871f, Steven "yes" to launching 538, 2026-10-28

## Results
