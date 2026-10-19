# Release 4.0.0-beta.3

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.2 to 4.0.0-beta.3,
merge, wait for green master CI, then cut the release with `dev release` from the
master worktree (tw-release convention).

## Checklist

- [x] Bump version to 4.0.0-beta.3
- [ ] Bump PR merged, master CI green
- [ ] `dev release --dry-run` clean
- [ ] `dev release` cut; release pipeline pushed all packages

## Session

- Created: 620936 (2026-10-19)

## Notes

Ships work merged since beta.2 (508-014, PR #333).
