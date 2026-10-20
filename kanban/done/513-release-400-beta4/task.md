# Release 4.0.0-beta.4

## Description

Bump `<Version>` in `source/Directory.Build.props` from 4.0.0-beta.3 to 4.0.0-beta.4, merge,
wait for green master CI, then cut the release with `dev release` from the master worktree
(tw-release convention).

Ships 508-009 (#335), 508-016 (#336), 508-011 (#337) and 508-015 (#338).

## Checklist

- [x] Bump version to 4.0.0-beta.4
- [x] Bump PR merged, master CI green
- [x] `dev release --dry-run` clean
- [x] `dev release` cut; release pipeline pushed all packages

## Session

- Created: 2026-10-20

## Notes

Release steps run from the master worktree right after this PR merges.
