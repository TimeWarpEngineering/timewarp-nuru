# Release 4.0.0-beta.9

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.8 to 4.0.0-beta.9, merge, then cut
the release with `dev release` from the master worktree (tw-release convention).

Ships everything merged since v4.0.0-beta.8: 558 (#374).

Release-note callouts:
- DevCli source content no longer trips consumer analyzers that Nuru used to suppress. That includes
  RCS1194 on `PackageFeedUnreadableException`, which timewarp-architecture hit. RCS1194, RCS1021,
  RCS1238, RCS1123, RCS9001-9003 and IDE1006 are now on in Nuru (558).

## Checklist

- [x] Bump version to 4.0.0-beta.9
- [ ] Bump PR merged
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.9
- [ ] Tell architecture beta.9 carries the RCS1194 fix

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-30)

## Notes
