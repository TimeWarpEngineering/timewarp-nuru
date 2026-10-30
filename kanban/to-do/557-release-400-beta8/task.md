# Release 4.0.0-beta.8

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.7 to 4.0.0-beta.8, merge, then cut
the release with `dev release` from the master worktree (tw-release convention). This is the first
release cut with 543 on master, so a kanban-only HEAD should no longer need a manual CI run.

Ships everything merged since v4.0.0-beta.7: 536 (#370), 543 (#371).

Release-note callouts:
- DevCli check-version and `dev release` read nuget.org **and** the org GitHub Packages feed. If GitHub
  Packages cannot be read they refuse with the feed, the HTTP status and the fix
  (`gh auth refresh -s read:packages`, or `packages: read` on the Actions job). Repos on the shared
  `workflow.yml` must add `packages: read` to the release job before taking this DevCli (ganda 516) (536).
- `dev release` tags the newest green CI commit when every later commit changes only `kanban/` (543).

## Checklist

- [x] Bump version to 4.0.0-beta.8
- [ ] Bump PR merged
- [ ] `dev release --dry-run` clean (expect the kanban-only walk-back)
- [ ] `dev release` cut; release pipeline pushed all packages
- [ ] Consumer check (`skills/tw-nuru-verify`) against the published beta.8
- [ ] Tell ganda (488) and taratibu beta.8 is out; other repos wait for ganda 516

## Session

- Created: 7dce871f-a50c-41b9-b2b5-aefa4c4ad890 (2026-10-30)

## Notes
