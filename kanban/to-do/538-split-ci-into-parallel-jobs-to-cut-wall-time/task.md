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

## Checklist

- [ ] Profile the current run to find the slow independent stages
- [ ] Split into parallel jobs
- [ ] Update required checks
- [ ] Record before and after wall time and runner-minutes
- [ ] Open a PR with run links as proof

## Notes

- Filed 2026-10-25 at Steven's request, relayed by Amina. File only, do not launch.

## Results
