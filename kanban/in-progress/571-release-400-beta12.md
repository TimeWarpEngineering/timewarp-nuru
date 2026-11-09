# Release 4.0.0-beta.12

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.11 to 4.0.0-beta.12, merge, then cut
the release with `dev release` from the master worktree (tw-release convention).

Ships everything merged since v4.0.0-beta.11: 569 (#384).

Release-note callouts:
- DevCli `check-version`, `dev release` and the pack/push paths evaluate packable projects together
  instead of one MSBuild process per project. On Roslynk (24 csproj under `source/`) the scan went
  from ~114 s of silence to a fast run.
- `PackableProjectService` prints `Evaluating N projects for IsPackable…` before the scan, so the
  command never sits silent. Multiple failing projects surface as one exception with an inner
  `AggregateException`.
- Follow-up for consumers: Roslynk bumps its DevCli pin to beta.12.

## Checklist

- [x] Bump version to 4.0.0-beta.12
- [ ] Bump PR merged
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Tell Roslynk beta.12 is out

## Session

- Created: b3be0249-1852-425a-9e90-5a7e56049503 (2026-11-09)

## Notes
