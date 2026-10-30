# Release 4.0.0-beta.10

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.9 to 4.0.0-beta.10, merge, then cut
the release with `dev release` from the master worktree (tw-release convention).

Ships everything merged since v4.0.0-beta.9: 560 (#376), 561 (#377), 562 (#378).

Release-note callouts:
- **Breaking (DevCli):** every DevCli content type is `internal`, and handlers stay nested. Take
  TimeWarp.Nuru and TimeWarp.Nuru.DevCli at the same version. A repo whose own public `Handler` exposes
  DevCli types fails with CS0051; the fix is `public` to `internal` (560).
- The generator no longer reports NURU054 for an internal service type in the app's own assembly (560).
- DevCli content is clean for CA1034, CA2000 and CA2263 under consumer analyzer settings. CI now compiles
  it that way (560, 562).
- `UseTelemetry(o => { o.ActivitySources.Add("..."); o.Meters.Add("..."); })` exports extra sources an app
  names, for example `TimeWarp.Taratibu` (561).

## Checklist

- [x] Bump version to 4.0.0-beta.10
- [ ] Bump PR merged
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.10
- [ ] Tell architecture (457, CA2263) and ganda (Taratibu spans) beta.10 is out

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-31)

## Notes
