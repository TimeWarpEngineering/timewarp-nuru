# Release 4.0.0-beta.11

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.10 to 4.0.0-beta.11, merge, then cut
the release with `dev release` from the master worktree (tw-release convention).

Ships everything merged since v4.0.0-beta.10: 564 (#380).

Release-note callouts:
- DevCli check-version and `dev release` read only the feeds a repo declares in
  `checkVersionConfig.feeds` (`nuget.org`, `github-packages`). The default is `nuget.org`. A repo that
  doesn't publish to GitHub Packages never needs `packages: read` (564).
- **Action for GitHub Packages publishers when they bump:**
  - Taratibu and Zana: `"feeds": "github-packages"`
  - ganda and Flexbox: `"feeds": "nuget.org,github-packages"`

  Without the key, their GitHub-only versions read as unpublished.
- Unblocks timewarp-mediator v15.0.0-beta.3 (run 41184701087).

## Checklist

- [x] Bump version to 4.0.0-beta.11
- [ ] Bump PR merged
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.11
- [ ] Tell mediator beta.11 is out

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-31)

## Notes
